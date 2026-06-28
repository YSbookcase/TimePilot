using TimePilot.WinForms.KYS24;

namespace TimePilot.WinForms.Refresh
{
    internal sealed record ViewRefreshRequest(
        ViewRefreshTarget Target,
        SummaryPeriodRange SummaryPeriodRange,
        DateTime TimelineDate,
        DateTime DetailDate,
        long? SelectedRuntimeAppId,
        int TimelineCategoryBucketMinutes,
        DateTimeOffset ObservedAt);
}
