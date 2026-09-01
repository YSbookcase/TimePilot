using TimePilot.WinForms.KYS24;
using Xunit;

namespace TimePilot.Tests
{
    public sealed class AppShutdownSignalTests
    {
        [Fact]
        public void RequestShutdown_ReturnsTrueWhenListenerExists()
        {
            var eventName = "ActiveLogbook.Tests.ShutdownRequested." + Guid.NewGuid();
            using var listener = AppShutdownSignal.CreateListener(eventName);

            Assert.True(AppShutdownSignal.RequestShutdown(eventName));
            Assert.True(listener.WaitOne(TimeSpan.FromSeconds(1)));
        }
    }
}
