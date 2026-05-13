namespace TimePilot.WinForms.KYS24
{
    internal static class AppDataPaths
    {
        public static string DataDirectory
        {
            get
            {
                var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                return Path.Combine(appDataPath, "TimePilot");
            }
        }

        public static string SettingsPath => Path.Combine(DataDirectory, "settings.json");

        public static string DatabasePath => Path.Combine(DataDirectory, "timepilot.db");
    }
}
