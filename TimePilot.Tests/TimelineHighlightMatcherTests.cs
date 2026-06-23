using TimePilot.WinForms.KYS24;
using TimePilot.WinForms.Timeline;
using Xunit;

namespace TimePilot.Tests
{
    public sealed class TimelineHighlightMatcherTests
    {
        public TimelineHighlightMatcherTests()
        {
            UiText.UseLanguage(UiLanguage.English);
        }

        [Fact]
        public void IsRowHighlighted_AppHighlightMatchesSameProcess()
        {
            var state = TimelineHighlightState.ForApp("chrome", "Google Chrome");
            var row = CreateRow("chrome");

            var isHighlighted = TimelineHighlightMatcher.IsRowHighlighted(
                row,
                state,
                TimelineActivityTypeHighlight.None);

            Assert.True(isHighlighted);
        }

        [Fact]
        public void IsRowHighlighted_SegmentHighlightRequiresExactSegment()
        {
            var highlightedRow = CreateRow("chrome", startedAt: DateTimeOffset.Parse("2026-06-23T01:00:00+09:00"));
            var otherRow = CreateRow("chrome", startedAt: DateTimeOffset.Parse("2026-06-23T01:05:00+09:00"));
            var state = TimelineHighlightState.ForSegment(highlightedRow);

            Assert.True(TimelineHighlightMatcher.IsRowHighlighted(
                highlightedRow,
                state,
                TimelineActivityTypeHighlight.None));
            Assert.False(TimelineHighlightMatcher.IsRowHighlighted(
                otherRow,
                state,
                TimelineActivityTypeHighlight.None));
        }

        [Fact]
        public void IsRowHighlighted_WindowsOnlyHighlightDoesNotDimActivityRows()
        {
            var row = CreateRow("chrome");

            var isHighlighted = TimelineHighlightMatcher.IsRowHighlighted(
                row,
                TimelineHighlightState.Empty,
                TimelineActivityTypeHighlight.Windows);

            Assert.False(isHighlighted);
        }

        [Fact]
        public void IsRowHighlighted_UntrackedHighlightMatchesUntrackedRows()
        {
            var row = CreateRow("timepilot", activityType: UiText.Main.Untracked);

            var isHighlighted = TimelineHighlightMatcher.IsRowHighlighted(
                row,
                TimelineHighlightState.Empty,
                TimelineActivityTypeHighlight.Untracked);

            Assert.True(isHighlighted);
        }

        private static ActivityTimelineRow CreateRow(
            string processName,
            string? activityType = null,
            DateTimeOffset? startedAt = null)
        {
            var start = startedAt ?? DateTimeOffset.Parse("2026-06-23T01:00:00+09:00");
            return new ActivityTimelineRow(
                activityType ?? UiText.Main.Active,
                start,
                start.AddMinutes(5),
                300_000,
                processName,
                ProcessName: processName,
                AppId: 1);
        }
    }
}
