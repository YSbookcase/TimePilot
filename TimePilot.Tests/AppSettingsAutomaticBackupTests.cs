using TimePilot.WinForms.KYS24;
using Xunit;

namespace TimePilot.Tests
{
    public sealed class AppSettingsAutomaticBackupTests
    {
        [Fact]
        public void AutomaticBackupSettings_RoundTripThroughSettingsFile()
        {
            var root = Path.Combine(Path.GetTempPath(), $"TimePilotSettings-{Guid.NewGuid():N}");
            var settingsPath = Path.Combine(root, "settings.json");
            var backupDirectory = Path.Combine(root, "backups");
            var completedAt = new DateTimeOffset(2026, 9, 22, 10, 30, 0, TimeSpan.Zero);
            var backupPath = Path.Combine(backupDirectory, "backup.zip");

            try
            {
                var settings = AppSettings.Load(settingsPath);
                settings.SetAutomaticBackup(true, backupDirectory, 7);
                settings.RecordAutomaticBackupSuccess(completedAt, backupPath);

                var reloaded = AppSettings.Load(settingsPath);

                Assert.True(reloaded.AutomaticBackupEnabled);
                Assert.Equal(backupDirectory, reloaded.AutomaticBackupDirectory);
                Assert.Equal(7, reloaded.AutomaticBackupRetentionCount);
                Assert.Equal(completedAt, reloaded.AutomaticBackupLastSuccessAt);
                Assert.Equal(backupPath, reloaded.AutomaticBackupLastPath);
                Assert.Null(reloaded.AutomaticBackupLastFailureAt);
                Assert.Null(reloaded.AutomaticBackupLastError);
            }
            finally
            {
                if (Directory.Exists(root))
                    Directory.Delete(root, recursive: true);
            }
        }

        [Fact]
        public void RecordAutomaticBackupFailure_PreservesLastSuccessAndRecordsError()
        {
            var root = Path.Combine(Path.GetTempPath(), $"TimePilotSettings-{Guid.NewGuid():N}");
            var settingsPath = Path.Combine(root, "settings.json");
            var successAt = new DateTimeOffset(2026, 9, 21, 10, 0, 0, TimeSpan.Zero);
            var failureAt = new DateTimeOffset(2026, 9, 22, 10, 0, 0, TimeSpan.Zero);

            try
            {
                var settings = AppSettings.Load(settingsPath);
                settings.RecordAutomaticBackupSuccess(successAt, "previous.zip");
                settings.RecordAutomaticBackupFailure(failureAt, "Drive unavailable");

                var reloaded = AppSettings.Load(settingsPath);

                Assert.Equal(successAt, reloaded.AutomaticBackupLastSuccessAt);
                Assert.Equal("previous.zip", reloaded.AutomaticBackupLastPath);
                Assert.Equal(failureAt, reloaded.AutomaticBackupLastFailureAt);
                Assert.Equal("Drive unavailable", reloaded.AutomaticBackupLastError);
            }
            finally
            {
                if (Directory.Exists(root))
                    Directory.Delete(root, recursive: true);
            }
        }
    }
}
