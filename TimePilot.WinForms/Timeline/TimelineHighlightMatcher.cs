using TimePilot.WinForms.KYS24;

namespace TimePilot.WinForms.Timeline
{
    internal static class TimelineHighlightMatcher
    {
        public static bool HasHighlight(
            TimelineHighlightState state,
            TimelineActivityTypeHighlight activityTypeHighlight)
        {
            return state.HasHighlight || activityTypeHighlight != TimelineActivityTypeHighlight.None;
        }

        public static bool IsRowHighlighted(
            ActivityTimelineRow row,
            TimelineHighlightState state,
            TimelineActivityTypeHighlight activityTypeHighlight)
        {
            if (state.SegmentKey is { } segmentKey)
                return segmentKey.Matches(row);

            if (activityTypeHighlight == TimelineActivityTypeHighlight.Windows
                && !state.HasAppHighlight)
                return false;

            var processMatches = string.IsNullOrWhiteSpace(state.ProcessName)
                || string.Equals(row.ProcessName, state.ProcessName, StringComparison.OrdinalIgnoreCase);
            var typeText = GetActivityTypeText(activityTypeHighlight);
            var typeMatches = typeText is null
                || string.Equals(row.ActivityType, typeText, StringComparison.Ordinal);

            if (activityTypeHighlight == TimelineActivityTypeHighlight.Untracked)
            {
                typeMatches = string.Equals(row.ActivityType, UiText.Main.Untracked, StringComparison.Ordinal)
                    || string.Equals(row.ActivityType, UiText.Main.TimePilotUntracked, StringComparison.Ordinal);
            }

            return processMatches && typeMatches;
        }

        public static string? GetActivityTypeText(TimelineActivityTypeHighlight activityTypeHighlight)
        {
            return activityTypeHighlight switch
            {
                TimelineActivityTypeHighlight.Active => UiText.Main.Active,
                TimelineActivityTypeHighlight.Idle => UiText.Main.Idle,
                TimelineActivityTypeHighlight.Untracked => UiText.Main.Untracked,
                _ => null
            };
        }
    }
}
