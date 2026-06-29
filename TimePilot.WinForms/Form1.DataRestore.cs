namespace TimePilot.WinForms
{
    public partial class Form1
    {
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
