using TimePilot.WinForms.KYS24;

namespace TimePilot.WinForms.Timeline
{
    internal static class TimelineHighlightSummaryBuilder
    {
        public static string? Build(
            TimelineHighlightState state,
            IReadOnlyList<ForegroundUsageSummary> foregroundUsage,
            IReadOnlyList<ActivityTimelineRow> rows,
            Func<long, string> formatDuration)
        {
            if (string.IsNullOrWhiteSpace(state.ProcessName))
                return null;

            var usage = foregroundUsage.FirstOrDefault(x =>
                string.Equals(x.ProcessName, state.ProcessName, StringComparison.OrdinalIgnoreCase));
            var highlightedRows = rows
                .Where(x => string.Equals(x.ProcessName, state.ProcessName, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (usage is null && highlightedRows.Count == 0)
                return null;

            var totalActiveUsageMs = foregroundUsage.Sum(x => x.ActiveUsageMs);
            var activeUsageMs = usage?.ActiveUsageMs ?? highlightedRows
                .Where(x => string.Equals(x.ActivityType, UiText.Main.Active, StringComparison.Ordinal))
                .Sum(x => x.DurationMs);
            var switchCount = usage?.SwitchCount ?? 0;
            var longestSegmentMs = highlightedRows.Count == 0
                ? 0
                : highlightedRows.Max(x => x.DurationMs);

            return UiText.Main.TimelineHighlightSummary(
                formatDuration(activeUsageMs),
                totalActiveUsageMs <= 0 ? 0 : (double)activeUsageMs / totalActiveUsageMs,
                switchCount,
                highlightedRows.Count,
                formatDuration(longestSegmentMs));
        }
    }
}
