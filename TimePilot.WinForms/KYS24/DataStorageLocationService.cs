using Microsoft.Data.Sqlite;

namespace TimePilot.WinForms.KYS24
{
    internal enum DataStorageLocationKind
    {
        LegacyExe,
        MsixVirtualizedLocalCache,
        MsixLocalState
    }

    internal enum DataStorageDatabaseState
    {
        NotInspected,
        NotPresent,
        Valid,
        Invalid,
        Unavailable
    }

    internal enum DataStorageMigrationDecisionKind
    {
        NoMigrationRequired,
        InitializeTarget,
        MigrateCurrentToTarget,
        UseExistingTarget,
        ConflictRequiresUserChoice,
        BlockedCurrentDatabaseInvalid,
        BlockedTargetDatabaseInvalid,
        InspectionFailed,
        TargetUnavailable
    }

    internal sealed record DataStorageCandidate(
        DataStorageLocationKind Kind,
        string DirectoryPath,
        bool IsCurrent,
        bool IsTarget,
        bool CanInspect,
        bool? DatabaseExists,
        bool? SettingsExists,
        bool? BackupDirectoryExists,
        DataStorageDatabaseState DatabaseState,
        string? DatabaseInspectionError);

    internal sealed record DataStorageMigrationDecision(
        DataStorageMigrationDecisionKind Kind,
        DataStorageCandidate? Current,
        DataStorageCandidate? Target)
    {
        public bool AllowsAutomaticAction => Kind is
            DataStorageMigrationDecisionKind.InitializeTarget
            or DataStorageMigrationDecisionKind.MigrateCurrentToTarget
            or DataStorageMigrationDecisionKind.UseExistingTarget;
    }

    internal sealed record DataStorageLocationPlan(
        bool IsPackaged,
        string CurrentDirectory,
        string TargetDirectory,
        IReadOnlyList<DataStorageCandidate> Candidates,
        bool IsTargetAvailable = true)
    {
        public bool RequiresMigration => !string.Equals(
            Path.TrimEndingDirectorySeparator(Path.GetFullPath(CurrentDirectory)),
            Path.TrimEndingDirectorySeparator(Path.GetFullPath(TargetDirectory)),
            StringComparison.OrdinalIgnoreCase);

        public DataStorageMigrationDecision MigrationDecision =>
            DataStorageMigrationPlanner.Decide(this);
    }

    internal static class DataStorageLocationService
    {
        private const string DataDirectoryName = "TimePilot";
        private const string PackageDirectoryPrefix = "YSBookcase.ActiveLogbook_";

        public static DataStorageLocationPlan Collect()
        {
            var isPackaged = WindowsStartupRegistration.IsPackagedApp();
            var localAppDataDirectory = Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData);
            var legacyDirectory = Path.Combine(localAppDataDirectory, DataDirectoryName);
            var packagedLocalCacheDirectory = AppDataPaths.TryGetPackagedLocalCacheDirectory();
            var packagedLocalStateDirectory = AppDataPaths.TryGetPackagedLocalStateDirectory();

            return BuildPlan(
                isPackaged,
                legacyDirectory,
                packagedLocalCacheDirectory,
                packagedLocalStateDirectory,
                DiscoverInstalledPackageDirectories(localAppDataDirectory));
        }

        internal static DataStorageLocationPlan BuildPlan(
            bool isPackaged,
            string legacyDirectory,
            string? packagedLocalCacheDirectory,
            string? packagedLocalStateDirectory,
            IReadOnlyList<string>? installedPackageDirectories = null)
        {
            var currentDirectory = isPackaged && !string.IsNullOrWhiteSpace(packagedLocalCacheDirectory)
                ? Path.Combine(packagedLocalCacheDirectory, "Local", DataDirectoryName)
                : legacyDirectory;
            var targetDirectory = ResolveTargetDataDirectory(
                isPackaged,
                legacyDirectory,
                packagedLocalStateDirectory,
                currentDirectory);
            var candidates = new List<DataStorageCandidate>();

            AddCandidate(
                candidates,
                DataStorageLocationKind.LegacyExe,
                legacyDirectory,
                isCurrent: PathsEqual(legacyDirectory, currentDirectory),
                isTarget: PathsEqual(legacyDirectory, targetDirectory),
                canInspect: !isPackaged);

            if (!string.IsNullOrWhiteSpace(packagedLocalCacheDirectory))
            {
                AddCandidate(
                    candidates,
                    DataStorageLocationKind.MsixVirtualizedLocalCache,
                    Path.Combine(packagedLocalCacheDirectory, "Local", DataDirectoryName),
                    isCurrent: isPackaged && PathsEqual(
                        Path.Combine(packagedLocalCacheDirectory, "Local", DataDirectoryName),
                        currentDirectory),
                    isTarget: PathsEqual(
                        Path.Combine(packagedLocalCacheDirectory, "Local", DataDirectoryName),
                        targetDirectory),
                    canInspect: true);
            }

            if (!string.IsNullOrWhiteSpace(packagedLocalStateDirectory))
            {
                AddCandidate(
                    candidates,
                    DataStorageLocationKind.MsixLocalState,
                    Path.Combine(packagedLocalStateDirectory, DataDirectoryName),
                    isCurrent: PathsEqual(
                        Path.Combine(packagedLocalStateDirectory, DataDirectoryName),
                        currentDirectory),
                    isTarget: PathsEqual(
                        Path.Combine(packagedLocalStateDirectory, DataDirectoryName),
                        targetDirectory),
                    canInspect: true);
            }

            foreach (var packageDirectory in installedPackageDirectories ?? Array.Empty<string>())
            {
                AddCandidate(
                    candidates,
                    DataStorageLocationKind.MsixVirtualizedLocalCache,
                    Path.Combine(packageDirectory, "LocalCache", "Local", DataDirectoryName),
                    isCurrent: false,
                    isTarget: false,
                    canInspect: true,
                    onlyWhenPresent: true);
                AddCandidate(
                    candidates,
                    DataStorageLocationKind.MsixLocalState,
                    Path.Combine(packageDirectory, "LocalState", DataDirectoryName),
                    isCurrent: false,
                    isTarget: false,
                    canInspect: true,
                    onlyWhenPresent: true);
            }

            return new DataStorageLocationPlan(
                isPackaged,
                currentDirectory,
                targetDirectory,
                candidates
                    .OrderByDescending(candidate => candidate.IsCurrent)
                    .ThenByDescending(candidate => candidate.IsTarget)
                    .ThenBy(candidate => candidate.Kind)
                    .ToArray(),
                IsTargetAvailable: !isPackaged || !string.IsNullOrWhiteSpace(packagedLocalStateDirectory));
        }

        internal static string ResolveTargetDataDirectory(
            bool isPackaged,
            string legacyDirectory,
            string? packagedLocalStateDirectory,
            string currentDirectory)
        {
            if (!isPackaged)
                return legacyDirectory;

            return string.IsNullOrWhiteSpace(packagedLocalStateDirectory)
                ? currentDirectory
                : Path.Combine(packagedLocalStateDirectory, DataDirectoryName);
        }

        private static void AddCandidate(
            ICollection<DataStorageCandidate> candidates,
            DataStorageLocationKind kind,
            string directory,
            bool isCurrent,
            bool isTarget,
            bool canInspect,
            bool onlyWhenPresent = false)
        {
            if (candidates.Any(candidate => PathsEqual(candidate.DirectoryPath, directory)))
                return;

            var databasePath = Path.Combine(directory, "timepilot.db");
            var settingsPath = Path.Combine(directory, "settings.json");
            var backupDirectory = Path.Combine(directory, "backups");
            var directoryExists = canInspect && Directory.Exists(directory);
            var databaseExists = canInspect && File.Exists(databasePath);
            var settingsExists = canInspect && File.Exists(settingsPath);
            var backupDirectoryExists = canInspect && Directory.Exists(backupDirectory);
            var databaseInspection = InspectDatabase(databasePath, canInspect);
            if (onlyWhenPresent
                && !directoryExists
                && !databaseExists
                && !settingsExists
                && !backupDirectoryExists)
            {
                return;
            }

            candidates.Add(new DataStorageCandidate(
                kind,
                directory,
                isCurrent,
                isTarget,
                canInspect,
                canInspect ? databaseExists : null,
                canInspect ? settingsExists : null,
                canInspect ? backupDirectoryExists : null,
                databaseInspection.State,
                databaseInspection.Error));
        }

        internal static (DataStorageDatabaseState State, string? Error) InspectDatabase(
            string databasePath,
            bool canInspect)
        {
            if (!canInspect)
                return (DataStorageDatabaseState.NotInspected, null);
            if (!File.Exists(databasePath))
                return (DataStorageDatabaseState.NotPresent, null);

            try
            {
                using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
                {
                    DataSource = databasePath,
                    Mode = SqliteOpenMode.ReadOnly,
                    Pooling = false
                }.ToString());
                connection.Open();

                using (var integrityCommand = connection.CreateCommand())
                {
                    integrityCommand.CommandText = "PRAGMA quick_check;";
                    var integrityResult = Convert.ToString(integrityCommand.ExecuteScalar());
                    if (!string.Equals(integrityResult, "ok", StringComparison.OrdinalIgnoreCase))
                    {
                        return (DataStorageDatabaseState.Invalid, integrityResult);
                    }
                }

                using var schemaCommand = connection.CreateCommand();
                schemaCommand.CommandText = """
                    SELECT COUNT(*)
                    FROM sqlite_master
                    WHERE type = 'table'
                      AND name IN ('apps', 'foreground_sessions');
                    """;
                var requiredTableCount = Convert.ToInt32(schemaCommand.ExecuteScalar());
                return requiredTableCount == 2
                    ? (DataStorageDatabaseState.Valid, null)
                    : (DataStorageDatabaseState.Invalid, "Required TimePilot tables were not found.");
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode is 11 or 26)
            {
                return (DataStorageDatabaseState.Invalid, ex.Message);
            }
            catch (Exception ex) when (ex is SqliteException
                or IOException
                or UnauthorizedAccessException
                or System.Security.SecurityException)
            {
                return (DataStorageDatabaseState.Unavailable, ex.Message);
            }
        }

        private static IReadOnlyList<string> DiscoverInstalledPackageDirectories(
            string localAppDataDirectory)
        {
            var packagesDirectory = Path.Combine(localAppDataDirectory, "Packages");
            if (!Directory.Exists(packagesDirectory))
                return Array.Empty<string>();

            try
            {
                return Directory.EnumerateDirectories(
                        packagesDirectory,
                        PackageDirectoryPrefix + "*",
                        SearchOption.TopDirectoryOnly)
                    .ToArray();
            }
            catch (Exception ex) when (ex is IOException
                or UnauthorizedAccessException
                or System.Security.SecurityException)
            {
                return Array.Empty<string>();
            }
        }

        private static bool PathsEqual(string left, string right)
        {
            try
            {
                return Path.TrimEndingDirectorySeparator(Path.GetFullPath(left)).Equals(
                    Path.TrimEndingDirectorySeparator(Path.GetFullPath(right)),
                    StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
            {
                return false;
            }
        }
    }

    internal static class DataStorageMigrationPlanner
    {
        public static DataStorageMigrationDecision Decide(DataStorageLocationPlan plan)
        {
            var current = plan.Candidates.FirstOrDefault(candidate => candidate.IsCurrent);
            var target = plan.Candidates.FirstOrDefault(candidate => candidate.IsTarget);

            if (plan.IsPackaged && !plan.IsTargetAvailable)
            {
                return new DataStorageMigrationDecision(
                    DataStorageMigrationDecisionKind.TargetUnavailable,
                    current,
                    target);
            }

            if (!plan.RequiresMigration)
            {
                return new DataStorageMigrationDecision(
                    DataStorageMigrationDecisionKind.NoMigrationRequired,
                    current,
                    target ?? current);
            }

            if (target is null)
            {
                return new DataStorageMigrationDecision(
                    DataStorageMigrationDecisionKind.TargetUnavailable,
                    current,
                    null);
            }

            if (current is null
                || current.DatabaseState is DataStorageDatabaseState.NotInspected
                    or DataStorageDatabaseState.Unavailable
                || target.DatabaseState is DataStorageDatabaseState.NotInspected
                    or DataStorageDatabaseState.Unavailable)
            {
                return new DataStorageMigrationDecision(
                    DataStorageMigrationDecisionKind.InspectionFailed,
                    current,
                    target);
            }

            var currentHasArtifacts = HasArtifacts(current);
            var targetHasArtifacts = HasArtifacts(target);

            if (current.DatabaseState == DataStorageDatabaseState.Invalid)
            {
                return new DataStorageMigrationDecision(
                    currentHasArtifacts && targetHasArtifacts
                        ? DataStorageMigrationDecisionKind.ConflictRequiresUserChoice
                        : DataStorageMigrationDecisionKind.BlockedCurrentDatabaseInvalid,
                    current,
                    target);
            }

            if (target.DatabaseState == DataStorageDatabaseState.Invalid)
            {
                return new DataStorageMigrationDecision(
                    DataStorageMigrationDecisionKind.BlockedTargetDatabaseInvalid,
                    current,
                    target);
            }

            if (currentHasArtifacts && targetHasArtifacts)
            {
                return new DataStorageMigrationDecision(
                    DataStorageMigrationDecisionKind.ConflictRequiresUserChoice,
                    current,
                    target);
            }

            if (currentHasArtifacts)
            {
                return new DataStorageMigrationDecision(
                    DataStorageMigrationDecisionKind.MigrateCurrentToTarget,
                    current,
                    target);
            }

            if (targetHasArtifacts)
            {
                return new DataStorageMigrationDecision(
                    DataStorageMigrationDecisionKind.UseExistingTarget,
                    current,
                    target);
            }

            return new DataStorageMigrationDecision(
                DataStorageMigrationDecisionKind.InitializeTarget,
                current,
                target);
        }

        private static bool HasArtifacts(DataStorageCandidate candidate)
        {
            return candidate.DatabaseExists == true
                || candidate.SettingsExists == true
                || candidate.BackupDirectoryExists == true;
        }
    }
}
