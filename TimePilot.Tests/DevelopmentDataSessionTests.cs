using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using TimePilot.WinForms.KYS24;
using Xunit;

namespace TimePilot.Tests
{
    public sealed class DevelopmentDataSessionTests
    {
        [Fact]
        public void Parse_NoOverrideLeavesExistingLaunchesUnchanged()
        {
            Assert.Null(DevelopmentDataOptions.Parse([], isPackaged: false));
            Assert.Null(DevelopmentDataOptions.Parse(["--tray"], isPackaged: true));
            Assert.Null(DevelopmentDataOptions.Parse(["--seed-sample-data"], isPackaged: false));
        }

        [Fact]
        public void Parse_ExplicitAbsolutePathAndTrayAreAccepted()
        {
            var directory = Path.Combine(Path.GetTempPath(), "existing data", "..", "selected");
            var options = DevelopmentDataOptions.Parse(["--tray", "--data-dir", directory], false);
            Assert.Equal(Path.GetFullPath(directory), options!.DataDirectory);
        }

        [Fact]
        public void Parse_PackagedOverrideIsRejected()
        {
            Assert.Throws<ArgumentException>(() =>
                DevelopmentDataOptions.Parse(["--data-dir", Path.GetTempPath()], true));
        }

        [Theory]
        [InlineData("--clear-sample-data")]
        [InlineData("--seed-sample-data")]
        [InlineData("--seed-large-sample-data")]
        [InlineData("--check-sample-data")]
        [InlineData("--set-ui-language")]
        [InlineData("--shutdown")]
        [InlineData("--remove-startup-registration")]
        [InlineData("--unknown")]
        public void Parse_RejectsMaintenanceAndUnknownCommands(string command)
        {
            Assert.Throws<ArgumentException>(() =>
                DevelopmentDataOptions.Parse(["--data-dir", Path.GetTempPath(), command], false));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        public void Parse_RejectsInvalidAndDuplicatePaths(int scenario)
        {
            string[] args = scenario switch
            {
                0 => ["--data-dir"],
                1 => ["--data-dir", "relative-folder"],
                2 => ["--data-dir", " "],
                3 => ["--data-dir", Path.GetTempPath(), "--data-dir", Path.GetTempPath()],
                _ => ["--data-dir=" + Path.GetTempPath()]
            };
            Assert.Throws<ArgumentException>(() => DevelopmentDataOptions.Parse(args, false));
        }

        [Fact]
        public void Prepare_CreatesVerifiedSnapshotWithoutChangingSourceOrActiveDirectory()
        {
            WithData((data, backups) =>
            {
                var databasePath = Path.Combine(data, "timepilot.db");
                var settingsPath = Path.Combine(data, "settings.json");
                var databaseHash = SHA256.HashData(File.ReadAllBytes(databasePath));
                var settingsText = File.ReadAllText(settingsPath);
                var previousDirectory = AppDataPaths.DataDirectory;

                var first = DevelopmentDataSession.Prepare(data, backups);
                var second = DevelopmentDataSession.Prepare(data, backups);

                Assert.Equal(data, first.DataDirectory);
                Assert.NotEqual(first.SafetyBackupPath, second.SafetyBackupPath);
                Assert.Equal(2, Directory.GetFiles(backups, "*.zip").Length);
                var inspection = new DataBackupService(data).InspectBackup(first.SafetyBackupPath);
                Assert.True(inspection.HasDatabase);
                Assert.True(inspection.HasSettings);
                using var archive = ZipFile.OpenRead(first.SafetyBackupPath);
                using var reader = new StreamReader(archive.GetEntry("settings.json")!.Open());
                Assert.Equal(settingsText, reader.ReadToEnd());
                Assert.Equal(databaseHash, SHA256.HashData(File.ReadAllBytes(databasePath)));
                Assert.Equal(settingsText, File.ReadAllText(settingsPath));
                Assert.Equal(previousDirectory, AppDataPaths.DataDirectory);
            });
        }

        [Fact]
        public void Prepare_MissingFolderDoesNotCreateDataOrFallback()
        {
            var missing = Path.Combine(Path.GetTempPath(), $"TimePilotMissing-{Guid.NewGuid():N}");
            Assert.Throws<InvalidDataException>(() =>
                DevelopmentDataSession.Prepare(missing, Path.Combine(missing, "backups")));
            Assert.False(Directory.Exists(missing));
        }

        [Theory]
        [InlineData("timepilot.db")]
        [InlineData("settings.json")]
        public void Prepare_MissingRequiredFileDoesNotStartOrCreateBackup(string name)
        {
            WithData((data, backups) =>
            {
                File.Delete(Path.Combine(data, name));
                Assert.Throws<InvalidDataException>(() => DevelopmentDataSession.Prepare(data, backups));
                Assert.False(Directory.Exists(backups));
                Assert.False(File.Exists(Path.Combine(data, name)));
            });
        }

        [Theory]
        [InlineData("broken json")]
        [InlineData("[]")]
        [InlineData("{\"IdleThresholdMinutes\":\"invalid\"}")]
        public void Prepare_InvalidSettingsDoNotFallBackToDefaults(string json)
        {
            WithData((data, backups) =>
            {
                var settingsPath = Path.Combine(data, "settings.json");
                File.WriteAllText(settingsPath, json);
                Assert.Throws<JsonException>(() => DevelopmentDataSession.Prepare(data, backups));
                Assert.False(Directory.Exists(backups));
                Assert.Equal(json, File.ReadAllText(settingsPath));
            });
        }

        [Fact]
        public void Prepare_InvalidDatabaseFailsBeforeWritingSource()
        {
            WithData((data, backups) =>
            {
                var databasePath = Path.Combine(data, "timepilot.db");
                File.WriteAllText(databasePath, "not a database");
                Assert.Throws<SqliteException>(() => DevelopmentDataSession.Prepare(data, backups));
                Assert.Equal("not a database", File.ReadAllText(databasePath));
                Assert.Empty(Directory.GetFiles(backups, "*.zip"));
            });
        }

        [Fact]
        public void Prepare_BackupFailureLeavesSelectedDataUnchanged()
        {
            WithData((data, backups) =>
            {
                var previousDirectory = AppDataPaths.DataDirectory;
                var settingsPath = Path.Combine(data, "settings.json");
                var settingsText = File.ReadAllText(settingsPath);
                File.WriteAllText(backups, "folder blocked by file");
                Assert.Throws<IOException>(() => DevelopmentDataSession.Prepare(data, backups));
                Assert.Equal(settingsText, File.ReadAllText(settingsPath));
                Assert.Equal(previousDirectory, AppDataPaths.DataDirectory);
            });
        }

        private static void WithData(Action<string, string> test)
        {
            var root = Path.Combine(Path.GetTempPath(), $"TimePilotDevData-{Guid.NewGuid():N}");
            var data = Path.Combine(root, "data");
            var backups = Path.Combine(root, "external-backups");
            Directory.CreateDirectory(data);
            try
            {
                var now = DateTimeOffset.UtcNow;
                using (var storage = new TimePilotStorage(Path.Combine(data, "timepilot.db")))
                    storage.Initialize(now, now.AddHours(-1));
                SqliteConnection.ClearAllPools();
                AppSettings.Load(Path.Combine(data, "settings.json")).Save();
                test(data, backups);
            }
            finally
            {
                SqliteConnection.ClearAllPools();
                Directory.Delete(root, recursive: true);
            }
        }
    }
}
