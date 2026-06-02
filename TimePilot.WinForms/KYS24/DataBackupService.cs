using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace TimePilot.WinForms.KYS24
{
    internal sealed class DataBackupService
    {
        private const string ReadmeEntryName = "README.txt";
        private const string MetadataEntryName = "metadata.json";
        private const string DatabaseEntryName = "timepilot.db";
        private const string SettingsEntryName = "settings.json";
        private const string LogsDirectoryName = "logs";

        private static readonly UTF8Encoding Utf8WithBom = new(encoderShouldEmitUTF8Identifier: true);
        private static readonly string[] RequiredDatabaseTables =
        [
            "apps",
            "foreground_sessions",
            "idle_sessions",
            "app_runtime_sessions",
            "process_runtime_sessions"
        ];

        public IReadOnlyList<string> CreateBackup(string zipFilePath, DateTimeOffset createdAt)
        {
            var directory = Path.GetDirectoryName(zipFilePath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            var tempFilePath = Path.Combine(
                string.IsNullOrWhiteSpace(directory) ? Path.GetTempPath() : directory,
                $"TimePilot-backup-{Guid.NewGuid():N}.tmp");

            var entries = new List<string>();
            try
            {
                using (var archive = ZipFile.Open(tempFilePath, ZipArchiveMode.Create))
                {
                    WriteTextEntry(archive, MetadataEntryName, BuildMetadata(createdAt));
                    entries.Add(MetadataEntryName);

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
                }

                File.Move(tempFilePath, zipFilePath, overwrite: true);
            }
            finally
            {
                TryDeleteFile(tempFilePath);
            }

            return entries;
        }

        public DataBackupRestorePlan InspectBackup(string zipFilePath)
        {
            using var archive = OpenBackupArchive(zipFilePath);
            var hasDatabase = archive.GetEntry(DatabaseEntryName) is not null;
            var hasSettings = archive.GetEntry(SettingsEntryName) is not null;
            var logCount = archive.Entries.Count(entry =>
                entry.FullName.StartsWith($"{LogsDirectoryName}/", StringComparison.Ordinal)
                && !string.IsNullOrWhiteSpace(entry.Name));
            var createdAt = ReadMetadataCreatedAt(archive);

            if (!hasDatabase)
                throw new InvalidDataException(UiText.Main.DataRestoreMissingDatabase);

            var backupCounts = ValidateDatabaseEntry(archive);
            var currentCountsAfterBackup = CountCurrentRecordsAfter(createdAt);
            return new DataBackupRestorePlan(
                hasDatabase,
                hasSettings,
                logCount,
                createdAt,
                backupCounts,
                currentCountsAfterBackup);
        }

        public DataBackupRestoreResult RestoreBackup(string zipFilePath)
        {
            var plan = InspectBackup(zipFilePath);
            Directory.CreateDirectory(AppDataPaths.DataDirectory);

            SqliteConnection.ClearAllPools();
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

        private static ZipArchive OpenBackupArchive(string zipFilePath)
        {
            try
            {
                return ZipFile.OpenRead(zipFilePath);
            }
            catch (Exception ex) when (ex is InvalidDataException or IOException or UnauthorizedAccessException)
            {
                throw new InvalidDataException(UiText.Main.DataRestoreInvalidBackup(ex.Message), ex);
            }
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
                           Mode = SqliteOpenMode.ReadOnly,
                           Pooling = false
                       }.ToString()))
                using (var destination = new SqliteConnection(new SqliteConnectionStringBuilder
                       {
                           DataSource = tempPath,
                           Pooling = false
                       }.ToString()))
                {
                    source.Open();
                    destination.Open();
                    source.BackupDatabase(destination);
                }

                SqliteConnection.ClearAllPools();
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

        private static DataBackupRecordCounts ValidateDatabaseEntry(ZipArchive archive)
        {
            var entry = archive.GetEntry(DatabaseEntryName);
            if (entry is null)
                throw new InvalidDataException(UiText.Main.DataRestoreMissingDatabase);

            var tempPath = Path.Combine(
                Path.GetTempPath(),
                $"TimePilot-restore-validate-{Guid.NewGuid():N}.db");

            try
            {
                entry.ExtractToFile(tempPath, overwrite: true);
                using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
                {
                    DataSource = tempPath,
                    Mode = SqliteOpenMode.ReadOnly,
                    Pooling = false
                }.ToString());
                connection.Open();

                ValidateIntegrity(connection);
                ValidateRequiredTables(connection);
                return CountDatabaseRecords(connection);
            }
            catch (InvalidDataException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidDataException(UiText.Main.DataRestoreInvalidBackup(ex.Message), ex);
            }
            finally
            {
                SqliteConnection.ClearAllPools();
                TryDeleteFile(tempPath);
            }
        }

        private static DataBackupRecordCounts CountCurrentRecordsAfter(DateTimeOffset? createdAt)
        {
            if (createdAt is null || !File.Exists(AppDataPaths.DatabasePath))
                return DataBackupRecordCounts.Empty;

            try
            {
                using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
                {
                    DataSource = AppDataPaths.DatabasePath,
                    Mode = SqliteOpenMode.ReadOnly,
                    Pooling = false
                }.ToString());
                connection.Open();

                return new DataBackupRecordCounts(
                    Apps: CountRowsAfter(connection, "apps", "first_seen_at", createdAt.Value),
                    ForegroundSessions: CountRowsAfter(connection, "foreground_sessions", "started_at", createdAt.Value),
                    IdleSessions: CountRowsAfter(connection, "idle_sessions", "started_at", createdAt.Value),
                    AppRuntimeSessions: CountRowsAfter(connection, "app_runtime_sessions", "started_at", createdAt.Value),
                    ProcessRuntimeSessions: CountRowsAfter(connection, "process_runtime_sessions", "started_at", createdAt.Value),
                    SystemEvents: CountRowsAfter(connection, "system_events", "occurred_at", createdAt.Value));
            }
            catch
            {
                return DataBackupRecordCounts.Empty;
            }
            finally
            {
                SqliteConnection.ClearAllPools();
            }
        }

        private static DataBackupRecordCounts CountDatabaseRecords(SqliteConnection connection)
        {
            return new DataBackupRecordCounts(
                Apps: CountRows(connection, "apps"),
                ForegroundSessions: CountRows(connection, "foreground_sessions"),
                IdleSessions: CountRows(connection, "idle_sessions"),
                AppRuntimeSessions: CountRows(connection, "app_runtime_sessions"),
                ProcessRuntimeSessions: CountRows(connection, "process_runtime_sessions"),
                SystemEvents: TableExists(connection, "system_events") ? CountRows(connection, "system_events") : 0);
        }

        private static int CountRows(SqliteConnection connection, string tableName)
        {
            if (!TableExists(connection, tableName))
                return 0;

            using var command = connection.CreateCommand();
            command.CommandText = $"SELECT COUNT(*) FROM {tableName};";
            return Convert.ToInt32(command.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
        }

        private static int CountRowsAfter(
            SqliteConnection connection,
            string tableName,
            string columnName,
            DateTimeOffset startedAfter)
        {
            if (!TableExists(connection, tableName))
                return 0;

            using var command = connection.CreateCommand();
            command.CommandText = $"SELECT COUNT(*) FROM {tableName} WHERE {columnName} >= $startedAfter;";
            command.Parameters.AddWithValue("$startedAfter", startedAfter.ToUniversalTime().ToString("O", System.Globalization.CultureInfo.InvariantCulture));
            return Convert.ToInt32(command.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
        }

        private static void ValidateIntegrity(SqliteConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA integrity_check;";
            var result = Convert.ToString(command.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
            if (!string.Equals(result, "ok", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException(UiText.Main.DataRestoreInvalidBackup(result ?? ""));
        }

        private static void ValidateRequiredTables(SqliteConnection connection)
        {
            var missingTables = RequiredDatabaseTables
                .Where(tableName => !TableExists(connection, tableName))
                .ToList();
            if (missingTables.Count == 0)
                return;

            throw new InvalidDataException(UiText.Main.DataRestoreInvalidBackup(
                $"Missing tables: {string.Join(", ", missingTables)}"));
        }

        private static bool TableExists(SqliteConnection connection, string tableName)
        {
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT COUNT(*)
                FROM sqlite_master
                WHERE type = 'table'
                  AND name = $tableName;
                """;
            command.Parameters.AddWithValue("$tableName", tableName);
            return Convert.ToInt32(command.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture) > 0;
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
            builder.AppendLine($"- {MetadataEntryName}");
            builder.AppendLine($"- {DatabaseEntryName}");
            builder.AppendLine($"- {SettingsEntryName}");
            builder.AppendLine($"- {LogsDirectoryName}/");
            return builder.ToString();
        }

        private static string BuildMetadata(DateTimeOffset createdAt)
        {
            var metadata = new DataBackupMetadata(
                SchemaVersion: 1,
                CreatedAtUtc: createdAt.ToUniversalTime(),
                CreatedAtLocal: createdAt.ToLocalTime(),
                AppVersion: typeof(DataBackupService).Assembly.GetName().Version?.ToString());

            return JsonSerializer.Serialize(metadata, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }

        private static DateTimeOffset? ReadMetadataCreatedAt(ZipArchive archive)
        {
            var entry = archive.GetEntry(MetadataEntryName);
            if (entry is null)
                return null;

            try
            {
                using var stream = entry.Open();
                var metadata = JsonSerializer.Deserialize<DataBackupMetadata>(stream);
                return metadata?.CreatedAtUtc;
            }
            catch
            {
                return null;
            }
        }

        private static void WriteTextEntry(ZipArchive archive, string entryName, string content)
        {
            var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
            using var stream = entry.Open();
            using var writer = new StreamWriter(stream, Utf8WithBom);
            writer.Write(content);
        }
    }

    internal sealed record DataBackupRestorePlan(
        bool HasDatabase,
        bool HasSettings,
        int LogCount,
        DateTimeOffset? CreatedAt,
        DataBackupRecordCounts BackupCounts,
        DataBackupRecordCounts CurrentCountsAfterBackup);

    internal sealed record DataBackupRestoreResult(IReadOnlyList<string> RestoredFiles);

    internal sealed record DataBackupRecordCounts(
        int Apps,
        int ForegroundSessions,
        int IdleSessions,
        int AppRuntimeSessions,
        int ProcessRuntimeSessions,
        int SystemEvents)
    {
        public static DataBackupRecordCounts Empty { get; } = new(0, 0, 0, 0, 0, 0);

        public int TotalUsageRecords =>
            ForegroundSessions + IdleSessions + AppRuntimeSessions + ProcessRuntimeSessions + SystemEvents;
    }

    internal sealed record DataBackupMetadata(
        int SchemaVersion,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset CreatedAtLocal,
        string? AppVersion);
}
