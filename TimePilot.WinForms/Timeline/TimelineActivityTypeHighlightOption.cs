using TimePilot.WinForms.KYS24;

namespace TimePilot.WinForms.Timeline
{
    internal sealed record TimelineActivityTypeHighlightOption(string Label, TimelineActivityTypeHighlight Value)
    {
        public override string ToString() => Label;

        public static IReadOnlyList<TimelineActivityTypeHighlightOption> GetOptions()
        {
            return
            [
                new(UiText.Main.ClearTimelineHighlight, TimelineActivityTypeHighlight.None),
                new(UiText.Main.Active, TimelineActivityTypeHighlight.Active),
                new(UiText.Main.Idle, TimelineActivityTypeHighlight.Idle),
                new(UiText.Main.Untracked, TimelineActivityTypeHighlight.Untracked),
                new(UiText.Main.WindowsRuntimeTrack, TimelineActivityTypeHighlight.Windows)
            ];
        }
    }
}
