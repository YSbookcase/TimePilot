using Microsoft.Data.Sqlite;
using TimePilot.WinForms.KYS24;
using Xunit;

namespace TimePilot.Tests
{
    public sealed class AutomaticBackupServiceTests
    {
        [Fact]
        public void CreateIfDue_CreatesValidatedBackupAndAppliesRetentionPolicy()
        {
            var paths = CreateTestPaths();

            try
            {
                CreateValidDatabase(paths.DatabasePath);
                File.WriteAllText(Path.Combine(paths.DataDirectory, "settings.json"), "{}");
                Directory.CreateDirectory(paths.BackupDirectory);
                CreateOldAutomaticBackup(paths.BackupDirectory, "2026-09-19-010000", 3);
                CreateOldAutomaticBackup(paths.BackupDirectory, "2026-09-20-010000", 2);
                CreateOldAutomaticBackup(paths.BackupDirectory, "2026-09-21-010000", 1);
                var manualBackup = Path.Combine(paths.BackupDirectory, "manual-backup.zip");
                File.WriteAllText(manualBackup, "keep");

                var service = new AutomaticBackupService(paths.DataDirectory);
                var result = service.CreateIfDue(
                    paths.BackupDirectory,
                    retentionCount: 2,
                    new DateTimeOffset(2026, 9, 22, 12, 0, 0, TimeSpan.Zero),
                    lastSuccessAt: null);

                Assert.Equal(AutomaticBackupResultKind.Created, result.Kind);
                Assert.NotNull(result.BackupPath);
                Assert.True(File.Exists(result.BackupPath));
                Assert.Equal(2, result.DeletedBackupCount);
                Assert.Equal(
                    2,
                    Directory.EnumerateFiles(
                        paths.BackupDirectory,
                        $"{AutomaticBackupService.FileNamePrefix}*.zip").Count());
                Assert.True(File.Exists(manualBackup));
                Assert.True(new DataBackupService(paths.DataDirectory)
                    .InspectBackup(result.BackupPath!).HasDatabase);
            }
            finally
            {
                DeleteTestRoot(paths.Root);
            }
        }

        [Fact]
        public void CreateIfDue_SkipsWhenBackupWasAlreadyCreatedToday()
        {
            var paths = CreateTestPaths();

            try
            {
                CreateValidDatabase(paths.DatabasePath);
                File.SetLastWriteTimeUtc(paths.DatabasePath, new DateTime(2026, 9, 22, 11, 0, 0, DateTimeKind.Utc));
                var service = new AutomaticBackupService(paths.DataDirectory);

                var result = service.CreateIfDue(
                    paths.BackupDirectory,
                    retentionCount: 14,
                    new DateTimeOffset(2026, 9, 22, 12, 0, 0, TimeSpan.Zero),
                    new DateTimeOffset(2026, 9, 22, 10, 0, 0, TimeSpan.Zero));

                Assert.Equal(AutomaticBackupResultKind.SkippedAlreadyCreatedToday, result.Kind);
                Assert.False(Directory.Exists(paths.BackupDirectory));
            }
            finally
            {
                DeleteTestRoot(paths.Root);
            }
        }

        [Fact]
        public void CreateIfDue_SkipsWhenDatabaseHasNotChangedSinceLastBackup()
        {
            var paths = CreateTestPaths();

            try
            {
                CreateValidDatabase(paths.DatabasePath);
                File.SetLastWriteTimeUtc(paths.DatabasePath, new DateTime(2026, 9, 21, 9, 0, 0, DateTimeKind.Utc));
                var service = new AutomaticBackupService(paths.DataDirectory);

                var result = service.CreateIfDue(
                    paths.BackupDirectory,
                    retentionCount: 14,
                    new DateTimeOffset(2026, 9, 22, 12, 0, 0, TimeSpan.Zero),
                    new DateTimeOffset(2026, 9, 21, 10, 0, 0, TimeSpan.Zero));

                Assert.Equal(AutomaticBackupResultKind.SkippedUnchanged, result.Kind);
                Assert.False(Directory.Exists(paths.BackupDirectory));
            }
            finally
            {
                DeleteTestRoot(paths.Root);
            }
        }

        [Fact]
        public void CreateIfDue_RejectsBackupFolderInsideApplicationData()
        {
            var paths = CreateTestPaths();

            try
            {
                CreateValidDatabase(paths.DatabasePath);
                var service = new AutomaticBackupService(paths.DataDirectory);

                Assert.Throws<InvalidOperationException>(() => service.CreateIfDue(
                    Path.Combine(paths.DataDirectory, "backups"),
                    retentionCount: 14,
                    DateTimeOffset.UtcNow,
                    lastSuccessAt: null));
            }
            finally
            {
                DeleteTestRoot(paths.Root);
            }
        }

        [Fact]
        public void CreateIfDue_ReportsFailureWhenBackupLocationCannotBeCreated()
        {
            var paths = CreateTestPaths();

            try
            {
                CreateValidDatabase(paths.DatabasePath);
                var filePath = Path.Combine(paths.Root, "not-a-directory");
                File.WriteAllText(filePath, "occupied");
                var service = new AutomaticBackupService(paths.DataDirectory);

                Assert.ThrowsAny<IOException>(() => service.CreateIfDue(
                    filePath,
                    retentionCount: 14,
                    DateTimeOffset.UtcNow,
                    lastSuccessAt: null));
            }
            finally
            {
                DeleteTestRoot(paths.Root);
            }
        }

        private static BackupTestPaths CreateTestPaths()
        {
            var root = Path.Combine(Path.GetTempPath(), $"TimePilotAutomaticBackup-{Guid.NewGuid():N}");
            var dataDirectory = Path.Combine(root, "data");
            Directory.CreateDirectory(dataDirectory);
            return new BackupTestPaths(
                root,
                dataDirectory,
                Path.Combine(dataDirectory, "timepilot.db"),
                Path.Combine(root, "external-backups"));
        }

        private static void CreateValidDatabase(string path)
        {
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
                CREATE TABLE idle_sessions (id INTEGER PRIMARY KEY);
                CREATE TABLE app_runtime_sessions (id INTEGER PRIMARY KEY);
                CREATE TABLE process_runtime_sessions (id INTEGER PRIMARY KEY);
                INSERT INTO apps (name) VALUES ('ActiveLogbook');
                """;
            command.ExecuteNonQuery();
        }

        private static void CreateOldAutomaticBackup(string directory, string timestamp, int ageDays)
        {
            var path = Path.Combine(directory, $"{AutomaticBackupService.FileNamePrefix}{timestamp}.zip");
            File.WriteAllText(path, "old");
            File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddDays(-ageDays));
        }

        private static void DeleteTestRoot(string root)
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }

        private sealed record BackupTestPaths(
            string Root,
            string DataDirectory,
            string DatabasePath,
            string BackupDirectory);
    }
}
