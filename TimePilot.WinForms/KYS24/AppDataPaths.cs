namespace TimePilot.WinForms.KYS24
{
    internal static class AppDataPaths
    {
        private const string DataDirectoryName = "TimePilot";

        public static string DataDirectory
        {
            get
            {
                var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                return Path.Combine(appDataPath, DataDirectoryName);
            }
        }

        public static string DataDirectoryForShell => ResolveDataDirectoryForShell(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            DataDirectory,
            TryGetPackagedLocalCacheDirectory());

        public static string SettingsPath => Path.Combine(DataDirectory, "settings.json");

        public static string DatabasePath => Path.Combine(DataDirectory, "timepilot.db");

        public static string BackupDirectory => Path.Combine(DataDirectory, "backups");

        public static string EnsureDataDirectoryForShell()
        {
            Directory.CreateDirectory(DataDirectory);

            var directory = DataDirectoryForShell;
            Directory.CreateDirectory(directory);
            return directory;
        }

        internal static string ResolveDataDirectoryForShell(
            string localAppDataDirectory,
            string logicalDataDirectory,
            string? packagedLocalCacheDirectory)
        {
            if (string.IsNullOrWhiteSpace(packagedLocalCacheDirectory))
                return logicalDataDirectory;

            var relativeDirectory = Path.GetRelativePath(localAppDataDirectory, logicalDataDirectory);
            if (Path.IsPathRooted(relativeDirectory)
                || relativeDirectory.Equals("..", StringComparison.Ordinal)
                || relativeDirectory.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                return logicalDataDirectory;
            }

            return Path.GetFullPath(Path.Combine(
                packagedLocalCacheDirectory,
                "Local",
                relativeDirectory));
        }

        private static string? TryGetPackagedLocalCacheDirectory()
        {
            try
            {
                _ = Windows.ApplicationModel.Package.Current;
                return Windows.Storage.ApplicationData.Current.LocalCacheFolder.Path;
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }
    }
}
