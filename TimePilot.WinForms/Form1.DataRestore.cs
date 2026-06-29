namespace TimePilot.WinForms
{
    public partial class Form1
    {
        private async void OnRestoreDataBackupMenuItemClick(object? sender, EventArgs e)
        {
            if (isExportRunning)
                return;

            using var dialog = new OpenFileDialog
            {
                AddExtension = true,
                DefaultExt = "zip",
                Filter = UiText.Main.ZipFilter,
                Title = UiText.Main.DataRestoreTitle
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            var service = new DataBackupService();
            DataBackupRestorePlan plan;
            try
            {
                SetExportRunning(true, UiText.Main.DataRestoreAnalyzingBackup);
                await AllowUiToRenderAsync();
                var selectedBackupPath = dialog.FileName;
                plan = await Task.Run(() => service.InspectBackup(selectedBackupPath));
                SetExportRunning(false, null);
            }
            catch (Exception ex)
            {
                SetExportRunning(
                    false,
                    DataOperationStatusFormatter.BuildFailedStatus(UiText.Main.DataRestoreTitle));
                CenteredMessageDialog.Show(
                    this,
                    UiText.Main.DataRestoreFailed(ex.Message),
                    UiText.Main.DataRestoreTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                ClearExportStatus();
                return;
            }

            using var restoreModeChoiceForm = new RestoreModeChoiceForm(
                settings.UiLanguage,
                plan,
                () => service.InspectBackupDetailedComparison(dialog.FileName));
            restoreModeChoiceForm.Icon = Icon;
            if (restoreModeChoiceForm.ShowDialog(this) != DialogResult.OK
                || restoreModeChoiceForm.Choice != RestoreModeChoice.FullReplace)
                return;

            using var safetyBackupChoiceForm =
                new RestoreSafetyBackupChoiceForm(settings.UiLanguage);
            safetyBackupChoiceForm.Icon = Icon;
            if (safetyBackupChoiceForm.ShowDialog(this) != DialogResult.OK
                || safetyBackupChoiceForm.Choice == RestoreSafetyBackupChoice.Cancel)
                return;

            var now = DateTimeOffset.UtcNow;
            string? safetyBackupPath = null;
            using var progressForm = new RestoreProgressForm(settings.UiLanguage, totalSteps: 5);
            progressForm.Icon = Icon;
            progressForm.ShowCentered(this);
            try
            {
                SetExportRunning(true, UiText.Main.DataRestorePreparing);
                progressForm.SetStep(1, UiText.Main.DataRestorePreparing);
                await AllowUiToRenderAsync();
                sampleTimer.Stop();
                EndCurrentTrackingSessions(now, "restore-data");

                if (safetyBackupChoiceForm.Choice
                    == RestoreSafetyBackupChoice.CreateSafetyBackup)
                {
                    SetExportRunning(true, UiText.Main.DataRestoreCreatingSafetyBackup);
                    progressForm.SetStep(2, UiText.Main.DataRestoreCreatingSafetyBackup);
                    await AllowUiToRenderAsync();
                    safetyBackupPath = CreatePreRestoreSafetyBackup(service, now);
                }
                else
                {
                    progressForm.SetStep(
                        2,
                        settings.UiLanguage == UiLanguage.English
                            ? "Safety backup was not created by your choice."
                            : "사용자 선택에 따라 안전 백업은 생성되지 않았습니다.");
                    await AllowUiToRenderAsync();
                }

                storage?.Dispose();
                storage = null;

                SetExportRunning(true, UiText.Main.DataRestoreApplyingBackup);
                progressForm.SetStep(3, UiText.Main.DataRestoreApplyingBackup);
                await AllowUiToRenderAsync();
                var fileName = dialog.FileName;
                var result = await Task.Run(() => service.RestoreBackup(fileName));

                SetExportRunning(true, UiText.Main.DataRestoreRestartingSession);
                progressForm.SetStep(4, UiText.Main.DataRestoreRestartingSession);
                await AllowUiToRenderAsync();
                ReinitializeStorageAfterDataRestore(DateTimeOffset.UtcNow);

                SetExportRunning(
                    false,
                    DataOperationStatusFormatter.BuildCompletedStatus(UiText.Main.DataRestoreTitle));
                progressForm.ShowCompleted(
                    safetyBackupPath is null
                        ? UiText.Main.DataRestoreCompletedWithoutSafetyBackup(
                            result.RestoredFiles.Count)
                        : UiText.Main.DataRestoreCompleted(
                            result.RestoredFiles.Count,
                            safetyBackupPath));
                ClearExportStatus();
                await progressForm.WaitForCloseAsync();
            }
            catch (Exception ex)
            {
                TryReinitializeStorageAfterRestoreFailure();
                SetExportRunning(
                    false,
                    DataOperationStatusFormatter.BuildFailedStatus(UiText.Main.DataRestoreTitle));
                progressForm.ShowFailed(UiText.Main.DataRestoreFailed(ex.Message));
                ClearExportStatus();
                await progressForm.WaitForCloseAsync();
            }
            finally
            {
                if (!isClosing && storage is not null)
                    sampleTimer.Start();
            }
        }

        private static Task AllowUiToRenderAsync()
        {
            Application.DoEvents();
            return Task.Delay(50);
        }

        private static string CreatePreRestoreSafetyBackup(
            DataBackupService service,
            DateTimeOffset now)
        {
            Directory.CreateDirectory(AppDataPaths.BackupDirectory);
            var fileName =
                $"TimePilot-before-restore-{now.ToLocalTime():yyyy-MM-dd-HHmmss}.zip";
            var backupPath = Path.Combine(AppDataPaths.BackupDirectory, fileName);
            service.CreateBackup(backupPath, now);
            return backupPath;
        }

        private void EndCurrentTrackingSessions(
            DateTimeOffset endedAt,
            string shutdownReason)
        {
            idleSessionTracker?.EndCurrentSession(endedAt);
            foregroundSessionTracker?.EndCurrentSession(endedAt);
            lock (processRuntimeTrackingLock)
            {
                processRuntimeSessionTracker?.EndCurrentSessions(endedAt);
            }

            storage?.EndRuntimeSession(endedAt, shutdownReason);
            foregroundSessionTracker = null;
            idleSessionTracker = null;
            processRuntimeSessionTracker = null;
        }

        private void ReinitializeStorageAfterDataRestore(DateTimeOffset startedAt)
        {
            settings = AppSettings.LoadDefault();
            WindowsStartupRegistration.SetEnabled(settings.StartWithWindows);
            UiText.UseLanguage(settings.UiLanguage);
            ApplyUiText();
            ApplySavedTableSortState();
            ApplySavedTableColumnLayouts();
            storage = TimePilotStorage.CreateDefault();
            foregroundSessionTracker = new ForegroundSessionTracker(storage);
            idleSessionTracker = new IdleSessionTracker(storage);
            processRuntimeSessionTracker = new ProcessRuntimeSessionTracker(storage);

            var systemBootedAt = GetCurrentSystemBootedAt(startedAt);
            storage.Initialize(startedAt, systemBootedAt);
            ApplyProcessRuntimeSafeModeIfNeeded();
            UpdateDetailTrackingDisabledBanner();
            storage.BeginRuntimeSession(
                startedAt,
                systemBootedAt,
                Application.ProductVersion);
            RecordWindowsSystemEvent(
                "timepilot-start",
                "ApplicationRestartedAfterRestore");
            lastProcessRuntimeSampleAt = null;
            lastSampleTickAt = null;
            selectedRuntimeAppId = null;
            RefreshViews(startedAt);
        }

        private void TryReinitializeStorageAfterRestoreFailure()
        {
            if (storage is not null)
                return;

            try
            {
                ReinitializeStorageAfterDataRestore(DateTimeOffset.UtcNow);
            }
            catch
            {
            }
        }
    }
}
