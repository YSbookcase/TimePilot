using TimePilot.WinForms.KYS24;

namespace TimePilot.WinForms.Refresh
{
    internal sealed record ViewRefreshSnapshot(
        IReadOnlyList<ForegroundUsageSummary>? ForegroundUsage,
        IReadOnlyList<DailyUsageTrendRow>? DailyUsageTrendRows,
        IdleUsageSummary? IdleUsage,
        RuntimeCoverageSummary? RuntimeCoverage,
        bool ShowDateInUsageTimestamps,
        bool? DetailDateHasData,
        bool? TimelineDateHasData,
        IReadOnlyList<ActivityTimelineRow>? TimelineRows,
        IReadOnlyList<TimelineRange>? WindowsRuntimeRanges,
        IReadOnlyList<SystemTimelineRange>? SystemTimelineRanges,
        IReadOnlyList<SystemTimelineEvent>? SystemTimelineEvents,
        IReadOnlyList<SystemTimelineEvent>? InferredSystemTimelineEvents,
        IReadOnlyList<CategoryTimelineSegment>? CategoryTimelineSegments,
        IReadOnlyList<ForegroundUsageSummary>? TimelineForegroundUsage,
        IReadOnlyList<ProcessRuntimeSummaryRow>? RuntimeRows,
        IReadOnlySet<long>? DetailSummaryAppIds,
        IReadOnlyList<ProcessRuntimeSegmentRow>? RuntimeSegmentRows,
        long ReadElapsedMs);
}
