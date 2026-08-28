using TimePilot.WinForms.KYS24;
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
    }
}
