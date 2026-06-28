using System.Diagnostics;
using TimePilot.WinForms.KYS24;

namespace TimePilot.WinForms.Refresh
{
    internal static class ViewRefreshSnapshotReader
    {
        public static ViewRefreshSnapshot Read(
            TimePilotStorage storage,
            ViewRefreshRequest request)
        {
            var stopwatch = Stopwatch.StartNew();
            var snapshot = request.Target switch
            {
                ViewRefreshTarget.Summary => ReadSummary(storage, request),
                ViewRefreshTarget.Timeline => ReadTimeline(storage, request),
                ViewRefreshTarget.Detail => ReadDetail(storage, request),
                _ => CreateEmpty(request)
            };
            stopwatch.Stop();
            return snapshot with { ReadElapsedMs = stopwatch.ElapsedMilliseconds };
        }

        private static ViewRefreshSnapshot ReadSummary(
            TimePilotStorage storage,
            ViewRefreshRequest request)
        {
            var range = request.SummaryPeriodRange;
            var usage = storage.GetForegroundUsageWithDailyTrendForPeriod(range.Start, range.End);
            return CreateEmpty(request) with
            {
                ForegroundUsage = usage.ForegroundUsage,
                DailyUsageTrendRows = usage.DailyUsageTrendRows,
                IdleUsage = storage.GetIdleUsageForPeriod(range.Start, range.End),
                RuntimeCoverage = storage.GetRuntimeCoverageForPeriod(
                    range.Start,
                    range.End,
                    request.ObservedAt)
            };
        }

        private static ViewRefreshSnapshot ReadTimeline(
            TimePilotStorage storage,
            ViewRefreshRequest request)
        {
            var date = request.TimelineDate;
            var observedAt = request.ObservedAt;
            var bucketMinutes = request.TimelineCategoryBucketMinutes;
            return CreateEmpty(request) with
            {
                TimelineDateHasData = storage.HasActivityDataForDate(date, observedAt),
                TimelineRows = storage.GetActivityTimelineForDate(date, observedAt),
                WindowsRuntimeRanges = storage.GetWindowsRuntimeRangesForDate(date, observedAt),
                SystemTimelineRanges = storage.GetSystemTimelineRangesForDate(date, observedAt),
                SystemTimelineEvents = storage.GetSystemTimelineEventsForDate(date, observedAt),
                InferredSystemTimelineEvents = storage.GetInferredSystemTimelineEventsForDate(date, observedAt),
                CategoryTimelineSegments = storage.GetCategoryTimelineSegmentsForDate(
                    date,
                    observedAt,
                    TimeSpan.FromMinutes(bucketMinutes),
                    bucketMinutes == 0),
                TimelineForegroundUsage = storage.GetForegroundUsageForDate(date)
            };
        }

        private static ViewRefreshSnapshot ReadDetail(
            TimePilotStorage storage,
            ViewRefreshRequest request)
        {
            var date = request.DetailDate;
            var observedAt = request.ObservedAt;
            var foregroundUsage = storage.GetForegroundUsageForDate(date);
            return CreateEmpty(request) with
            {
                DetailDateHasData = storage.HasActivityDataForDate(date, observedAt),
                RuntimeRows = storage.GetProcessRuntimeUsageForDate(date, observedAt),
                DetailSummaryAppIds = foregroundUsage.Select(item => item.AppId).ToHashSet(),
                RuntimeSegmentRows = request.SelectedRuntimeAppId is { } appId
                    ? storage.GetProcessRuntimeSegmentsForDate(appId, date, observedAt)
                    : null
            };
        }

        private static ViewRefreshSnapshot CreateEmpty(ViewRefreshRequest request)
        {
            return new ViewRefreshSnapshot(
                null,
                null,
                null,
                null,
                request.SummaryPeriodRange.ShowDateInTimestamps,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                0);
        }
    }
}
