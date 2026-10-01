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
        string? CategoryColor,
        long ActiveUsageMs,
        long IdleRecordedMs,
        double UsageRatio,
        int SwitchCount,
        Image? AppIcon = null,
        DateTimeOffset? FirstStartedAt = null,
        DateTimeOffset? LastObservedAt = null,
        bool ShowDateInTimestamps = false)
    {
        public UsageSummaryRow(
            long? appId,
            string appName,
            string processName,
            string? executablePath,
            long? primaryCategoryId,
            string? categoryName,
            long activeUsageMs,
            double usageRatio,
            int switchCount,
            Image? appIcon = null,
            DateTimeOffset? firstStartedAt = null,
            DateTimeOffset? lastObservedAt = null,
            bool showDateInTimestamps = false)
            : this(
                appId,
                appName,
                processName,
                executablePath,
                primaryCategoryId,
                categoryName,
                null,
                activeUsageMs,
                usageRatio,
                switchCount,
                appIcon,
                firstStartedAt,
                lastObservedAt,
                showDateInTimestamps)
        {
        }

        public UsageSummaryRow(
            long? appId,
            string appName,
            string processName,
            string? executablePath,
            long? primaryCategoryId,
            string? categoryName,
            string? categoryColor,
            long activeUsageMs,
            double usageRatio,
            int switchCount,
            Image? appIcon = null,
            DateTimeOffset? firstStartedAt = null,
            DateTimeOffset? lastObservedAt = null,
            bool showDateInTimestamps = false)
            : this(
                appId,
                appName,
                processName,
                executablePath,
                primaryCategoryId,
                categoryName,
                categoryColor,
                activeUsageMs,
                0,
                usageRatio,
                switchCount,
                appIcon,
                firstStartedAt,
                lastObservedAt,
                showDateInTimestamps)
        {
        }

        public string CategoryText => string.IsNullOrWhiteSpace(CategoryName)
            ? UiText.Main.Uncategorized
            : AppCategoryDisplay.GetDisplayName(CategoryName);

        public string ActiveUsageTimeText => FormatDuration(ActiveUsageMs);

        public string IdleRecordedTimeText => FormatDuration(IdleRecordedMs);

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
