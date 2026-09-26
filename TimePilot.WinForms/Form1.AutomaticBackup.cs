using System.Diagnostics;
using TimePilot.WinForms.KYS24;

namespace TimePilot.WinForms
{
    public partial class Form1
    {
        private async Task TryRunAutomaticBackupAsync(bool forceCheck = false)
        {
            var now = DateTimeOffset.UtcNow;
            if (!forceCheck && nextAutomaticBackupCheckAt is { } nextCheck && now < nextCheck)
                return;

            nextAutomaticBackupCheckAt = now + AutomaticBackupCheckInterval;
            if (isClosing
                || isAutomaticBackupRunning
                || isExportRunning
                || !settings.AutomaticBackupEnabled
                || string.IsNullOrWhiteSpace(settings.AutomaticBackupDirectory))
            {
                return;
            }

            isAutomaticBackupRunning = true;
            try
            {
                var backupDirectory = settings.AutomaticBackupDirectory;
                var retentionCount = settings.AutomaticBackupRetentionCount;
                var lastSuccessAt = settings.AutomaticBackupLastSuccessAt;
                var result = await Task.Run(() => new AutomaticBackupService().CreateIfDue(
                    backupDirectory,
                    retentionCount,
                    now,
                    lastSuccessAt));

                if (result.Kind == AutomaticBackupResultKind.Created
                    && result.BackupPath is { } backupPath)
                {
                    settings.RecordAutomaticBackupSuccess(DateTimeOffset.UtcNow, backupPath);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Automatic backup failed: {ex}");
                try
                {
                    settings.RecordAutomaticBackupFailure(DateTimeOffset.UtcNow, ex.Message);
                }
                catch (Exception settingsException)
                {
                    Debug.WriteLine($"Failed to save automatic backup status: {settingsException}");
                }
            }
            finally
            {
                isAutomaticBackupRunning = false;
            }
        }
    }
}
