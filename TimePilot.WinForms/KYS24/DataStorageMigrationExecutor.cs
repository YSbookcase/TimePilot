using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace TimePilot.WinForms.KYS24
{
    internal sealed record DataStorageMigrationMarker(
        int SchemaVersion,
        string SourceDirectory,
        string TargetDirectory,
        DateTimeOffset CompletedAtUtc,
        string AppVersion);

    internal sealed record DataStorageMigrationExecutionResult(
        string SourceDirectory,
        string TargetDirectory,
        string MarkerPath);

    internal static class DataStorageMigrationExecutor
    {
        public const int MigrationSchemaVersion = 1;
        public const string MigrationMarkerFileName = ".storage-migration-v1.json";

        public static DataStorageMigrationExecutionResult Execute(
            DataStorageLocationPlan plan,
            DateTimeOffset completedAtUtc,
            string appVersion)
        {
            var decision = plan.MigrationDecision;
            if (decision.Kind != DataStorageMigrationDecisionKind.MigrateCurrentToTarget
                || decision.Current is null
                || decision.Target is null)
            {
                throw new InvalidOperationException(
                    $"Storage migration is not allowed for decision {decision.Kind}.");
            }

            var sourceDirectory = decision.Current.DirectoryPath;
            var targetDirectory = decision.Target.DirectoryPath;
            EnsureTargetIsEmpty(targetDirectory);

            var targetParentDirectory = Path.GetDirectoryName(targetDirectory)
                ?? throw new InvalidOperationException("The LocalState target parent could not be resolved.");
            Directory.CreateDirectory(targetParentDirectory);
            var stagingDirectory = Path.Combine(
                targetParentDirectory,
                $".{Path.GetFileName(targetDirectory)}-migration-{Guid.NewGuid():N}.tmp");

            try
            {
                Directory.CreateDirectory(stagingDirectory);
                CopyDirectoryWithoutDatabase(sourceDirectory, stagingDirectory);
                CopyDatabaseSnapshotIfPresent(sourceDirectory, stagingDirectory);
                ValidateStagingDatabase(sourceDirectory, stagingDirectory);

                var marker = new DataStorageMigrationMarker(
                    MigrationSchemaVersion,
                    sourceDirectory,
                    targetDirectory,
                    completedAtUtc.ToUniversalTime(),
                    appVersion);
                var markerPath = Path.Combine(stagingDirectory, MigrationMarkerFileName);
                File.WriteAllText(
                    markerPath,
                    JsonSerializer.Serialize(marker, new JsonSerializerOptions { WriteIndented = true }));

                if (Directory.Exists(targetDirectory))
                    Directory.Delete(targetDirectory, recursive: false);
                Directory.Move(stagingDirectory, targetDirectory);

                var promotedMarkerPath = Path.Combine(targetDirectory, MigrationMarkerFileName);
                if (!DataStorageLocationService.HasValidMigrationMarker(
                        sourceDirectory,
                        targetDirectory))
                {
                    throw new InvalidDataException("The promoted storage migration marker is invalid.");
                }

                return new DataStorageMigrationExecutionResult(
                    sourceDirectory,
                    targetDirectory,
                    promotedMarkerPath);
            }
            finally
            {
                TryDeleteDirectory(stagingDirectory);
            }
        }

        private static void EnsureTargetIsEmpty(string targetDirectory)
        {
            if (!Directory.Exists(targetDirectory))
                return;

            if (Directory.EnumerateFileSystemEntries(targetDirectory).Any())
            {
                throw new InvalidOperationException(
                    "The LocalState target is not empty. Automatic migration was stopped.");
            }
        }

        private static void CopyDirectoryWithoutDatabase(string sourceDirectory, string destinationDirectory)
        {
            if (!Directory.Exists(sourceDirectory))
                return;

            foreach (var filePath in Directory.EnumerateFiles(sourceDirectory))
            {
                var fileName = Path.GetFileName(filePath);
                if (fileName.Equals("timepilot.db", StringComparison.OrdinalIgnoreCase)
                    || fileName.Equals("timepilot.db-wal", StringComparison.OrdinalIgnoreCase)
                    || fileName.Equals("timepilot.db-shm", StringComparison.OrdinalIgnoreCase)
                    || fileName.Equals(MigrationMarkerFileName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                EnsureNotReparsePoint(filePath);
                File.Copy(filePath, Path.Combine(destinationDirectory, fileName), overwrite: false);
            }

            foreach (var childDirectory in Directory.EnumerateDirectories(sourceDirectory))
            {
                EnsureNotReparsePoint(childDirectory);
                var destinationChild = Path.Combine(
                    destinationDirectory,
                    Path.GetFileName(childDirectory));
                Directory.CreateDirectory(destinationChild);
                CopyDirectoryWithoutDatabase(childDirectory, destinationChild);
            }
        }

        private static void CopyDatabaseSnapshotIfPresent(
            string sourceDirectory,
            string stagingDirectory)
        {
            var sourcePath = Path.Combine(sourceDirectory, "timepilot.db");
            if (!File.Exists(sourcePath))
                return;

            var destinationPath = Path.Combine(stagingDirectory, "timepilot.db");
            using var source = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = sourcePath,
                Mode = SqliteOpenMode.ReadOnly,
                Pooling = false
            }.ToString());
            using var destination = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = destinationPath,
                Pooling = false
            }.ToString());
            source.Open();
            destination.Open();
            source.BackupDatabase(destination);
        }

        private static void ValidateStagingDatabase(
            string sourceDirectory,
            string stagingDirectory)
        {
            var sourceDatabasePath = Path.Combine(sourceDirectory, "timepilot.db");
            if (!File.Exists(sourceDatabasePath))
                return;

            var result = DataStorageLocationService.InspectDatabase(
                Path.Combine(stagingDirectory, "timepilot.db"),
                canInspect: true);
            if (result.State != DataStorageDatabaseState.Valid)
            {
                throw new InvalidDataException(
                    $"The migrated database failed validation: {result.Error ?? result.State.ToString()}.");
            }
        }

        private static void EnsureNotReparsePoint(string path)
        {
            if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            {
                throw new InvalidDataException(
                    $"Storage migration does not follow reparse points: {path}");
            }
        }

        private static void TryDeleteDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path))
                    Directory.Delete(path, recursive: true);
            }
            catch
            {
                // A leftover staging directory is never selected as active storage.
            }
        }
    }

    internal sealed record DataStorageBootstrapResult(
        DataStorageMigrationDecisionKind Decision,
        string SelectedDirectory,
        bool MigrationAttempted,
        bool MigrationSucceeded,
        string? Error)
    {
        public bool RequiresUserAttention => Decision is
            DataStorageMigrationDecisionKind.ConflictRequiresUserChoice
            or DataStorageMigrationDecisionKind.BlockedCurrentDatabaseInvalid
            or DataStorageMigrationDecisionKind.BlockedTargetDatabaseInvalid
            or DataStorageMigrationDecisionKind.InspectionFailed
            or DataStorageMigrationDecisionKind.TargetUnavailable
            || MigrationAttempted && !MigrationSucceeded;
    }

    internal static class DataStorageBootstrapper
    {
        public static DataStorageBootstrapResult Prepare()
        {
            var plan = DataStorageLocationService.Collect();
            var decision = plan.MigrationDecision;
            var selectedDirectory = plan.CurrentDirectory;
            var attempted = false;
            var succeeded = false;
            string? error = null;

            try
            {
                switch (decision.Kind)
                {
                    case DataStorageMigrationDecisionKind.InitializeTarget:
                    case DataStorageMigrationDecisionKind.UseExistingTarget:
                        selectedDirectory = plan.TargetDirectory;
                        break;
                    case DataStorageMigrationDecisionKind.MigrateCurrentToTarget:
                        attempted = true;
                        DataStorageMigrationExecutor.Execute(
                            plan,
                            DateTimeOffset.UtcNow,
                            Application.ProductVersion);
                        selectedDirectory = plan.TargetDirectory;
                        succeeded = true;
                        break;
                }
            }
            catch (Exception ex) when (ex is IOException
                or UnauthorizedAccessException
                or System.Security.SecurityException
                or InvalidDataException
                or InvalidOperationException
                or SqliteException)
            {
                selectedDirectory = plan.CurrentDirectory;
                error = ex.Message;
            }

            AppDataPaths.SetActiveDataDirectory(selectedDirectory);
            return new DataStorageBootstrapResult(
                decision.Kind,
                selectedDirectory,
                attempted,
                succeeded,
                error);
        }
    }
}
