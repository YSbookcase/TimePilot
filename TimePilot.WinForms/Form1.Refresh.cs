using TimePilot.WinForms.KYS24;
using TimePilot.WinForms.Refresh;
using TimePilot.WinForms.Tables;
using TimePilot.WinForms.Timeline;

namespace TimePilot.WinForms
{
    public partial class Form1
    {
        private void ApplySummarySnapshot(ViewRefreshSnapshot snapshot)
        {
            SetRuntimeCoverageSummary(snapshot.RuntimeCoverage);
            SetSummaryIdleAnalysis(snapshot.ForegroundUsage!, snapshot.IdleUsage);
            var previousUsageSelection = GetSelectedUsageSummaryRow();
            var usageRows = AddIcons(SortUsageSummaryRows(UsageSummaryRowBuilder.FromForegroundUsage(
                snapshot.ForegroundUsage!,
                snapshot.ShowDateInUsageTimestamps)));
            GridViewStatePreserver.SetDataSourcePreservingView(usageGrid, usageRows);
            RestoreUsageGridSelection(previousUsageSelection);
            SetSummaryUsageBars(usageRows);
            GridViewStatePreserver.SetDataSourcePreservingView(
                dailyUsageTrendGrid,
                SortDailyUsageTrendRows(snapshot.DailyUsageTrendRows ?? Array.Empty<DailyUsageTrendRow>()));
            usageGrid.Invalidate();
        }

        private void ApplyTimelineSnapshot(ViewRefreshSnapshot snapshot)
        {
            var timelineRows = snapshot.TimelineRows!;
            currentTimelineRows = timelineRows;
            currentTimelineForegroundUsage =
                snapshot.TimelineForegroundUsage ?? Array.Empty<ForegroundUsageSummary>();
            SetDateStatus(timelineDateStatusLabel, snapshot.TimelineDateHasData);
            var filteredSystemRanges = TimelineSystemEventPresenter.FilterRanges(
                snapshot.SystemTimelineRanges ?? Array.Empty<SystemTimelineRange>(),
                selectedTimelineSystemEventFilter);
            var filteredSystemEvents = TimelineSystemEventPresenter.FilterEvents(
                snapshot.SystemTimelineEvents ?? Array.Empty<SystemTimelineEvent>(),
                selectedTimelineSystemEventFilter);
            currentTimelineWindowsRuntimeRanges =
                snapshot.WindowsRuntimeRanges ?? Array.Empty<TimelineRange>();
            currentTimelineSystemRanges =
                snapshot.SystemTimelineRanges ?? Array.Empty<SystemTimelineRange>();
            currentTimelineSystemEvents = TimelineSystemEventPresenter.FilterEvents(
                (snapshot.SystemTimelineEvents ?? Array.Empty<SystemTimelineEvent>())
                    .Concat(snapshot.InferredSystemTimelineEvents ?? Array.Empty<SystemTimelineEvent>())
                    .ToList(),
                selectedTimelineSystemEventFilter);
            timelineOverviewControl.SetTimeline(
                selectedTimelineDate,
                timelineRows,
                currentTimelineWindowsRuntimeRanges,
                filteredSystemRanges,
                filteredSystemEvents,
                snapshot.CategoryTimelineSegments ?? Array.Empty<CategoryTimelineSegment>());
            timelineOverviewControl.SetSystemEventHighlightEnabled(
                selectedTimelineSystemEventFilter != TimelineSystemEventFilter.All);
            ApplyTimelineHighlightToOverview();
            GridViewStatePreserver.SetDataSourcePreservingView(
                timelineGrid,
                AddIcons(SortTimelineRows(timelineRows)));
            UpdateTimelineHighlightUi();
        }

        private void ApplyDetailSnapshot(
            ViewRefreshSnapshot snapshot,
            DateTimeOffset observedAt,
            long? appIdToRestore,
            int firstDisplayedRowIndex,
            int firstDisplayedColumnIndex,
            int horizontalOffset)
        {
            SetDateStatus(detailDateStatusLabel, snapshot.DetailDateHasData);
            var appIdToRestoreOnApply = selectedRuntimeAppId ?? appIdToRestore;
            isRefreshingRuntimeGrid = true;
            try
            {
                var runtimeRows = ApplyCurrentTrackingScope(snapshot.RuntimeRows!);
                GridViewStatePreserver.SetDataSourcePreservingView(
                    runtimeGrid,
                    AddIcons(SortRuntimeSummaryRows(FilterRuntimeSummaryRows(
                        runtimeRows,
                        snapshot.DetailSummaryAppIds))),
                    preserveSelection: false);
                RestoreRuntimeSelection(
                    appIdToRestoreOnApply,
                    firstDisplayedRowIndex,
                    firstDisplayedColumnIndex,
                    horizontalOffset);
            }
            finally
            {
                isRefreshingRuntimeGrid = false;
            }

            selectedRuntimeAppId = appIdToRestoreOnApply ?? GetSelectedRuntimeAppId();
            if (selectedRuntimeAppId == appIdToRestore && snapshot.RuntimeSegmentRows is not null)
            {
                var sortedSegments = SortRuntimeSegmentRows(
                    FilterRuntimeSegmentRows(snapshot.RuntimeSegmentRows));
                SetRuntimeSegmentsDataSource(sortedSegments);
                var keyToRestore = runtimeSegmentSelectionCoordinator.CurrentKey;
                runtimeSegmentSelectionCoordinator.RestoreSelection(
                    keyToRestore,
                    selectFirstWhenMissing: keyToRestore is null);
                UpdateRuntimeSegmentTimeline(GetRuntimeRowForSelectedApp(), sortedSegments);
            }
            else
            {
                RefreshRuntimeSegments(observedAt);
            }

            RestoreRuntimeGridView(
                firstDisplayedRowIndex,
                firstDisplayedColumnIndex,
                horizontalOffset);
            ScheduleRuntimeGridViewRestore(
                firstDisplayedRowIndex,
                firstDisplayedColumnIndex,
                horizontalOffset);
        }
    }
}
