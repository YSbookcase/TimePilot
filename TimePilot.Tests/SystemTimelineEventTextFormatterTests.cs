using TimePilot.WinForms.KYS24;
using Xunit;

namespace TimePilot.Tests
{
    public sealed class SystemTimelineEventTextFormatterTests
    {
        public SystemTimelineEventTextFormatterTests()
        {
            UiText.UseLanguage(UiLanguage.English);
        }

        [Theory]
        [InlineData("lock", "Lock range start")]
        [InlineData("unlock", "Lock range end candidate")]
        [InlineData("suspend", "Sleep range start")]
        [InlineData("resume", "Sleep range end candidate")]
        [InlineData("unknown", "-")]
        public void GetRelationText_MapsKnownEvents(string eventType, string expected)
        {
            Assert.Equal(expected, SystemTimelineEventTextFormatter.GetRelationText(eventType));
        }

        [Fact]
        public void GetListTitle_FormatsSelectedDate()
        {
            var text = SystemTimelineEventTextFormatter.GetListTitle(new DateTime(2026, 6, 28));

            Assert.Equal("System event list (2026-06-28)", text);
        }

        [Fact]
        public void GetListDescription_ExplainsEventIntervals()
        {
            var text = SystemTimelineEventTextFormatter.GetListDescription(new DateTime(2026, 6, 28));

            Assert.Contains("Selected date: 2026-06-28", text);
            Assert.Contains("not confirmed causes", text);
        }

        [Fact]
        public void GetHeaderTexts_ReturnEnglishColumnLabels()
        {
            Assert.Equal("Time", SystemTimelineEventTextFormatter.GetTimeHeaderText());
            Assert.Equal("Since previous", SystemTimelineEventTextFormatter.GetPreviousIntervalHeaderText());
            Assert.Equal("Hint", SystemTimelineEventTextFormatter.GetRelationHeaderText());
            Assert.Equal("Details", SystemTimelineEventTextFormatter.GetDetailsHeaderText());
        }

        [Fact]
        public void FormatDetails_FormatsTimePilotStartedAtDetail()
        {
            var systemEvent = new SystemTimelineEvent(
                DateTimeOffset.Parse("2026-06-26T10:00:00+09:00"),
                "timepilot-start",
                "TimePilotStartedAt:2026-06-26T09:30:00+09:00");

            var text = SystemTimelineEventTextFormatter.FormatDetails(systemEvent);

            Assert.Contains("ActiveLogbook started at", text);
            Assert.Contains("09:30:00", text);
        }

        [Fact]
        public void FormatDetails_FormatsShutdownReasonDetail()
        {
            var systemEvent = new SystemTimelineEvent(
                DateTimeOffset.Parse("2026-06-26T10:00:00+09:00"),
                "system-shutdown",
                "Reason:system-shutdown");

            var text = SystemTimelineEventTextFormatter.FormatDetails(systemEvent);

            Assert.Contains("Reason:", text);
            Assert.Contains(UiText.Main.ShutdownReasonSystemShutdown, text);
        }

        [Fact]
        public void FormatDetails_UsesInferredTextWhenDetailsAreEmpty()
        {
            var systemEvent = new SystemTimelineEvent(
                DateTimeOffset.Parse("2026-06-26T10:00:00+09:00"),
                "windows-boot-estimate",
                null,
                IsInferred: true);

            var text = SystemTimelineEventTextFormatter.FormatDetails(systemEvent);

            Assert.Equal("Estimated from Windows system startup time.", text);
        }
    }
}
