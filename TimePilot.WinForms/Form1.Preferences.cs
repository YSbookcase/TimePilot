using TimePilot.WinForms.KYS24;
using TimePilot.WinForms.Tables;
using TimePilot.WinForms.Timeline;

namespace TimePilot.WinForms
{
    public partial class Form1
    {
        private void OnPreferencesMenuItemClick(object? sender, EventArgs e)
        {
            ShowPreferencesDialog();
        }

        private void ShowPreferencesDialog()
        {
            using var form = new PreferencesForm(settings);
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

            settings.SetStartWithWindows(form.StartWithWindows);
            settings.SetPerformanceDiagnosticsEnabled(
                form.PerformanceDiagnosticsEnabled);
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
