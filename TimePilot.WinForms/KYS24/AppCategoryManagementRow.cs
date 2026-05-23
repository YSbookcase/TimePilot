using System.Globalization;

namespace TimePilot.WinForms.KYS24
{
    internal sealed record AppCategoryManagementRow(
        long AppId,
        string AppName,
        string ProcessName,
        string? ExecutablePath,
        long? PrimaryCategoryId,
        string? CategoryName,
        DateTimeOffset? LastObservedAt,
        long ActiveUsageMs,
        long RuntimeMs,
        int SwitchCount,
        int RuntimeSegmentCount,
        string? FileDescription = null,
        string? ProductName = null,
        string? CompanyName = null,
        string? CategoryDisplayName = null,
        bool HasExtractedAppIcon = false,
        Image? AppIcon = null)
    {
        public string CategoryText => string.IsNullOrWhiteSpace(CategoryName)
            ? UiText.Main.Uncategorized
            : CategoryDisplayName ?? AppCategoryDisplay.GetDisplayName(CategoryName);

        public string LastObservedAtText => LastObservedAt?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture) ?? "";

        public string ActiveUsageTimeText => FormatDuration(ActiveUsageMs);

        public string RuntimeText => FormatDuration(RuntimeMs);

        public string SwitchCountText => SwitchCount.ToString("N0", CultureInfo.CurrentCulture);

        public string RuntimeSegmentCountText => RuntimeSegmentCount.ToString("N0", CultureInfo.CurrentCulture);

        public string FileDescriptionText => FileDescription ?? "";

        public string ProductNameText => ProductName ?? "";

        public string CompanyNameText => CompanyName ?? "";

        public bool HasAppIcon => HasExtractedAppIcon;

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
    }
}
