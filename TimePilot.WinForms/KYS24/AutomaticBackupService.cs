namespace TimePilot.WinForms.KYS24
{
    internal sealed class AutomaticBackupService
    {
        internal const string FileNamePrefix = "ActiveLogbook-auto-backup-";

        private readonly string _dataDirectory;

        public AutomaticBackupService()
            : this(AppDataPaths.DataDirectory)
        {
        }

        internal AutomaticBackupService(string dataDirectory)
        {
            if (string.IsNullOrWhiteSpace(dataDirectory))
                throw new ArgumentException("The data directory is required.", nameof(dataDirectory));

            _dataDirectory = Path.GetFullPath(dataDirectory);
        }

        public AutomaticBackupResult CreateIfDue(
            string backupDirectory,
            int retentionCount,
            DateTimeOffset now,
            DateTimeOffset? lastSuccessAt)
        {
            if (string.IsNullOrWhiteSpace(backupDirectory))
                throw new ArgumentException("The backup directory is required.", nameof(backupDirectory));

            var databasePath = Path.Combine(_dataDirectory, "timepilot.db");
            if (!File.Exists(databasePath))
                return new AutomaticBackupResult(AutomaticBackupResultKind.SkippedNoDatabase, null, 0);

            var databaseLastWriteAt = new DateTimeOffset(File.GetLastWriteTimeUtc(databasePath), TimeSpan.Zero);
            if (lastSuccessAt is { } previousSuccess)
            {
                if (databaseLastWriteAt <= previousSuccess.ToUniversalTime())
                    return new AutomaticBackupResult(AutomaticBackupResultKind.SkippedUnchanged, null, 0);

                if (previousSuccess.ToLocalTime().Date == now.ToLocalTime().Date)
                    return new AutomaticBackupResult(AutomaticBackupResultKind.SkippedAlreadyCreatedToday, null, 0);
            }

            var targetDirectory = Path.GetFullPath(backupDirectory);
            if (IsSameOrChildPath(targetDirectory, _dataDirectory))
            {
                throw new InvalidOperationException(
                    UiText.Preferences.AutomaticBackupFolderMustBeExternal);
            }

            Directory.CreateDirectory(targetDirectory);
            var backupPath = Path.Combine(
                targetDirectory,
                $"{FileNamePrefix}{now.ToLocalTime():yyyy-MM-dd-HHmmss}.zip");

            var backupService = new DataBackupService(_dataDirectory);
            backupService.CreateBackup(backupPath, now);
            var deletedCount = ApplyRetentionPolicy(targetDirectory, Math.Max(1, retentionCount), backupPath);
            return new AutomaticBackupResult(AutomaticBackupResultKind.Created, backupPath, deletedCount);
        }

        private static bool IsSameOrChildPath(string candidatePath, string parentPath)
        {
            var normalizedCandidate = Path.TrimEndingDirectorySeparator(Path.GetFullPath(candidatePath));
            var normalizedParent = Path.TrimEndingDirectorySeparator(Path.GetFullPath(parentPath));
            return normalizedCandidate.Equals(normalizedParent, StringComparison.OrdinalIgnoreCase)
                || normalizedCandidate.StartsWith(
                    normalizedParent + Path.DirectorySeparatorChar,
                    StringComparison.OrdinalIgnoreCase);
        }

        private static int ApplyRetentionPolicy(
            string backupDirectory,
            int retentionCount,
            string createdBackupPath)
        {
            var automaticBackups = Directory
                .EnumerateFiles(backupDirectory, $"{FileNamePrefix}*.zip", SearchOption.TopDirectoryOnly)
                .Select(path => new FileInfo(path))
                .OrderByDescending(file => file.LastWriteTimeUtc)
                .ThenByDescending(file => file.Name, StringComparer.Ordinal)
                .ToList();
            var deletedCount = 0;
            foreach (var oldBackup in automaticBackups.Skip(retentionCount))
            {
                if (string.Equals(oldBackup.FullName, createdBackupPath, StringComparison.OrdinalIgnoreCase))
                    continue;

                oldBackup.Delete();
                deletedCount++;
            }

            return deletedCount;
        }
    }

    internal enum AutomaticBackupResultKind
    {
        Created,
        SkippedNoDatabase,
        SkippedUnchanged,
        SkippedAlreadyCreatedToday
    }

    internal sealed record AutomaticBackupResult(
        AutomaticBackupResultKind Kind,
        string? BackupPath,
        int DeletedBackupCount);
}
