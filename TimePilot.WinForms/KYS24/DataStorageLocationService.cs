namespace TimePilot.WinForms.KYS24
{
    internal enum DataStorageLocationKind
    {
        LegacyExe,
        MsixVirtualizedLocalCache,
        MsixLocalState
    }

    internal sealed record DataStorageCandidate(
        DataStorageLocationKind Kind,
        string DirectoryPath,
        bool IsCurrent,
        bool IsTarget,
        bool CanInspect,
        bool? DatabaseExists,
        bool? SettingsExists,
        bool? BackupDirectoryExists);

    internal sealed record DataStorageLocationPlan(
        bool IsPackaged,
        string CurrentDirectory,
        string TargetDirectory,
        IReadOnlyList<DataStorageCandidate> Candidates)
    {
        public bool RequiresMigration => !string.Equals(
            Path.TrimEndingDirectorySeparator(Path.GetFullPath(CurrentDirectory)),
            Path.TrimEndingDirectorySeparator(Path.GetFullPath(TargetDirectory)),
            StringComparison.OrdinalIgnoreCase);
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
                    .ToArray());
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
                canInspect ? backupDirectoryExists : null));
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
}
