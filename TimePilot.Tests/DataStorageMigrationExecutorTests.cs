using Microsoft.Data.Sqlite;
using TimePilot.WinForms.KYS24;
using Xunit;

namespace TimePilot.Tests
{
    public sealed class DataStorageMigrationExecutorTests
    {
        [Fact]
        public void Execute_MigratesValidatedSnapshotAndKeepsSource()
        {
            var paths = CreateTestPaths();

            try
            {
                CreateValidDatabase(Path.Combine(paths.CurrentDirectory, "timepilot.db"));
                File.WriteAllText(Path.Combine(paths.CurrentDirectory, "settings.json"), "{}");
                var backupDirectory = Path.Combine(paths.CurrentDirectory, "backups");
                Directory.CreateDirectory(backupDirectory);
                File.WriteAllText(Path.Combine(backupDirectory, "backup.txt"), "backup");

                var plan = BuildPlan(paths);
                Assert.Equal(
                    DataStorageMigrationDecisionKind.MigrateCurrentToTarget,
                    plan.MigrationDecision.Kind);

                var result = DataStorageMigrationExecutor.Execute(
                    plan,
                    new DateTimeOffset(2026, 9, 18, 0, 0, 0, TimeSpan.Zero),
                    "0.2.12");

                Assert.True(File.Exists(Path.Combine(paths.CurrentDirectory, "timepilot.db")));
                Assert.True(File.Exists(Path.Combine(paths.CurrentDirectory, "settings.json")));
                Assert.True(File.Exists(Path.Combine(paths.TargetDirectory, "timepilot.db")));
                Assert.True(File.Exists(Path.Combine(paths.TargetDirectory, "settings.json")));
                Assert.True(File.Exists(Path.Combine(paths.TargetDirectory, "backups", "backup.txt")));
                Assert.True(File.Exists(result.MarkerPath));
                Assert.True(DataStorageLocationService.HasValidMigrationMarker(
                    paths.CurrentDirectory,
                    paths.TargetDirectory));
                Assert.Equal(
                    DataStorageDatabaseState.Valid,
                    DataStorageLocationService.InspectDatabase(
                        Path.Combine(paths.TargetDirectory, "timepilot.db"),
                        canInspect: true).State);
                Assert.Equal(1, ReadAppCount(Path.Combine(paths.TargetDirectory, "timepilot.db")));
            }
            finally
            {
                DeleteTestRoot(paths.Root);
            }
        }

        [Fact]
        public void BuildPlan_AfterCompletedMigrationUsesTargetWithoutMigratingAgain()
        {
            var paths = CreateTestPaths();

            try
            {
                CreateValidDatabase(Path.Combine(paths.CurrentDirectory, "timepilot.db"));
                var initialPlan = BuildPlan(paths);
                DataStorageMigrationExecutor.Execute(
                    initialPlan,
                    DateTimeOffset.UtcNow,
                    "0.2.12");

                var nextPlan = BuildPlan(paths);

                Assert.True(nextPlan.HasCompletedMigration);
                Assert.Equal(
                    DataStorageMigrationDecisionKind.UseExistingTarget,
                    nextPlan.MigrationDecision.Kind);
            }
            finally
            {
                DeleteTestRoot(paths.Root);
            }
        }

        [Fact]
        public void Execute_RefusesNonEmptyTargetAndPreservesBothLocations()
        {
            var paths = CreateTestPaths();

            try
            {
                CreateValidDatabase(Path.Combine(paths.CurrentDirectory, "timepilot.db"));
                Directory.CreateDirectory(paths.TargetDirectory);
                var existingTargetFile = Path.Combine(paths.TargetDirectory, "keep.txt");
                File.WriteAllText(existingTargetFile, "keep");
                var plan = BuildPlan(paths);
                Assert.Equal(
                    DataStorageMigrationDecisionKind.MigrateCurrentToTarget,
                    plan.MigrationDecision.Kind);

                var exception = Assert.Throws<InvalidOperationException>(() =>
                    DataStorageMigrationExecutor.Execute(
                        plan,
                        DateTimeOffset.UtcNow,
                        "0.2.12"));

                Assert.Contains("not empty", exception.Message);
                Assert.True(File.Exists(Path.Combine(paths.CurrentDirectory, "timepilot.db")));
                Assert.Equal("keep", File.ReadAllText(existingTargetFile));
                Assert.False(File.Exists(Path.Combine(
                    paths.TargetDirectory,
                    DataStorageMigrationExecutor.MigrationMarkerFileName)));
            }
            finally
            {
                DeleteTestRoot(paths.Root);
            }
        }

        private static StorageTestPaths CreateTestPaths()
        {
            var root = Path.Combine(
                Path.GetTempPath(),
                $"TimePilotStorageMigration-{Guid.NewGuid():N}");
            var localCache = Path.Combine(root, "Package", "LocalCache");
            var localState = Path.Combine(root, "Package", "LocalState");
            var currentDirectory = Path.Combine(localCache, "Local", "TimePilot");
            var targetDirectory = Path.Combine(localState, "TimePilot");
            Directory.CreateDirectory(currentDirectory);
            return new StorageTestPaths(
                root,
                Path.Combine(root, "Legacy", "TimePilot"),
                localCache,
                localState,
                currentDirectory,
                targetDirectory);
        }

        private static DataStorageLocationPlan BuildPlan(StorageTestPaths paths)
        {
            return DataStorageLocationService.BuildPlan(
                isPackaged: true,
                paths.LegacyDirectory,
                paths.LocalCache,
                paths.LocalState);
        }

        private static void CreateValidDatabase(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = path,
                Pooling = false
            }.ToString());
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = """
                CREATE TABLE apps (id INTEGER PRIMARY KEY, name TEXT NOT NULL);
                CREATE TABLE foreground_sessions (id INTEGER PRIMARY KEY);
                INSERT INTO apps (name) VALUES ('Codex');
                """;
            command.ExecuteNonQuery();
        }

        private static int ReadAppCount(string path)
        {
            using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = path,
                Mode = SqliteOpenMode.ReadOnly,
                Pooling = false
            }.ToString());
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM apps;";
            return Convert.ToInt32(command.ExecuteScalar());
        }

        private static void DeleteTestRoot(string root)
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }

        private sealed record StorageTestPaths(
            string Root,
            string LegacyDirectory,
            string LocalCache,
            string LocalState,
            string CurrentDirectory,
            string TargetDirectory);
    }
}
