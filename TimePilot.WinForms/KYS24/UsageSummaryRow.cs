using System.Globalization;

namespace TimePilot.WinForms.KYS24
{
    internal sealed record UsageSummaryRow(
        long? AppId,
        string AppName,
        string ProcessName,
        string? ExecutablePath,
        long? PrimaryCategoryId,
        string? CategoryName,
        long ActiveUsageMs,
        double UsageRatio,
        int SwitchCount,
        Image? AppIcon = null,
        DateTimeOffset? FirstStartedAt = null,
        DateTimeOffset? LastObservedAt = null,
        bool ShowDateInTimestamps = false)
    {
        public string CategoryText => string.IsNullOrWhiteSpace(CategoryName)
            ? UiText.Main.Uncategorized
            : CategoryName;

        public string ActiveUsageTimeText => FormatDuration(ActiveUsageMs);

        public string UsageRatioText => UsageRatio.ToString("P1", CultureInfo.CurrentCulture);

        public string SwitchCountText => SwitchCount.ToString("N0", CultureInfo.CurrentCulture);

        public string FirstStartedAtText => FormatTimestamp(FirstStartedAt, ShowDateInTimestamps);

        public string LastObservedAtText => FormatTimestamp(LastObservedAt, ShowDateInTimestamps);

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

        private static string FormatTimestamp(DateTimeOffset? timestamp, bool showDate)
        {
            return timestamp?.ToLocalTime().ToString(showDate ? "yyyy-MM-dd HH:mm:ss" : "HH:mm:ss", CultureInfo.CurrentCulture) ?? "";
        }
    }
}
