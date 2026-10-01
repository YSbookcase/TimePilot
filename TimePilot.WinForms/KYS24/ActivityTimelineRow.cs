using System.Globalization;

namespace TimePilot.WinForms.KYS24
{
    internal sealed record ActivityTimelineRow(
        string ActivityType,
        DateTimeOffset StartedAt,
        DateTimeOffset? EndedAt,
        long DurationMs,
        string DisplayName,
        string? ExecutablePath = null,
        Image? AppIcon = null,
        string ProcessName = "",
        long? AppId = null,
        long? PrimaryCategoryId = null,
        string? CategoryName = null,
        int? IdleThresholdMs = null)
    {
        public string CategoryText => string.IsNullOrWhiteSpace(CategoryName)
            ? UiText.Main.Uncategorized
            : AppCategoryDisplay.GetDisplayName(CategoryName);

        public string StartedAtText => StartedAt.ToLocalTime().ToString("HH:mm:ss", CultureInfo.CurrentCulture);

        public string EndedAtText => EndedAt?.ToLocalTime().ToString("HH:mm:ss", CultureInfo.CurrentCulture)
            ?? UiText.Timeline.InProgress;

        public string DurationText => FormatDuration(DurationMs);

        public string IdleThresholdText => IdleThresholdMs is null
            ? ""
            : FormatDuration(IdleThresholdMs.Value);

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
