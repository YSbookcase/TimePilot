using TimePilot.WinForms.KYS24;

namespace TimePilot.WinForms.Timeline
{
    internal readonly record struct TimelineSegmentSelectionKey(
        string ActivityType,
        DateTimeOffset StartedAt,
        DateTimeOffset? EndedAt,
        string DisplayName,
        string ProcessName,
        long? AppId)
    {
        public static TimelineSegmentSelectionKey From(ActivityTimelineRow row)
        {
            return new TimelineSegmentSelectionKey(
                row.ActivityType,
                row.StartedAt,
                row.EndedAt,
                row.DisplayName,
                row.ProcessName,
                row.AppId);
        }

        public bool Matches(ActivityTimelineRow row)
        {
            return string.Equals(ActivityType, row.ActivityType, StringComparison.Ordinal)
                && StartedAt == row.StartedAt
                && EndedAt == row.EndedAt
                && string.Equals(DisplayName, row.DisplayName, StringComparison.Ordinal)
                && string.Equals(ProcessName, row.ProcessName, StringComparison.OrdinalIgnoreCase)
                && AppId == row.AppId;
        }
    }
}
