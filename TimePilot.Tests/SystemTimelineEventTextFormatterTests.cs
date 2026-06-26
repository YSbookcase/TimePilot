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
        public void FormatDetails_FormatsTimePilotStartedAtDetail()
        {
            var systemEvent = new SystemTimelineEvent(
                DateTimeOffset.Parse("2026-06-26T10:00:00+09:00"),
                "timepilot-start",
                "TimePilotStartedAt:2026-06-26T09:30:00+09:00");

            var text = SystemTimelineEventTextFormatter.FormatDetails(systemEvent);

            Assert.Contains("TimePilot started at", text);
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
