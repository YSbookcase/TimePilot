namespace TimePilot.WinForms.KYS24
{
    internal sealed record DevelopmentDataOptions(string DataDirectory)
    {
        public const string Argument = "--data-dir";

        public static DevelopmentDataOptions? Parse(string[] args, bool isPackaged)
        {
            if (!args.Any(arg => arg == Argument || arg.StartsWith(Argument + "=", StringComparison.Ordinal)))
                return null;

            if (isPackaged)
                throw new ArgumentException("--data-dir is supported only by an unpackaged development build.");

            string? directory = null;
            for (var i = 0; i < args.Length; i++)
            {
                if (args[i] == Argument && directory is null && i + 1 < args.Length)
                    directory = args[++i];
                else if (args[i] != WindowsStartupRegistration.TrayStartupArgument)
                    throw new ArgumentException("Use --data-dir <absolute folder> with no maintenance or sample-data commands.");
            }

            if (string.IsNullOrWhiteSpace(directory) || !Path.IsPathFullyQualified(directory))
                throw new ArgumentException("--data-dir requires an absolute path to an existing data folder.");

            return new DevelopmentDataOptions(Path.GetFullPath(directory));
        }
    }

    internal sealed record DevelopmentDataPreparation(string DataDirectory, string SafetyBackupPath);

    internal static class DevelopmentDataSession
    {
        public static DevelopmentDataPreparation Prepare(string directory, string backupDirectory)
        {
            if (!Path.IsPathFullyQualified(directory))
                throw new ArgumentException("The development data folder must be an absolute path.");
            if (!Path.IsPathFullyQualified(backupDirectory))
                throw new ArgumentException("The safety backup folder must be an absolute path.");

            var fullPath = Path.GetFullPath(directory);
            if (!Directory.Exists(fullPath)
                || !File.Exists(Path.Combine(fullPath, "timepilot.db"))
                || !File.Exists(Path.Combine(fullPath, "settings.json")))
            {
                throw new InvalidDataException("The selected folder must already contain timepilot.db and settings.json. No fallback folder will be used.");
            }

            AppSettings.ValidateSettingsFile(Path.Combine(fullPath, "settings.json"));

            var now = DateTimeOffset.UtcNow;
            var backupPath = Path.Combine(backupDirectory,
                $"ActiveLogbook-before-development-{now:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.zip");
            // Reuse SQLite snapshot backup and archive verification before the app can write data.
            new DataBackupService(fullPath).CreateBackup(backupPath, now);
            return new DevelopmentDataPreparation(fullPath, backupPath);
        }
    }
}
