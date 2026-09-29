using System.Diagnostics;
using TimePilot.WinForms.KYS24;
using TimePilot.WinForms.Timeline;

namespace TimePilot.WinForms
{
    public partial class Form1
    {
        private async void OnPreferencesMenuItemClick(object? sender, EventArgs e)
        {
            await ShowPreferencesDialogAsync();
        }

        private async Task ShowPreferencesDialogAsync()
        {
            bool? effectiveStartupEnabled = null;
            try
            {
                var state = await WindowsStartupRegistration.GetPackagedStateAsync();
                if (state.HasValue)
                {
                    effectiveStartupEnabled = state.Value is Windows.ApplicationModel.StartupTaskState.Enabled
                        or Windows.ApplicationModel.StartupTaskState.EnabledByPolicy;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to read startup task state: {ex}");
            }

            using var form = new PreferencesForm(settings, effectiveStartupEnabled);
            if (form.ShowDialog(this) != DialogResult.OK)
                return;

            settings.SetIdleThresholdMinutes(form.IdleThresholdMinutes);
            var languageChanged = settings.UiLanguage != form.UiLanguage;
            settings.SetUiLanguage(form.UiLanguage);
            if (languageChanged)
            {
                UiText.UseLanguage(settings.UiLanguage);
                ApplyUiText();
            }

            if (form.StartupDisplayMode != settings.StartupDisplayMode)
                settings.SetStartupDisplayMode(form.StartupDisplayMode);

            if (form.StartWithWindows != settings.StartWithWindows
                || effectiveStartupEnabled.HasValue
                    && form.StartWithWindows != effectiveStartupEnabled.Value)
            {
                try
                {
                    await settings.SetStartWithWindowsAsync(form.StartWithWindows);
                }
                catch (Exception ex)
                {
                    var reason = settings.UiLanguage == UiLanguage.Korean && ex is StartupTaskStateException blocked
                        ? blocked.State switch
                        {
                            Windows.ApplicationModel.StartupTaskState.DisabledByPolicy =>
                                "Windows 정책이 이 앱의 자동 시작을 차단하고 있습니다. 이 PC의 시작 앱 정책을 확인하세요.",
                            Windows.ApplicationModel.StartupTaskState.DisabledByUser =>
                                "Windows 시작 앱에서 이 앱이 꺼져 있습니다. Windows 시작 앱 설정에서 다시 켜세요.",
                            Windows.ApplicationModel.StartupTaskState.EnabledByPolicy =>
                                "Windows 정책이 이 앱의 자동 시작을 요구하고 있어 앱에서 끌 수 없습니다.",
                            _ => ex.Message
                        }
                        : ex.Message;
                    var message = settings.UiLanguage == UiLanguage.English
                        ? $"Windows could not update the startup setting.\n\n{reason}"
                        : $"Windows 시작 앱 설정을 변경하지 못했습니다.\n\n{reason}";
                    CenteredMessageDialog.Show(
                        this,
                        message,
                        UiText.Preferences.Title,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }
            settings.SetPerformanceDiagnosticsEnabled(
                form.PerformanceDiagnosticsEnabled);
            settings.SetAutomaticBackup(
                form.AutomaticBackupEnabled,
                form.AutomaticBackupDirectory,
                form.AutomaticBackupRetentionCount);
            nextAutomaticBackupCheckAt = null;
            if (settings.AutomaticBackupEnabled && !form.ClearUsageDataRequested)
                _ = TryRunAutomaticBackupAsync(forceCheck: true);

            if (!settings.PerformanceDiagnosticsEnabled)
            {
                performanceStatusText = null;
                performanceStatusExpiresAt = null;
                RefreshStatusLabel();
            }

            settings.SetProcessRuntimeTracking(
                form.ProcessRuntimeTrackingEnabled,
                form.ProcessRuntimeTrackingScope,
                form.ProcessRuntimeSampleIntervalSeconds,
                form.ProcessRuntimeRiskAccepted);
            lastProcessRuntimeSampleAt = null;
            UpdateDetailTrackingDisabledBanner();

            if (form.ClearUsageDataRequested)
            {
                await ClearUsageDataAsync();
            }
            else
            {
                RefreshViews(DateTimeOffset.UtcNow);
            }
        }

        private async Task ClearUsageDataAsync()
        {
            if (storage is null)
                return;

            var now = DateTimeOffset.UtcNow;
            var storageSnapshot = storage;
            var wasTimerEnabled = sampleTimer.Enabled;
            var clearProgress = new UsageDataClearProgress();
            var appVersion = Application.ProductVersion;
            var totalStopwatch = Stopwatch.StartNew();
            long preparationElapsedMs = 0;
            long deleteElapsedMs = 0;
            long finalizeElapsedMs = 0;
            Exception? clearError = null;
            var initialStatus = BuildUsageDataClearStatus(
                "finishing current activity",
                "진행 중인 작업 정리");
            using var progressForm = new OperationProgressForm(
                UiText.Preferences.ClearUsageDataTitle,
                initialStatus);
            isUsageDataClearRunning = true;
            sampleTimer.Stop();
            viewRefreshGeneration.Invalidate();
            viewRefreshCache.Clear();

            try
            {
                progressForm.ShowCentered(this);
                Enabled = false;
                SetExportRunning(true, initialStatus);
                await AllowUiToRenderAsync();

                var preparationStopwatch = Stopwatch.StartNew();
                await WaitForUsageClearPreconditionsAsync();
                preparationStopwatch.Stop();
                preparationElapsedMs = preparationStopwatch.ElapsedMilliseconds;

                var deleteStatus = BuildUsageDataClearStatus(
                    "deleting stored records",
                    "저장된 기록 삭제");
                SetExportRunning(true, deleteStatus);
                progressForm.SetStatus(deleteStatus);
                await AllowUiToRenderAsync();

                var deleteStopwatch = Stopwatch.StartNew();
                await Task.Run(() =>
                    ClearUsageStorage(storageSnapshot, now, appVersion, clearProgress));
                deleteStopwatch.Stop();
                deleteElapsedMs = deleteStopwatch.ElapsedMilliseconds;

                var finalizeStatus = BuildUsageDataClearStatus(
                    "updating the screen",
                    "화면 마무리");
                SetExportRunning(true, finalizeStatus);
                progressForm.SetStatus(finalizeStatus);
                await AllowUiToRenderAsync();

                var finalizeStopwatch = Stopwatch.StartNew();
                ResetTrackingAfterUsageClear(storageSnapshot);
                ClearUsageDataViews();
                finalizeStopwatch.Stop();
                finalizeElapsedMs = finalizeStopwatch.ElapsedMilliseconds;
                totalStopwatch.Stop();
                ReportPerformanceTimings(
                    ("clear-prepare", preparationElapsedMs),
                    ("clear-delete", deleteElapsedMs),
                    ("clear-finalize", finalizeElapsedMs),
                    ("clear-total", totalStopwatch.ElapsedMilliseconds));
                SetStatusText(UiText.Main.UsageDataCleared);
            }
            catch (Exception ex)
            {
                if (clearProgress.IsRuntimeSessionEnded)
                {
                    TryRestartTrackingAfterClearFailure(
                        storageSnapshot,
                        beginRuntimeSession: !clearProgress.IsRuntimeSessionRestarted);
                }
                clearError = ex;
            }
            finally
            {
                isUsageDataClearRunning = false;
                SetExportRunning(false, null);
                if (wasTimerEnabled && !isClosing)
                    sampleTimer.Start();

                Enabled = true;
                progressForm.Close();
                if (!isClosing)
                    Activate();
            }

            if (isClosing)
                return;

            if (clearError is null)
            {
                CenteredMessageDialog.Show(
                    this,
                    UiText.Main.UsageDataCleared,
                    UiText.Preferences.ClearUsageDataTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            var message = settings.UiLanguage == UiLanguage.English
                ? $"Could not delete usage records.\n\n{clearError.Message}"
                : $"사용 기록을 삭제하지 못했습니다.\n\n{clearError.Message}";
            CenteredMessageDialog.Show(
                this,
                message,
                UiText.Preferences.ClearUsageDataTitle,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        private async Task WaitForUsageClearPreconditionsAsync()
        {
            while (isViewRefreshRunning || isProcessRuntimeSampleRunning)
                await Task.Delay(20);
        }

        private static string BuildUsageDataClearStatus(
            string englishStage,
            string koreanStage)
        {
            return UiText.CurrentLanguage == UiLanguage.English
                ? $"Deleting usage records: {englishStage}..."
                : $"사용 기록 삭제: {koreanStage} 중...";
        }

        private void ClearUsageDataViews()
        {
            selectedRuntimeAppId = null;
            SuspendLayout();
            try
            {
                usageGrid.DataSource = Array.Empty<UsageSummaryRow>();
                dailyUsageTrendGrid.DataSource = Array.Empty<DailyUsageTrendRow>();
                timelineGrid.DataSource = Array.Empty<ActivityTimelineRow>();
                runtimeGrid.DataSource = Array.Empty<ProcessRuntimeSummaryRow>();
                runtimeSegmentsGrid.DataSource = Array.Empty<ProcessRuntimeSegmentRow>();
                SetSummaryOverview(Array.Empty<UsageSummaryRow>());
                SetSummaryUsageBars(Array.Empty<UsageSummaryRow>());
                SetSummaryIdleAnalysis(Array.Empty<ForegroundUsageSummary>(), null);
                SetRuntimeCoverageSummary(null);
                currentTimelineForegroundUsage = Array.Empty<ForegroundUsageSummary>();
                currentTimelineRows = Array.Empty<ActivityTimelineRow>();
                currentTimelineWindowsRuntimeRanges = Array.Empty<TimelineRange>();
                currentTimelineSystemRanges = Array.Empty<SystemTimelineRange>();
                currentTimelineSystemEvents = Array.Empty<SystemTimelineEvent>();
                timelineOverviewControl.SetTimeline(
                    selectedTimelineDate,
                    currentTimelineRows,
                    currentTimelineWindowsRuntimeRanges,
                    currentTimelineSystemRanges,
                    currentTimelineSystemEvents,
                    Array.Empty<CategoryTimelineSegment>());
            }
            finally
            {
                ResumeLayout(true);
            }
        }

        private void ClearUsageStorage(
            TimePilotStorage storageSnapshot,
            DateTimeOffset endedAt,
            string? appVersion,
            UsageDataClearProgress clearProgress)
        {
            idleSessionTracker?.EndCurrentSession(endedAt);
            foregroundSessionTracker?.EndCurrentSession(endedAt);
            lock (processRuntimeTrackingLock)
            {
                processRuntimeSessionTracker?.EndCurrentSessions(endedAt);
            }

            storageSnapshot.EndRuntimeSession(endedAt, "clear-data");
            clearProgress.IsRuntimeSessionEnded = true;
            storageSnapshot.ClearUsageData();
            var restartedAt = DateTimeOffset.UtcNow;
            storageSnapshot.BeginRuntimeSession(
                restartedAt,
                GetCurrentSystemBootedAt(restartedAt),
                appVersion);
            clearProgress.IsRuntimeSessionRestarted = true;
            RecordWindowsSystemEvent(
                "timepilot-start",
                "ApplicationRestartedAfterClearData");
        }

        private void ResetTrackingAfterUsageClear(TimePilotStorage storageSnapshot)
        {
            foregroundSessionTracker = new ForegroundSessionTracker(storageSnapshot);
            idleSessionTracker = new IdleSessionTracker(storageSnapshot);
            processRuntimeSessionTracker = new ProcessRuntimeSessionTracker(storageSnapshot);
            lastProcessRuntimeSampleAt = null;
            lastSampleTickAt = null;
            selectedRuntimeAppId = null;
            performanceStatusText = null;
            performanceStatusExpiresAt = null;
            viewRefreshStatusText = null;
            isViewRefreshWaitCursorActive = false;
            UpdateWaitCursor();
        }

        private void TryRestartTrackingAfterClearFailure(
            TimePilotStorage storageSnapshot,
            bool beginRuntimeSession)
        {
            try
            {
                var restartedAt = DateTimeOffset.UtcNow;
                if (beginRuntimeSession)
                {
                    storageSnapshot.BeginRuntimeSession(
                        restartedAt,
                        GetCurrentSystemBootedAt(restartedAt),
                        Application.ProductVersion);
                }

                foregroundSessionTracker = new ForegroundSessionTracker(storageSnapshot);
                idleSessionTracker = new IdleSessionTracker(storageSnapshot);
                processRuntimeSessionTracker = new ProcessRuntimeSessionTracker(storageSnapshot);
                lastProcessRuntimeSampleAt = null;
                lastSampleTickAt = null;
            }
            catch
            {
            }
        }

        private sealed class UsageDataClearProgress
        {
            public bool IsRuntimeSessionEnded { get; set; }

            public bool IsRuntimeSessionRestarted { get; set; }
        }
    }
}
