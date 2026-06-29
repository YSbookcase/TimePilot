namespace TimePilot.WinForms
{
    public partial class Form1
    {
        private async void OnCreateDataBackupMenuItemClick(object? sender, EventArgs e)
        {
            if (storage is null || isExportRunning)
                return;

            var confirm = CenteredMessageDialog.Show(
                this,
                UiText.Main.DataBackupWarning,
                UiText.Main.DataBackupTitle,
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Information);
            if (confirm != DialogResult.OK)
                return;

            var now = DateTimeOffset.UtcNow;
            using var dialog = new SaveFileDialog
            {
                AddExtension = true,
                DefaultExt = "zip",
                FileName = $"TimePilot-backup-{now.ToLocalTime():yyyy-MM-dd-HHmm}.zip",
                Filter = UiText.Main.ZipFilter,
                OverwritePrompt = true,
                Title = UiText.Main.DataBackupTitle
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            var wasTimerEnabled = sampleTimer.Enabled;
            try
            {
                SetExportRunning(
                    true,
                    DataOperationStatusFormatter.BuildInProgressStatus(UiText.Main.DataBackupTitle));
                sampleTimer.Stop();
                storage.UpdateRuntimeHeartbeat(now);

                var fileName = dialog.FileName;
                var entries = await Task.Run(() =>
                {
                    var service = new DataBackupService();
                    return service.CreateBackup(fileName, now);
                });

                SetExportRunning(
                    false,
                    DataOperationStatusFormatter.BuildCompletedStatus(UiText.Main.DataBackupTitle));
                CenteredMessageDialog.Show(
                    this,
                    UiText.Main.DataBackupCompleted(dialog.FileName, entries.Count),
                    UiText.Main.DataBackupTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                ClearExportStatus();
            }
            catch (Exception ex)
            {
                SetExportRunning(
                    false,
                    DataOperationStatusFormatter.BuildFailedStatus(UiText.Main.DataBackupTitle));
                CenteredMessageDialog.Show(
                    this,
                    UiText.Main.DataBackupFailed(ex.Message),
                    UiText.Main.DataBackupTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                ClearExportStatus();
            }
            finally
            {
                if (wasTimerEnabled && !isClosing)
                    sampleTimer.Start();
            }
        }
    }
}
