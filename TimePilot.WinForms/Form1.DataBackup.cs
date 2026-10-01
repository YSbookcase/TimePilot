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
                FileName = $"ActiveLogbook-backup-{now.ToLocalTime():yyyy-MM-dd-HHmm}.zip",
                Filter = UiText.Main.ZipFilter,
                OverwritePrompt = true,
                Title = UiText.Main.DataBackupTitle
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            var wasTimerEnabled = sampleTimer.Enabled;
            using var progressForm = new OperationProgressForm(
                UiText.Main.DataBackupTitle,
                BuildManualBackupProgressStatus(DataBackupProgressStage.Preparing));
            using var progressDelayCancellation = new CancellationTokenSource();
            var progressDisplayTask = ShowManualBackupProgressAfterDelayAsync(
                progressForm,
                progressDelayCancellation.Token);
            try
            {
                SetExportRunning(
                    true,
                    BuildManualBackupProgressStatus(DataBackupProgressStage.Preparing));
                sampleTimer.Stop();
                storage.UpdateRuntimeHeartbeat(now);

                var fileName = dialog.FileName;
                var progress = new Progress<DataBackupProgressStage>(stage =>
                {
                    var status = BuildManualBackupProgressStatus(stage);
                    SetExportRunning(true, status);
                    progressForm.SetStatus(status);
                });
                var entries = await Task.Run(() =>
                {
                    var service = new DataBackupService();
                    return service.CreateBackup(fileName, now, progress);
                });

                progressDelayCancellation.Cancel();
                await progressDisplayTask;
                progressForm.Close();
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
                progressDelayCancellation.Cancel();
                await progressDisplayTask;
                progressForm.Close();
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
                progressDelayCancellation.Cancel();
                await progressDisplayTask;
                progressForm.Close();
                if (wasTimerEnabled && !isClosing)
                    sampleTimer.Start();
            }
        }

        private async Task ShowManualBackupProgressAfterDelayAsync(
            OperationProgressForm progressForm,
            CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMilliseconds(400), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (!isClosing && !progressForm.IsDisposed)
                progressForm.ShowCentered(this);
        }

        private static string BuildManualBackupProgressStatus(DataBackupProgressStage stage)
        {
            if (UiText.CurrentLanguage == UiLanguage.English)
            {
                return stage switch
                {
                    DataBackupProgressStage.CopyingDatabase => "Creating a database snapshot...",
                    DataBackupProgressStage.CompressingFiles => "Compressing backup files...",
                    DataBackupProgressStage.VerifyingBackup => "Verifying the backup...",
                    DataBackupProgressStage.Finalizing => "Finishing the backup...",
                    _ => "Preparing the backup..."
                };
            }

            return stage switch
            {
                DataBackupProgressStage.CopyingDatabase => "데이터베이스 복사본을 만드는 중...",
                DataBackupProgressStage.CompressingFiles => "백업 파일을 압축하는 중...",
                DataBackupProgressStage.VerifyingBackup => "백업 파일을 확인하는 중...",
                DataBackupProgressStage.Finalizing => "백업을 마무리하는 중...",
                _ => "백업을 준비하는 중..."
            };
        }
    }
}
