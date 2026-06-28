using TimePilot.WinForms.KYS24;
using TimePilot.WinForms.Timeline;
using Xunit;

namespace TimePilot.Tests
{
    public sealed class TimelineSystemEventFilterMatcherTests
    {
        [Theory]
        [InlineData("lock")]
        [InlineData("unlock")]
        [InlineData("logon")]
        [InlineData("logoff")]
        public void Matches_LockFilter_IncludesLockAndLogonEvents(string eventType)
        {
            Assert.True(TimelineSystemEventFilterMatcher.Matches(eventType, TimelineSystemEventFilter.Lock));
        }

        [Theory]
        [InlineData("suspend")]
        [InlineData("resume")]
        [InlineData("power-status-change")]
        [InlineData("power-mode")]
        public void Matches_PowerFilter_IncludesPowerEvents(string eventType)
        {
            Assert.True(TimelineSystemEventFilterMatcher.Matches(eventType, TimelineSystemEventFilter.Power));
        }

        [Theory]
        [InlineData("system-shutdown")]
        [InlineData("recording-end-estimate")]
        public void Matches_ShutdownFilter_IncludesShutdownEvents(string eventType)
        {
            Assert.True(TimelineSystemEventFilterMatcher.Matches(eventType, TimelineSystemEventFilter.Shutdown));
        }

        [Theory]
        [InlineData("timepilot-start")]
        [InlineData("timepilot-exit")]
        [InlineData("windows-boot-estimate")]
        [InlineData("recording-end-estimate")]
        public void Matches_TimePilotFilter_IncludesTimePilotEvents(string eventType)
        {
            Assert.True(TimelineSystemEventFilterMatcher.Matches(eventType, TimelineSystemEventFilter.TimePilot));
        }

        [Fact]
        public void Matches_AllFilter_IncludesAnyEvent()
        {
            Assert.True(TimelineSystemEventFilterMatcher.Matches("unknown-event", TimelineSystemEventFilter.All));
        }

        [Fact]
        public void Matches_SpecificFilter_ExcludesUnrelatedEvent()
        {
            Assert.False(TimelineSystemEventFilterMatcher.Matches("lock", TimelineSystemEventFilter.Power));
        }

        [Fact]
        public void Matches_IsCaseInsensitive()
        {
            Assert.True(TimelineSystemEventFilterMatcher.Matches("SYSTEM-SHUTDOWN", TimelineSystemEventFilter.Shutdown));
        }
    }
}
