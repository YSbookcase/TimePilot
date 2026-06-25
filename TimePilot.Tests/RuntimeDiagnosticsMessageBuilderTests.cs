using TimePilot.WinForms.KYS24;
using Xunit;

namespace TimePilot.Tests
{
    public sealed class RuntimeDiagnosticsMessageBuilderTests
    {
        public RuntimeDiagnosticsMessageBuilderTests()
        {
            UiText.UseLanguage(UiLanguage.English);
        }

        [Fact]
        public void BuildMessage_ReturnsNoHistoryWhenEmpty()
        {
            var message = RuntimeDiagnosticsMessageBuilder.BuildMessage(
                Array.Empty<AppRuntimeSessionDiagnostic>(),
                Array.Empty<SystemEventDiagnostic>());

            Assert.Equal(UiText.Main.RuntimeDiagnosticsNoHistory, message);
        }

        [Fact]
        public void BuildMessage_IncludesRuntimeHistoryAndSystemEvents()
        {
            var startedAt = DateTimeOffset.Parse("2026-06-25T10:00:00+09:00");
            var sessions = new[]
            {
                new AppRuntimeSessionDiagnostic(
                    startedAt,
                    startedAt.AddMinutes(2),
                    startedAt.AddMinutes(2),
                    120_000,
                    "unexpected",
                    null,
                    null)
            };
            var events = new[]
            {
                new SystemEventDiagnostic(
                    startedAt.AddMinutes(1),
                    "timepilot-start",
                    "ApplicationStarted")
            };

            var message = RuntimeDiagnosticsMessageBuilder.BuildMessage(sessions, events);

            Assert.Contains(UiText.Main.RuntimeDiagnosticsLastRun, message);
            Assert.Contains(UiText.Main.ShutdownReasonUnexpected, message);
            Assert.Contains(UiText.Main.RuntimeDiagnosticsSystemEvents, message);
            Assert.Contains(UiText.Main.SystemEventTimePilotStart, message);
        }

        [Theory]
        [InlineData(59_000, "00:59")]
        [InlineData(61_000, "01:01")]
        [InlineData(3_661_000, "01:01:01")]
        public void FormatDuration_FormatsMilliseconds(long durationMs, string expected)
        {
            Assert.Equal(expected, RuntimeDiagnosticsMessageBuilder.FormatDuration(durationMs));
        }
    }
}
