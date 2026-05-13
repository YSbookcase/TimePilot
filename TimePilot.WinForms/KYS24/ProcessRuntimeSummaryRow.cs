using System.Globalization;

namespace TimePilot.WinForms.KYS24
{
    internal sealed record ProcessRuntimeSummaryRow(
        long AppId,
        string AppName,
        string? ExecutablePath,
        long RuntimeMs,
        long ActiveUsageMs,
        double? ActualUsageRatio,
        int RuntimeSegmentCount,
        bool HasRunningSession,
        bool HasMainWindow,
        bool IsCurrentSessionProcess,
        Image? AppIcon = null,
        DateTimeOffset? FirstObservedAt = null,
        DateTimeOffset? LastObservedAt = null)
    {
        public string RuntimeText => FormatDuration(RuntimeMs);

        public string ActiveUsageTimeText => FormatDuration(ActiveUsageMs);

        public string ActualUsageRatioText => ActualUsageRatio is null
            ? "-"
            : ActualUsageRatio.Value.ToString("P1", CultureInfo.CurrentCulture);

        public string RuntimeSegmentCountText => RuntimeSegmentCount.ToString("N0", CultureInfo.CurrentCulture);

        public string StatusText => HasRunningSession ? "실행 중" : "종료";

        public string FirstObservedAtText => FormatTime(FirstObservedAt);

        public string LastObservedAtText => FormatTime(LastObservedAt);

        private static string FormatDuration(long durationMs)
        {
            var span = TimeSpan.FromMilliseconds(durationMs);
            return string.Format(
                CultureInfo.CurrentCulture,
                "{0:D2}:{1:D2}:{2:D2}",
                (int)span.TotalHours,
                span.Minutes,
                span.Seconds);
        }

        private static string FormatTime(DateTimeOffset? timestamp)
        {
            return timestamp?.ToLocalTime().ToString("HH:mm:ss", CultureInfo.CurrentCulture) ?? "";
        }
    }
}
