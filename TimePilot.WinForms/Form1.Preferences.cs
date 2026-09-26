using TimePilot.WinForms.KYS24;
using TimePilot.WinForms.Tables;
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
            if (settings.AutomaticBackupEnabled)
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
            RefreshViews(DateTimeOffset.UtcNow);

            if (form.ClearUsageDataRequested)
                ClearUsageData();
        }

        private void ClearUsageData()
        {
            if (storage is null)
                return;

            var now = DateTimeOffset.UtcNow;
            sampleTimer.Stop();

            try
            {
                idleSessionTracker?.EndCurrentSession(now);
                foregroundSessionTracker?.EndCurrentSession(now);
                lock (processRuntimeTrackingLock)
                {
                    processRuntimeSessionTracker?.EndCurrentSessions(now);
                }

                storage.EndRuntimeSession(now, "clear-data");
                storage.ClearUsageData();
                storage.BeginRuntimeSession(
                    now,
                    GetCurrentSystemBootedAt(now),
                    Application.ProductVersion);
                RecordWindowsSystemEvent(
                    "timepilot-start",
                    "ApplicationRestartedAfterClearData");

                foregroundSessionTracker = new ForegroundSessionTracker(storage);
                idleSessionTracker = new IdleSessionTracker(storage);
                processRuntimeSessionTracker =
                    new ProcessRuntimeSessionTracker(storage);
                lastProcessRuntimeSampleAt = null;
                lastSampleTickAt = null;
                selectedRuntimeAppId = null;
                performanceStatusText = null;
                performanceStatusExpiresAt = null;
                viewRefreshStatusText = null;
                isViewRefreshWaitCursorActive = false;
                UpdateWaitCursor();
                viewRefreshCache.Clear();

                GridViewStatePreserver.SetDataSourcePreservingView(
                    usageGrid,
                    Array.Empty<UsageSummaryRow>());
                GridViewStatePreserver.SetDataSourcePreservingView(
                    dailyUsageTrendGrid,
                    Array.Empty<DailyUsageTrendRow>());
                SetRuntimeCoverageSummary(null);
                timelineOverviewControl.SetTimeline(
                    selectedTimelineDate,
                    Array.Empty<ActivityTimelineRow>(),
                    Array.Empty<TimelineRange>(),
                    Array.Empty<SystemTimelineRange>(),
                    Array.Empty<SystemTimelineEvent>(),
                    Array.Empty<CategoryTimelineSegment>());
                GridViewStatePreserver.SetDataSourcePreservingView(
                    timelineGrid,
                    Array.Empty<ActivityTimelineRow>());
                currentTimelineForegroundUsage =
                    Array.Empty<ForegroundUsageSummary>();
                currentTimelineRows = Array.Empty<ActivityTimelineRow>();
                currentTimelineWindowsRuntimeRanges = Array.Empty<TimelineRange>();
                currentTimelineSystemRanges = Array.Empty<SystemTimelineRange>();
                currentTimelineSystemEvents = Array.Empty<SystemTimelineEvent>();
                GridViewStatePreserver.SetDataSourcePreservingView(
                    runtimeGrid,
                    Array.Empty<ProcessRuntimeSummaryRow>());
                GridViewStatePreserver.SetDataSourcePreservingView(
                    runtimeSegmentsGrid,
                    Array.Empty<ProcessRuntimeSegmentRow>());
                SetStatusText(UiText.Main.UsageDataCleared);
            }
            finally
            {
                if (!isClosing)
                    sampleTimer.Start();
            }
        }
    }
}
