using System.Diagnostics;

namespace TimePilot.WinForms;

public partial class Form1
{
    private void ShowBackupRemovalDialog(Form owner)
    {
        if (storage is null || isBackupRemovalOpen || isExportRunning || isAutomaticBackupRunning)
        {
            CenteredMessageDialog.Show(owner, new BackupRemovalText(UiText.CurrentLanguage).Busy,
                UiText.Preferences.Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        isBackupRemovalOpen = true;
        try
        {
            // Preferences remain unsaved; this is a separate explicit backup operation.
            using var dialog = new BackupRemovalForm(BackupRemovalPreparation.ForCurrentApp(), UiText.CurrentLanguage,
                settings.AutomaticBackupDirectory,
                settings.LastExternalBackupAt ?? settings.AutomaticBackupLastSuccessAt,
                settings.LastExternalBackupPath ?? settings.AutomaticBackupLastPath,
                receipt => settings.RecordExternalBackupSuccess(DateTimeOffset.UtcNow, receipt.Path));
            dialog.ShowDialog(owner);
        }
        finally
        {
            isBackupRemovalOpen = false;
        }
    }

    private void TryRecordExternalBackup(string path, DateTimeOffset completedAt)
    {
        try
        {
            BackupRemovalPreparation.ForCurrentApp().ValidateDestination(path, requireNew: false);
            settings.RecordExternalBackupSuccess(completedAt, path);
        }
        catch (Exception ex)
        {
            // The backup itself succeeded even if it is internal or history cannot be persisted.
            Debug.WriteLine($"External backup history not updated: {ex}");
        }
    }
}
