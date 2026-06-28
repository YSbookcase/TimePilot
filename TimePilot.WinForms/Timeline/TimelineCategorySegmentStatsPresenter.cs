using System.ComponentModel;
using System.Globalization;
using TimePilot.WinForms.KYS24;

namespace TimePilot.WinForms.Timeline
{
    internal static class TimelineCategorySegmentStatsPresenter
    {
        public static string GetMenuText()
        {
            return UiText.CurrentLanguage == UiLanguage.English ? "Segment app stats" : "구간 앱 통계";
        }

        public static string GetTitle()
        {
            return UiText.CurrentLanguage == UiLanguage.English
                ? "Timeline Segment App Stats"
                : "타임라인 구간 앱 통계";
        }

        public static string BuildDescription(
            CategoryTimelineSegment segment,
            IReadOnlyList<ActivityTimelineRow> timelineRows,
            IReadOnlyList<TimelineRange> windowsRuntimeRanges,
            IReadOnlyList<SystemTimelineRange> systemRanges)
        {
            var start = segment.StartedAt.ToLocalTime().ToString("HH:mm:ss", CultureInfo.CurrentCulture);
            var end = segment.EndedAt.ToLocalTime().ToString("HH:mm:ss", CultureInfo.CurrentCulture);
            var duration = FormatDuration((long)(segment.EndedAt - segment.StartedAt).TotalMilliseconds);
            var activeUsage = FormatDuration(segment.ActiveUsageMs);
            var stateSummary = BuildStateSummary(segment, timelineRows, windowsRuntimeRanges, systemRanges);

            return UiText.CurrentLanguage == UiLanguage.English
                ? $"{segment.CategoryName} | {start}-{end} | segment {duration} | recorded active {activeUsage} | {segment.DetailText}\n{stateSummary}"
                : $"{segment.CategoryName} | {start}-{end} | 구간 {duration} | 기록된 활성 {activeUsage} | {segment.DetailText}\n{stateSummary}";
        }

        public static IReadOnlyList<UsageSummaryRow> SortRows(
            IReadOnlyList<UsageSummaryRow> rows,
            string propertyName,
            ListSortDirection direction)
        {
            IOrderedEnumerable<UsageSummaryRow> orderedRows = propertyName switch
            {
                nameof(UsageSummaryRow.AppName) => rows.OrderBy(row => row.AppName, StringComparer.CurrentCulture),
                nameof(UsageSummaryRow.CategoryText) => rows.OrderBy(row => row.CategoryText, StringComparer.CurrentCulture),
                nameof(UsageSummaryRow.ActiveUsageTimeText) => rows.OrderBy(row => row.ActiveUsageMs),
                nameof(UsageSummaryRow.UsageRatioText) => rows.OrderBy(row => row.UsageRatio),
                nameof(UsageSummaryRow.SwitchCountText) => rows.OrderBy(row => row.SwitchCount),
                nameof(UsageSummaryRow.FirstStartedAtText) => rows.OrderBy(row => row.FirstStartedAt),
                nameof(UsageSummaryRow.LastObservedAtText) => rows.OrderBy(row => row.LastObservedAt),
                _ => rows.OrderBy(row => row.ActiveUsageMs)
            };

            return direction == ListSortDirection.Ascending
                ? orderedRows.ToList()
                : orderedRows.Reverse().ToList();
        }

        private static string BuildStateSummary(
            CategoryTimelineSegment segment,
            IReadOnlyList<ActivityTimelineRow> timelineRows,
            IReadOnlyList<TimelineRange> windowsRuntimeRanges,
            IReadOnlyList<SystemTimelineRange> systemRanges)
        {
            var activeMs = SumTimelineRowDuration(segment, timelineRows, row =>
                !string.Equals(row.ActivityType, UiText.Main.Idle, StringComparison.Ordinal)
                && !IsUntrackedActivity(row));
            var idleMs = SumTimelineRowDuration(segment, timelineRows, row =>
                string.Equals(row.ActivityType, UiText.Main.Idle, StringComparison.Ordinal));
            var untrackedMs = SumTimelineRowDuration(segment, timelineRows, IsUntrackedActivity);
            var windowsRuntimeMs = windowsRuntimeRanges.Sum(range =>
                GetOverlapDurationMs(segment.StartedAt, segment.EndedAt, range.StartedAt, range.EndedAt));
            var sleepMs = SumSystemRangeDuration(segment, systemRanges, SystemTimelineRangeType.SleepEstimate);
            var lockMs = SumSystemRangeDuration(segment, systemRanges, SystemTimelineRangeType.LockSession);

            return UiText.CurrentLanguage == UiLanguage.English
                ? $"Status: active apps {FormatDuration(activeMs)} | idle {FormatDuration(idleMs)} | not tracked {FormatDuration(untrackedMs)} | Windows runtime {FormatDuration(windowsRuntimeMs)} | sleep estimate {FormatDuration(sleepMs)} | lock {FormatDuration(lockMs)}"
                : $"상태: 활성 앱 {FormatDuration(activeMs)} | 유휴 {FormatDuration(idleMs)} | 미기록 {FormatDuration(untrackedMs)} | Windows 실행 {FormatDuration(windowsRuntimeMs)} | 절전 추정 {FormatDuration(sleepMs)} | 잠금 {FormatDuration(lockMs)}";
        }

        private static long SumTimelineRowDuration(
            CategoryTimelineSegment segment,
            IEnumerable<ActivityTimelineRow> rows,
            Func<ActivityTimelineRow, bool> predicate)
        {
            return rows
                .Where(predicate)
                .Sum(row => GetOverlapDurationMs(
                    segment.StartedAt,
                    segment.EndedAt,
                    row.StartedAt,
                    row.EndedAt ?? segment.EndedAt));
        }

        private static long SumSystemRangeDuration(
            CategoryTimelineSegment segment,
            IEnumerable<SystemTimelineRange> ranges,
            SystemTimelineRangeType rangeType)
        {
            return ranges
                .Where(range => range.RangeType == rangeType)
                .Sum(range => GetOverlapDurationMs(
                    segment.StartedAt,
                    segment.EndedAt,
                    range.StartedAt,
                    range.EndedAt));
        }

        private static bool IsUntrackedActivity(ActivityTimelineRow row)
        {
            return string.Equals(row.ActivityType, UiText.Main.Untracked, StringComparison.Ordinal)
                || string.Equals(row.ActivityType, UiText.Main.TimePilotUntracked, StringComparison.Ordinal);
        }

        private static long GetOverlapDurationMs(
            DateTimeOffset leftStart,
            DateTimeOffset leftEnd,
            DateTimeOffset rightStart,
            DateTimeOffset rightEnd)
        {
            var start = leftStart > rightStart ? leftStart : rightStart;
            var end = leftEnd < rightEnd ? leftEnd : rightEnd;
            return end <= start ? 0 : (long)(end - start).TotalMilliseconds;
        }

        private static string FormatDuration(long durationMs)
        {
            return RuntimeDiagnosticsMessageBuilder.FormatDuration(durationMs);
        }
    }
}
