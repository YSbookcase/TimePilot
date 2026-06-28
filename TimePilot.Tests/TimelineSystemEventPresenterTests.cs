using System.ComponentModel;
using TimePilot.WinForms.KYS24;
using TimePilot.WinForms.Timeline;
using Xunit;

namespace TimePilot.Tests
{
    public sealed class TimelineSystemEventPresenterTests
    {
        public TimelineSystemEventPresenterTests()
        {
            UiText.UseLanguage(UiLanguage.English);
        }

        [Fact]
        public void FilterEvents_ReturnsOnlyEventsMatchingSelectedFilter()
        {
            var events = new[]
            {
                CreateEvent("lock", 9),
                CreateEvent("resume", 10),
                CreateEvent("unlock", 11)
            };

            var result = TimelineSystemEventPresenter.FilterEvents(events, TimelineSystemEventFilter.Lock);

            Assert.Equal(new[] { "lock", "unlock" }, result.Select(item => item.EventType));
        }

        [Fact]
        public void FilterRanges_ReturnsOnlyRangesMatchingSelectedFilter()
        {
            var ranges = new[]
            {
                CreateRange(SystemTimelineRangeType.LockSession, 9),
                CreateRange(SystemTimelineRangeType.SleepEstimate, 10)
            };

            var result = TimelineSystemEventPresenter.FilterRanges(ranges, TimelineSystemEventFilter.Power);

            Assert.Single(result);
            Assert.Equal(SystemTimelineRangeType.SleepEstimate, result[0].RangeType);
        }

        [Fact]
        public void BuildRows_ReturnsNewestFirstAndCalculatesPreviousInterval()
        {
            var events = new[]
            {
                CreateEvent("unlock", 10),
                CreateEvent("lock", 9)
            };

            var result = TimelineSystemEventPresenter.BuildRows(events);

            Assert.Equal(new[] { "Lock range end candidate", "Lock range start" }, result.Select(row => row.RelationText));
            Assert.Equal(3_600_000, result[0].PreviousIntervalMs);
            Assert.Equal(-1, result[1].PreviousIntervalMs);
        }

        [Fact]
        public void SortRows_SortsByNumericIntervalInsteadOfDisplayText()
        {
            var rows = new[]
            {
                CreateRow("Long", 60_000),
                CreateRow("Short", 5_000)
            };

            var result = TimelineSystemEventPresenter.SortRows(
                rows,
                nameof(SystemTimelineEventRow.PreviousIntervalText),
                ListSortDirection.Ascending);

            Assert.Equal(new[] { "Short", "Long" }, result.Select(row => row.EventTypeText));
        }

        private static SystemTimelineEvent CreateEvent(string eventType, int hour)
        {
            return new SystemTimelineEvent(
                new DateTimeOffset(2026, 6, 28, hour, 0, 0, TimeSpan.FromHours(9)),
                eventType,
                null);
        }

        private static SystemTimelineRange CreateRange(SystemTimelineRangeType rangeType, int hour)
        {
            var start = new DateTimeOffset(2026, 6, 28, hour, 0, 0, TimeSpan.FromHours(9));
            return new SystemTimelineRange(start, start.AddMinutes(10), rangeType);
        }

        private static SystemTimelineEventRow CreateRow(string eventTypeText, long intervalMs)
        {
            return new SystemTimelineEventRow(
                DateTimeOffset.MinValue,
                string.Empty,
                eventTypeText,
                intervalMs,
                intervalMs.ToString(),
                string.Empty,
                string.Empty);
        }
    }
}
