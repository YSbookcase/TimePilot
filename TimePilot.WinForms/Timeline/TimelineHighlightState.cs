using TimePilot.WinForms.KYS24;

namespace TimePilot.WinForms.Timeline
{
    internal sealed record TimelineHighlightState(
        string? ProcessName,
        string? AppName,
        TimelineSegmentSelectionKey? SegmentKey,
        string? SegmentLabel)
    {
        public static TimelineHighlightState Empty { get; } = new(null, null, null, null);

        public bool HasAppHighlight => !string.IsNullOrWhiteSpace(ProcessName);

        public bool HasSegmentHighlight => SegmentKey is not null;

        public bool HasHighlight => HasAppHighlight || HasSegmentHighlight;

        public static TimelineHighlightState ForApp(string processName, string appName)
        {
            return new TimelineHighlightState(processName, appName, null, null);
        }

        public static TimelineHighlightState ForSegment(ActivityTimelineRow row)
        {
            return new TimelineHighlightState(
                null,
                null,
                TimelineSegmentSelectionKey.From(row),
                $"{row.DisplayName} {row.StartedAtText}-{row.EndedAtText}");
        }

        public string GetDisplayText()
        {
            if (SegmentKey is not null)
            {
                var label = SegmentLabel ?? "";
                return UiText.CurrentLanguage == UiLanguage.English
                    ? $"Highlight segment: {label}"
                    : $"구간 강조: {label}";
            }

            return UiText.Main.TimelineHighlight(AppName ?? ProcessName!);
        }
    }
}
