using System.Diagnostics;
using TimePilot.WinForms.KYS24;
using TimePilot.WinForms.Refresh;
using TimePilot.WinForms.Tables;
using TimePilot.WinForms.Timeline;

namespace TimePilot.WinForms
{
    public partial class Form1
    {
        private void RefreshViews(DateTimeOffset observedAt)
        {
            _ = RefreshViewsAsync(observedAt);
        }

        private void UpdateTimelineDataVersion(AppMetadata? foregroundApp, bool isIdle)
        {
            var foregroundKey = foregroundApp is null
                ? null
                : $"{foregroundApp.ProcessName}|{foregroundApp.ExecutablePath}";
            if (string.Equals(lastForegroundViewKey, foregroundKey, StringComparison.OrdinalIgnoreCase)
                && lastForegroundIdleState == isIdle)
                return;

            lastForegroundViewKey = foregroundKey;
            lastForegroundIdleState = isIdle;
            viewRefreshCache.MarkTimelineDataChanged();
        }

        private void InvalidateCategoryDependentViewCaches()
        {
            viewRefreshCache.InvalidateCategoryDependentViews();
        }

        private bool TryGetCachedHeavyViewSnapshot(
            TabPage? selectedTab,
            SummaryPeriodRange summaryPeriodRange,
            DateTime timelineDate,
            DateTime detailDate,
            long? selectedRuntimeAppId,
            int timelineCategoryBucketMinutes,
            DateTimeOffset observedAt,
            out ViewRefreshSnapshot snapshot)
        {
            snapshot = default!;
            if (selectedTab == summaryTab)
                return viewRefreshCache.TryGetSummary(summaryPeriodRange, observedAt, out snapshot);

            if (selectedTab == timelineTab)
            {
                return viewRefreshCache.TryGetTimeline(
                    timelineDate,
                    timelineCategoryBucketMinutes,
                    observedAt,
                    out snapshot);
            }

            if (selectedTab == detailTab)
            {
                return viewRefreshCache.TryGetDetail(
                    detailDate,
                    selectedRuntimeAppId,
                    observedAt,
                    out snapshot);
            }

            return false;
        }

        private void CacheHeavyViewSnapshot(
            TabPage? selectedTab,
            SummaryPeriodRange summaryPeriodRange,
            DateTime timelineDate,
            DateTime detailDate,
            long? selectedRuntimeAppId,
            int timelineCategoryBucketMinutes,
            DateTimeOffset observedAt,
            ViewRefreshSnapshot snapshot)
        {
            if (selectedTab == summaryTab && snapshot.ForegroundUsage is not null)
            {
                viewRefreshCache.StoreSummary(summaryPeriodRange, observedAt, snapshot);
                return;
            }

            if (selectedTab == timelineTab && snapshot.TimelineRows is not null)
            {
                viewRefreshCache.StoreTimeline(
                    timelineDate,
                    timelineCategoryBucketMinutes,
                    observedAt,
                    snapshot);
                return;
            }

            if (selectedTab == detailTab && snapshot.RuntimeRows is not null)
            {
                viewRefreshCache.StoreDetail(
                    detailDate,
                    selectedRuntimeAppId,
                    observedAt,
                    snapshot);
            }
        }

        private async Task RefreshViewsAsync(DateTimeOffset observedAt)
        {
            if (storage is null)
                return;

            if (isViewRefreshRunning)
            {
                ReportPerformanceEvents("view-skip");
                return;
            }

            var totalStopwatch = Stopwatch.StartNew();
            var appIdToRestore = selectedRuntimeAppId ?? GetSelectedRuntimeAppId();
            var runtimeFirstDisplayedRowIndex =
                GridViewStatePreserver.GetFirstDisplayedRowIndex(runtimeGrid);
            var runtimeFirstDisplayedColumnIndex =
                GridViewStatePreserver.GetFirstDisplayedColumnIndex(runtimeGrid);
            var runtimeHorizontalOffset =
                GridViewStatePreserver.GetHorizontalScrollingOffset(runtimeGrid);
            var selectedTab = mainTabs.SelectedTab;
            RefreshSummaryPeriodOptionsIfDateChanged(observedAt);
            RefreshDateSelectorsIfDateChanged(observedAt);
            var summaryPeriodRange = SummaryPeriodCalculator.GetRange(
                observedAt,
                selectedSummaryPeriod,
                selectedSummarySpecificDate,
                selectedSummaryCustomStartDate,
                selectedSummaryCustomEndDate);
            var detailDate = selectedDetailDate;
            var timelineDate = selectedTimelineDate;
            var refreshTarget = selectedTab == summaryTab
                ? ViewRefreshTarget.Summary
                : selectedTab == timelineTab
                    ? ViewRefreshTarget.Timeline
                    : selectedTab == detailTab
                        ? ViewRefreshTarget.Detail
                        : ViewRefreshTarget.None;
            var refreshRequest = new ViewRefreshRequest(
                refreshTarget,
                summaryPeriodRange,
                timelineDate,
                detailDate,
                appIdToRestore,
                selectedTimelineCategoryBucketMinutes,
                observedAt);
            ViewRefreshSnapshot snapshot;
            if (TryGetCachedHeavyViewSnapshot(
                selectedTab,
                summaryPeriodRange,
                timelineDate,
                detailDate,
                appIdToRestore,
                selectedTimelineCategoryBucketMinutes,
                observedAt,
                out var cachedSnapshot))
            {
                snapshot = cachedSnapshot;
            }
            else
            {
                isViewRefreshRunning = true;
                var showSummaryLoading = selectedTab == summaryTab;
                if (showSummaryLoading)
                    SetViewRefreshRunning(true, BuildViewRefreshInProgressStatus());

                try
                {
                    snapshot = await Task.Run(() =>
                        ViewRefreshSnapshotReader.Read(storage, refreshRequest));
                    CacheHeavyViewSnapshot(
                        selectedTab,
                        summaryPeriodRange,
                        timelineDate,
                        detailDate,
                        appIdToRestore,
                        selectedTimelineCategoryBucketMinutes,
                        observedAt,
                        snapshot);
                }
                catch
                {
                    return;
                }
                finally
                {
                    isViewRefreshRunning = false;
                    if (showSummaryLoading)
                        SetViewRefreshRunning(false, null);
                }
            }

            if (isClosing)
                return;

            var applyStopwatch = Stopwatch.StartNew();
            if (snapshot.ForegroundUsage is not null)
                ApplySummarySnapshot(snapshot);

            if (snapshot.TimelineRows is not null)
                ApplyTimelineSnapshot(snapshot);

            if (snapshot.RuntimeRows is not null)
            {
                ApplyDetailSnapshot(
                    snapshot,
                    observedAt,
                    appIdToRestore,
                    runtimeFirstDisplayedRowIndex,
                    runtimeFirstDisplayedColumnIndex,
                    runtimeHorizontalOffset);
            }

            UpdateSortGlyphs();
            RepositionHeaderToolTip();
            applyStopwatch.Stop();
            totalStopwatch.Stop();
            ReportPerformanceTimings(
                ("view-read", snapshot.ReadElapsedMs),
                ("view-apply", applyStopwatch.ElapsedMilliseconds),
                ("view-total", totalStopwatch.ElapsedMilliseconds));
        }

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
