using TimePilot.WinForms.KYS24;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Xunit;

namespace TimePilot.Tests
{
    public sealed class WindowsStartupRegistrationTests
    {
        [Fact]
        public void BuildStartupCommand_QuotesExecutableAndStartsInTray()
        {
            var command = WindowsStartupRegistration.BuildStartupCommand(
                @"C:\Program Files\ActiveLogbook\ActiveLogbook.exe");

            Assert.Equal(
                @"""C:\Program Files\ActiveLogbook\ActiveLogbook.exe"" --tray",
                command);
        }

        [Fact]
        public void IsStartupCommandForExecutable_ReturnsTrueForMatchingCommand()
        {
            var executablePath = @"C:\Program Files\ActiveLogbook\ActiveLogbook.exe";
            var command = WindowsStartupRegistration.BuildStartupCommand(executablePath);

            Assert.True(WindowsStartupRegistration.IsStartupCommandForExecutable(
                command,
                executablePath));
        }

        [Fact]
        public void IsStartupCommandForExecutable_ReturnsFalseForMissingOrStaleCommand()
        {
            Assert.False(WindowsStartupRegistration.IsStartupCommandForExecutable(
                null,
                @"C:\Program Files\ActiveLogbook\ActiveLogbook.exe"));
            Assert.False(WindowsStartupRegistration.IsStartupCommandForExecutable(
                @"""E:\Old\TimePilot.exe"" --tray",
                @"C:\Program Files\ActiveLogbook\ActiveLogbook.exe"));
        }

        [Theory]
        [InlineData(StartupTaskState.DisabledByUser)]
        [InlineData(StartupTaskState.DisabledByPolicy)]
        public void ThrowIfNotEnabled_ExplainsWhyWindowsRejectedStartup(StartupTaskState state)
        {
            var exception = Assert.Throws<StartupTaskStateException>(
                () => WindowsStartupRegistration.ThrowIfNotEnabled(state));

            Assert.Equal(state, exception.State);
            Assert.Contains("Windows", exception.Message);
        }

        [Fact]
        public void ThrowIfNotEnabled_AllowsEnabledStates()
        {
            WindowsStartupRegistration.ThrowIfNotEnabled(StartupTaskState.Enabled);
            WindowsStartupRegistration.ThrowIfNotEnabled(StartupTaskState.EnabledByPolicy);
        }

        [Theory]
        [InlineData(ActivationKind.StartupTask, true)]
        [InlineData(ActivationKind.Launch, false)]
        public void IsStartupLaunch_UsesPackagedActivationKind(ActivationKind kind, bool expected)
        {
            Assert.Equal(expected, WindowsStartupRegistration.IsStartupLaunch([], kind));
        }

        [Fact]
        public void IsStartupLaunch_PreservesUnpackagedTrayArgument()
        {
            Assert.True(WindowsStartupRegistration.IsStartupLaunch(["--tray"], null));
            Assert.False(WindowsStartupRegistration.IsStartupLaunch([], null));
        }
    }
}
