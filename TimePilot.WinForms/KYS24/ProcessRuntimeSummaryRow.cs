using System.Globalization;

namespace TimePilot.WinForms.KYS24
{
    internal sealed record ProcessRuntimeSummaryRow(
        long AppId,
        string AppName,
        string ProcessName,
        string? ExecutablePath,
        long? PrimaryCategoryId,
        string? CategoryName,
        long RuntimeMs,
        long ActiveUsageMs,
        double? ActualUsageRatio,
        int RuntimeSegmentCount,
        bool HasRunningSession,
        bool HasMainWindow,
        bool IsCurrentSessionProcess,
        Image? AppIcon = null,
        DateTimeOffset? FirstObservedAt = null,
        DateTimeOffset? LastObservedAt = null,
        bool IsInCurrentTrackingScope = true)
    {
        public string CategoryText => string.IsNullOrWhiteSpace(CategoryName)
            ? UiText.Main.Uncategorized
            : CategoryName;

        public string RuntimeText => FormatDuration(RuntimeMs);

        public string ActiveUsageTimeText => FormatDuration(ActiveUsageMs);

        public string ActualUsageRatioText => ActualUsageRatio is null
            ? "-"
            : ActualUsageRatio.Value.ToString("P1", CultureInfo.CurrentCulture);

        public string RuntimeSegmentCountText => RuntimeSegmentCount.ToString("N0", CultureInfo.CurrentCulture);

        public string TrackingTypeText
        {
            get
            {
                if (HasMainWindow)
                    return UiText.Main.WindowedApp;

                return IsCurrentSessionProcess ? UiText.Main.UserProcess : UiText.Main.AllProcesses;
            }
        }

        public string StatusText
        {
            get
            {
                if (!IsInCurrentTrackingScope)
                    return UiText.Main.OutsideTrackingScope;

                return HasRunningSession ? UiText.Main.Running : UiText.Main.Ended;
            }
        }

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
