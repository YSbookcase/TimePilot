using TimePilot.WinForms.KYS24;
using TimePilot.WinForms.Timeline;
using Xunit;

namespace TimePilot.Tests
{
    public sealed class TimelineHighlightSummaryBuilderTests
    {
        public TimelineHighlightSummaryBuilderTests()
        {
            UiText.UseLanguage(UiLanguage.English);
        }

        [Fact]
        public void Build_NoAppHighlightReturnsNull()
        {
            var summary = TimelineHighlightSummaryBuilder.Build(
                TimelineHighlightState.Empty,
                Array.Empty<ForegroundUsageSummary>(),
                Array.Empty<ActivityTimelineRow>(),
                FormatDuration);

            Assert.Null(summary);
        }

        [Fact]
        public void Build_AppHighlightUsesForegroundUsageAndTimelineRows()
        {
            var start = DateTimeOffset.Parse("2026-06-23T01:00:00+09:00");
            var state = TimelineHighlightState.ForApp("chrome", "Google Chrome");
            var foregroundUsage = new[]
            {
                CreateUsage("chrome", 600_000, 3, start),
                CreateUsage("code", 400_000, 1, start)
            };
            var rows = new[]
            {
                CreateRow("chrome", start, 300_000),
                CreateRow("chrome", start.AddMinutes(10), 600_000)
            };

            var summary = TimelineHighlightSummaryBuilder.Build(
                state,
                foregroundUsage,
                rows,
                FormatDuration);

            Assert.NotNull(summary);
            Assert.Contains("00:10:00", summary);
            Assert.Contains("60.0", summary);
            Assert.Contains("3", summary);
            Assert.Contains("2", summary);
        }

        private static ForegroundUsageSummary CreateUsage(
            string processName,
            long activeUsageMs,
            int switchCount,
            DateTimeOffset at)
        {
            return new ForegroundUsageSummary(
                1,
                processName,
                processName,
                null,
                null,
                null,
                null,
                activeUsageMs,
                0,
                switchCount,
                at,
                at.AddMinutes(10));
        }

        private static ActivityTimelineRow CreateRow(
            string processName,
            DateTimeOffset startedAt,
            long durationMs)
        {
            return new ActivityTimelineRow(
                UiText.Main.Active,
                startedAt,
                startedAt.AddMilliseconds(durationMs),
                durationMs,
                processName,
                ProcessName: processName,
                AppId: 1);
        }

        private static string FormatDuration(long durationMs)
        {
            var span = TimeSpan.FromMilliseconds(durationMs);
            return $"{(int)span.TotalHours:00}:{span.Minutes:00}:{span.Seconds:00}";
        }
    }
}
