using System.IO.Compression;
using System.Text;
using Microsoft.Data.Sqlite;

namespace TimePilot.WinForms.KYS24
{
    internal sealed class DataBackupService
    {
        private const string ReadmeEntryName = "README.txt";
        private const string DatabaseEntryName = "timepilot.db";
        private const string SettingsEntryName = "settings.json";
        private const string LogsDirectoryName = "logs";

        private static readonly UTF8Encoding Utf8WithBom = new(encoderShouldEmitUTF8Identifier: true);

        public IReadOnlyList<string> CreateBackup(string zipFilePath, DateTimeOffset createdAt)
        {
            var directory = Path.GetDirectoryName(zipFilePath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            if (File.Exists(zipFilePath))
                File.Delete(zipFilePath);

            using var archive = ZipFile.Open(zipFilePath, ZipArchiveMode.Create);
            var entries = new List<string>();

            WriteTextEntry(archive, ReadmeEntryName, BuildReadme(createdAt));
            entries.Add(ReadmeEntryName);

            AddDatabaseIfExists(archive, entries);
            AddFileIfExists(archive, AppDataPaths.SettingsPath, SettingsEntryName, entries);

            var logsDirectory = Path.Combine(AppDataPaths.DataDirectory, LogsDirectoryName);
            if (Directory.Exists(logsDirectory))
            {
                foreach (var logFile in Directory.EnumerateFiles(logsDirectory))
                {
                    var entryName = $"{LogsDirectoryName}/{Path.GetFileName(logFile)}";
                    AddFileIfExists(archive, logFile, entryName, entries);
                }
            }

            return entries;
        }

        public DataBackupRestorePlan InspectBackup(string zipFilePath)
        {
            using var archive = ZipFile.OpenRead(zipFilePath);
            var hasDatabase = archive.GetEntry(DatabaseEntryName) is not null;
            var hasSettings = archive.GetEntry(SettingsEntryName) is not null;
            var logCount = archive.Entries.Count(entry =>
                entry.FullName.StartsWith($"{LogsDirectoryName}/", StringComparison.Ordinal)
                && !string.IsNullOrWhiteSpace(entry.Name));

            if (!hasDatabase)
                throw new InvalidDataException(UiText.Main.DataRestoreMissingDatabase);

            return new DataBackupRestorePlan(hasDatabase, hasSettings, logCount);
        }

        public DataBackupRestoreResult RestoreBackup(string zipFilePath)
        {
            var plan = InspectBackup(zipFilePath);
            Directory.CreateDirectory(AppDataPaths.DataDirectory);

            using var archive = ZipFile.OpenRead(zipFilePath);
            var restoredFiles = new List<string>();
            RestoreEntry(archive, DatabaseEntryName, AppDataPaths.DatabasePath, restoredFiles);

            if (plan.HasSettings)
                RestoreEntry(archive, SettingsEntryName, AppDataPaths.SettingsPath, restoredFiles);

            var logEntries = archive.Entries
                .Where(entry =>
                    entry.FullName.StartsWith($"{LogsDirectoryName}/", StringComparison.Ordinal)
                    && !string.IsNullOrWhiteSpace(entry.Name))
                .ToList();
            if (logEntries.Count > 0)
            {
                var logsDirectory = Path.Combine(AppDataPaths.DataDirectory, LogsDirectoryName);
                Directory.CreateDirectory(logsDirectory);
                foreach (var entry in logEntries)
                {
                    var destinationPath = Path.Combine(logsDirectory, Path.GetFileName(entry.FullName));
                    entry.ExtractToFile(destinationPath, overwrite: true);
                    restoredFiles.Add(entry.FullName);
                }
            }

            return new DataBackupRestoreResult(restoredFiles);
        }

        private static void AddFileIfExists(
            ZipArchive archive,
            string sourcePath,
            string entryName,
            List<string> entries)
        {
            if (!File.Exists(sourcePath))
                return;

            archive.CreateEntryFromFile(sourcePath, entryName, CompressionLevel.Optimal);
            entries.Add(entryName);
        }

        private static void AddDatabaseIfExists(ZipArchive archive, List<string> entries)
        {
            if (!File.Exists(AppDataPaths.DatabasePath))
                return;

            var tempPath = Path.Combine(
                Path.GetTempPath(),
                $"TimePilot-backup-{Guid.NewGuid():N}.db");

            try
            {
                using (var source = new SqliteConnection(new SqliteConnectionStringBuilder
                       {
                           DataSource = AppDataPaths.DatabasePath,
                           Mode = SqliteOpenMode.ReadOnly
                       }.ToString()))
                using (var destination = new SqliteConnection(new SqliteConnectionStringBuilder
                       {
                           DataSource = tempPath
                       }.ToString()))
                {
                    source.Open();
                    destination.Open();
                    source.BackupDatabase(destination);
                }

                archive.CreateEntryFromFile(tempPath, DatabaseEntryName, CompressionLevel.Optimal);
                entries.Add(DatabaseEntryName);
            }
            finally
            {
                TryDeleteFile(tempPath);
            }
        }

        private static void TryDeleteFile(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
            }
        }

        private static void RestoreEntry(
            ZipArchive archive,
            string entryName,
            string destinationPath,
            List<string> restoredFiles)
        {
            var entry = archive.GetEntry(entryName);
            if (entry is null)
                return;

            var directory = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            entry.ExtractToFile(destinationPath, overwrite: true);
            restoredFiles.Add(entryName);
        }

        private static string BuildReadme(DateTimeOffset createdAt)
        {
            var builder = new StringBuilder();
            builder.AppendLine(UiText.Main.DataBackupReadmeTitle);
            builder.AppendLine();
            builder.AppendLine(UiText.Main.DataBackupReadmeCreatedAt(createdAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")));
            builder.AppendLine();
            builder.AppendLine(UiText.Main.DataBackupReadmePrivacyNotice);
            builder.AppendLine();
            builder.AppendLine(UiText.Main.DataBackupReadmeFileList);
            builder.AppendLine($"- {DatabaseEntryName}");
            builder.AppendLine($"- {SettingsEntryName}");
            builder.AppendLine($"- {LogsDirectoryName}/");
            return builder.ToString();
        }

        private static void WriteTextEntry(ZipArchive archive, string entryName, string content)
        {
            var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
            using var stream = entry.Open();
            using var writer = new StreamWriter(stream, Utf8WithBom);
            writer.Write(content);
        }
    }

    internal sealed record DataBackupRestorePlan(bool HasDatabase, bool HasSettings, int LogCount);

    internal sealed record DataBackupRestoreResult(IReadOnlyList<string> RestoredFiles);
}
