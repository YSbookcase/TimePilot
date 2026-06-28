using TimePilot.WinForms.KYS24;
using TimePilot.WinForms.Timeline;
using Xunit;

namespace TimePilot.Tests
{
    public sealed class TimelineCategorySegmentStatsPresenterTests
    {
        public TimelineCategorySegmentStatsPresenterTests()
        {
            UiText.UseLanguage(UiLanguage.English);
        }

        [Fact]
        public void BuildDescription_SummarizesOverlappingActivityAndSystemRanges()
        {
            var start = new DateTimeOffset(2026, 6, 28, 9, 0, 0, TimeSpan.FromHours(9));
            var segment = new CategoryTimelineSegment(
                start,
                start.AddMinutes(30),
                "Development",
                "#2563EB",
                false,
                10 * 60_000,
                "Development 100%");
            var rows = new[]
            {
                CreateRow("App", start.AddMinutes(-5), start.AddMinutes(10)),
                CreateRow(UiText.Main.Idle, start.AddMinutes(10), start.AddMinutes(15)),
                CreateRow(UiText.Main.Untracked, start.AddMinutes(20), start.AddMinutes(35))
            };
            var windowsRanges = new[]
            {
                new TimelineRange(start.AddMinutes(-10), start.AddMinutes(40))
            };
            var systemRanges = new[]
            {
                new SystemTimelineRange(start.AddMinutes(12), start.AddMinutes(18), SystemTimelineRangeType.SleepEstimate),
                new SystemTimelineRange(start.AddMinutes(25), start.AddMinutes(35), SystemTimelineRangeType.LockSession)
            };

            var result = TimelineCategorySegmentStatsPresenter.BuildDescription(
                segment,
                rows,
                windowsRanges,
                systemRanges);

            Assert.Contains("active apps 10:00", result);
            Assert.Contains("idle 05:00", result);
            Assert.Contains("not tracked 10:00", result);
            Assert.Contains("Windows runtime 30:00", result);
            Assert.Contains("sleep estimate 06:00", result);
            Assert.Contains("lock 05:00", result);
        }

        [Fact]
        public void GetMenuAndTitle_ReturnEnglishLabels()
        {
            Assert.Equal("Segment app stats", TimelineCategorySegmentStatsPresenter.GetMenuText());
            Assert.Equal("Timeline Segment App Stats", TimelineCategorySegmentStatsPresenter.GetTitle());
        }

        private static ActivityTimelineRow CreateRow(
            string activityType,
            DateTimeOffset startedAt,
            DateTimeOffset endedAt)
        {
            return new ActivityTimelineRow(
                activityType,
                startedAt,
                endedAt,
                (long)(endedAt - startedAt).TotalMilliseconds,
                activityType);
        }
    }
}
