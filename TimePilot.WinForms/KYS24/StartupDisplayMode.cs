namespace TimePilot.WinForms.KYS24
{
    internal enum StartupDisplayMode
    {
        Tray = 0,
        MainWindow = 1
    }

    internal static class StartupDisplayModeResolver
    {
        internal static bool ShouldStartInTray(
            bool isStartupLaunch,
            StartupDisplayMode displayMode)
        {
            return isStartupLaunch && displayMode != StartupDisplayMode.MainWindow;
        }
    }
}
