using Microsoft.Data.Sqlite;
using TimePilot.WinForms.KYS24;
using Xunit;

namespace TimePilot.Tests
{
    public sealed class DataBackupServiceTests
    {
        [Fact]
        public void CreateBackup_CreatesInspectableSnapshotWithSettingsAndLogs()
        {
            var paths = CreateTestPaths();

            try
            {
                CreateDatabase(paths.DatabasePath, "Before backup");
                File.WriteAllText(paths.SettingsPath, "{\"language\":\"ko\"}");
                Directory.CreateDirectory(paths.LogsDirectory);
                File.WriteAllText(Path.Combine(paths.LogsDirectory, "activity.log"), "log entry");

                var createdAt = new DateTimeOffset(2026, 9, 22, 1, 2, 3, TimeSpan.Zero);
                var service = new DataBackupService(paths.DataDirectory);
                var entries = service.CreateBackup(paths.BackupPath, createdAt);
                var plan = service.InspectBackup(paths.BackupPath);

                Assert.Equal(
                    ["metadata.json", "README.txt", "timepilot.db", "settings.json", "logs/activity.log"],
                    entries);
                Assert.True(File.Exists(paths.BackupPath));
                Assert.True(plan.HasDatabase);
                Assert.True(plan.HasSettings);
                Assert.Equal(1, plan.LogCount);
                Assert.Equal(createdAt, plan.CreatedAt);
                Assert.Equal(1, plan.BackupCounts.Apps);
                Assert.Equal(1, plan.BackupCounts.ForegroundSessions);
                Assert.Equal(1, plan.BackupCounts.IdleSessions);
                Assert.Equal(1, plan.BackupCounts.AppRuntimeSessions);
                Assert.Equal(1, plan.BackupCounts.ProcessRuntimeSessions);
                Assert.Equal(1, plan.BackupCounts.SystemEvents);
            }
            finally
            {
                DeleteTestRoot(paths.Root);
            }
        }

        [Fact]
        public void RestoreBackup_RestoresDatabaseSettingsAndLogs()
        {
            var paths = CreateTestPaths();

            try
            {
                CreateDatabase(paths.DatabasePath, "Before backup");
                File.WriteAllText(paths.SettingsPath, "before settings");
                Directory.CreateDirectory(paths.LogsDirectory);
                var logPath = Path.Combine(paths.LogsDirectory, "activity.log");
                File.WriteAllText(logPath, "before log");

                var service = new DataBackupService(paths.DataDirectory);
                service.CreateBackup(paths.BackupPath, DateTimeOffset.UtcNow);

                ReplaceAppName(paths.DatabasePath, "After backup");
                File.WriteAllText(paths.SettingsPath, "after settings");
                File.WriteAllText(logPath, "after log");

                var result = service.RestoreBackup(paths.BackupPath);

                Assert.Equal("Before backup", ReadAppName(paths.DatabasePath));
                Assert.Equal("before settings", File.ReadAllText(paths.SettingsPath));
                Assert.Equal("before log", File.ReadAllText(logPath));
                Assert.Equal(
                    ["timepilot.db", "settings.json", "logs/activity.log"],
                    result.RestoredFiles);
            }
            finally
            {
                DeleteTestRoot(paths.Root);
            }
        }

        [Fact]
        public void InspectBackup_RejectsArchiveWithoutDatabase()
        {
            var paths = CreateTestPaths();

            try
            {
                using (var archive = System.IO.Compression.ZipFile.Open(
                           paths.BackupPath,
                           System.IO.Compression.ZipArchiveMode.Create))
                {
                    archive.CreateEntry("settings.json");
                }

                var service = new DataBackupService(paths.DataDirectory);

                Assert.Throws<InvalidDataException>(() => service.InspectBackup(paths.BackupPath));
            }
            finally
            {
                DeleteTestRoot(paths.Root);
            }
        }

        private static BackupTestPaths CreateTestPaths()
        {
            var root = Path.Combine(Path.GetTempPath(), $"TimePilotBackup-{Guid.NewGuid():N}");
            var dataDirectory = Path.Combine(root, "data");
            Directory.CreateDirectory(dataDirectory);
            return new BackupTestPaths(
                root,
                dataDirectory,
                Path.Combine(dataDirectory, "timepilot.db"),
                Path.Combine(dataDirectory, "settings.json"),
                Path.Combine(dataDirectory, "logs"),
                Path.Combine(root, "backup.zip"));
        }

        private static void CreateDatabase(string path, string appName)
        {
            using var connection = OpenConnection(path);
            using var command = connection.CreateCommand();
            command.CommandText = """
                CREATE TABLE apps (id INTEGER PRIMARY KEY, name TEXT NOT NULL);
                CREATE TABLE foreground_sessions (id INTEGER PRIMARY KEY);
                CREATE TABLE idle_sessions (id INTEGER PRIMARY KEY);
                CREATE TABLE app_runtime_sessions (id INTEGER PRIMARY KEY);
                CREATE TABLE process_runtime_sessions (id INTEGER PRIMARY KEY);
                CREATE TABLE system_events (id INTEGER PRIMARY KEY);

                INSERT INTO apps (name) VALUES ($appName);
                INSERT INTO foreground_sessions DEFAULT VALUES;
                INSERT INTO idle_sessions DEFAULT VALUES;
                INSERT INTO app_runtime_sessions DEFAULT VALUES;
                INSERT INTO process_runtime_sessions DEFAULT VALUES;
                INSERT INTO system_events DEFAULT VALUES;
                """;
            command.Parameters.AddWithValue("$appName", appName);
            command.ExecuteNonQuery();
        }

        private static void ReplaceAppName(string path, string appName)
        {
            using var connection = OpenConnection(path);
            using var command = connection.CreateCommand();
            command.CommandText = "UPDATE apps SET name = $appName;";
            command.Parameters.AddWithValue("$appName", appName);
            command.ExecuteNonQuery();
        }

        private static string ReadAppName(string path)
        {
            using var connection = OpenConnection(path, readOnly: true);
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT name FROM apps LIMIT 1;";
            return Convert.ToString(command.ExecuteScalar())!;
        }

        private static SqliteConnection OpenConnection(string path, bool readOnly = false)
        {
            var connection = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = path,
                Mode = readOnly ? SqliteOpenMode.ReadOnly : SqliteOpenMode.ReadWriteCreate,
                Pooling = false
            }.ToString());
            connection.Open();
            return connection;
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
            string SettingsPath,
            string LogsDirectory,
            string BackupPath);
    }
}
