using TimePilot.WinForms.KYS24;
using Xunit;

namespace TimePilot.Tests
{
    public sealed class AppSettingsStartupDisplayModeTests
    {
        [Fact]
        public void MissingStartupDisplayMode_DefaultsToTray()
        {
            var root = Path.Combine(Path.GetTempPath(), $"TimePilotSettings-{Guid.NewGuid():N}");
            var settingsPath = Path.Combine(root, "settings.json");

            try
            {
                Directory.CreateDirectory(root);
                File.WriteAllText(settingsPath, "{\"StartWithWindows\":true}");

                var settings = AppSettings.Load(settingsPath);

                Assert.Equal(StartupDisplayMode.Tray, settings.StartupDisplayMode);
            }
            finally
            {
                if (Directory.Exists(root))
                    Directory.Delete(root, recursive: true);
            }
        }

        [Fact]
        public void StartupDisplayMode_RoundTripsThroughSettingsFile()
        {
            var root = Path.Combine(Path.GetTempPath(), $"TimePilotSettings-{Guid.NewGuid():N}");
            var settingsPath = Path.Combine(root, "settings.json");

            try
            {
                var settings = AppSettings.Load(settingsPath);
                settings.SetStartupDisplayMode(StartupDisplayMode.MainWindow);

                var reloaded = AppSettings.Load(settingsPath);

                Assert.Equal(StartupDisplayMode.MainWindow, reloaded.StartupDisplayMode);
            }
            finally
            {
                if (Directory.Exists(root))
                    Directory.Delete(root, recursive: true);
            }
        }
    }
}
