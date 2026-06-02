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

            var databaseInspection = InspectDatabaseEntry(archive, createdAt);
            return new DataBackupRestorePlan(
                hasDatabase,
                hasSettings,
                logCount,
                createdAt,
                databaseInspection.BackupCounts,
                databaseInspection.CurrentCountsAfterBackup,
                databaseInspection.PreviewAnalysis);
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

        private static DataBackupDatabaseInspection InspectDatabaseEntry(ZipArchive archive, DateTimeOffset? createdAt)
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
                var backupCounts = CountDatabaseRecords(connection);
                var previewAnalysis = AnalyzeCurrentRecordsAfterBackup(tempPath, createdAt);

                return new DataBackupDatabaseInspection(
                    backupCounts,
                    previewAnalysis.CurrentCountsAfterBackup,
                    previewAnalysis);
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

        private static DataBackupPreviewAnalysis AnalyzeCurrentRecordsAfterBackup(
            string backupDatabasePath,
            DateTimeOffset? createdAt)
        {
            if (createdAt is null || !File.Exists(AppDataPaths.DatabasePath))
                return DataBackupPreviewAnalysis.Empty;

            try
            {
                using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
                {
                    DataSource = AppDataPaths.DatabasePath,
                    Mode = SqliteOpenMode.ReadOnly,
                    Pooling = false
                }.ToString());
                connection.Open();
                AttachBackupDatabase(connection, backupDatabasePath);

                var currentCountsAfterBackup = new DataBackupRecordCounts(
                    Apps: CountRowsAfter(connection, "apps", "first_seen_at", createdAt.Value),
                    ForegroundSessions: CountRowsAfter(connection, "foreground_sessions", "started_at", createdAt.Value),
                    IdleSessions: CountRowsAfter(connection, "idle_sessions", "started_at", createdAt.Value),
                    AppRuntimeSessions: CountRowsAfter(connection, "app_runtime_sessions", "started_at", createdAt.Value),
                    ProcessRuntimeSessions: CountRowsAfter(connection, "process_runtime_sessions", "started_at", createdAt.Value),
                    SystemEvents: CountRowsAfter(connection, "system_events", "occurred_at", createdAt.Value));

                var excludedOverlapCounts = new DataBackupRecordCounts(
                    Apps: 0,
                    ForegroundSessions: CountOverlappingRowsAfter(
                        connection,
                        "foreground_sessions",
                        "current_row.started_at",
                        "COALESCE(current_row.ended_at, current_row.last_observed_at, current_row.started_at)",
                        "backup_row.started_at",
                        "COALESCE(backup_row.ended_at, backup_row.last_observed_at, backup_row.started_at)",
                        createdAt.Value),
                    IdleSessions: CountOverlappingRowsAfter(
                        connection,
                        "idle_sessions",
                        "current_row.started_at",
                        "COALESCE(current_row.ended_at, current_row.started_at)",
                        "backup_row.started_at",
                        "COALESCE(backup_row.ended_at, backup_row.started_at)",
                        createdAt.Value),
                    AppRuntimeSessions: CountOverlappingRowsAfter(
                        connection,
                        "app_runtime_sessions",
                        "current_row.started_at",
                        "COALESCE(current_row.ended_at, current_row.last_heartbeat_at, current_row.started_at)",
                        "backup_row.started_at",
                        "COALESCE(backup_row.ended_at, backup_row.last_heartbeat_at, backup_row.started_at)",
                        createdAt.Value),
                    ProcessRuntimeSessions: CountOverlappingProcessRuntimeRowsAfter(connection, createdAt.Value),
                    SystemEvents: CountMatchingSystemEventsAfter(connection, createdAt.Value));

                var importableCounts = new DataBackupRecordCounts(
                    Apps: Math.Max(0, CountNewAppCandidates(connection, createdAt.Value)),
                    ForegroundSessions: Math.Max(0, currentCountsAfterBackup.ForegroundSessions - excludedOverlapCounts.ForegroundSessions),
                    IdleSessions: Math.Max(0, currentCountsAfterBackup.IdleSessions - excludedOverlapCounts.IdleSessions),
                    AppRuntimeSessions: Math.Max(0, currentCountsAfterBackup.AppRuntimeSessions - excludedOverlapCounts.AppRuntimeSessions),
                    ProcessRuntimeSessions: Math.Max(0, currentCountsAfterBackup.ProcessRuntimeSessions - excludedOverlapCounts.ProcessRuntimeSessions),
                    SystemEvents: Math.Max(0, currentCountsAfterBackup.SystemEvents - excludedOverlapCounts.SystemEvents));

                return new DataBackupPreviewAnalysis(
                    currentCountsAfterBackup,
                    importableCounts,
                    excludedOverlapCounts,
                    NewAppCandidates: importableCounts.Apps,
                    AppMatchConflictCandidates: CountAppMatchConflictCandidates(connection, createdAt.Value));
            }
            catch
            {
                return DataBackupPreviewAnalysis.Empty;
            }
            finally
            {
                SqliteConnection.ClearAllPools();
            }
        }

        private static void AttachBackupDatabase(SqliteConnection connection, string backupDatabasePath)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "ATTACH DATABASE $backupPath AS backup;";
            command.Parameters.AddWithValue("$backupPath", backupDatabasePath);
            command.ExecuteNonQuery();
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

        private static int CountOverlappingRowsAfter(
            SqliteConnection connection,
            string tableName,
            string currentStartExpression,
            string currentEndExpression,
            string backupStartExpression,
            string backupEndExpression,
            DateTimeOffset startedAfter)
        {
            if (!TableExists(connection, tableName) || !TableExists(connection, $"backup.{tableName}"))
                return 0;

            using var command = connection.CreateCommand();
            command.CommandText = $"""
                SELECT COUNT(*)
                FROM {tableName} current_row
                WHERE current_row.started_at >= $startedAfter
                  AND EXISTS (
                      SELECT 1
                      FROM backup.{tableName} backup_row
                      WHERE {currentStartExpression} < {backupEndExpression}
                        AND {currentEndExpression} > {backupStartExpression}
                  );
                """;
            command.Parameters.AddWithValue("$startedAfter", startedAfter.ToUniversalTime().ToString("O", System.Globalization.CultureInfo.InvariantCulture));
            return Convert.ToInt32(command.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
        }

        private static int CountOverlappingProcessRuntimeRowsAfter(SqliteConnection connection, DateTimeOffset startedAfter)
        {
            if (!TableExists(connection, "process_runtime_sessions")
                || !TableExists(connection, "backup.process_runtime_sessions")
                || !TableExists(connection, "apps")
                || !TableExists(connection, "backup.apps"))
                return 0;

            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT COUNT(*)
                FROM process_runtime_sessions current_row
                JOIN apps current_app ON current_app.id = current_row.app_id
                WHERE current_row.started_at >= $startedAfter
                  AND EXISTS (
                      SELECT 1
                      FROM backup.process_runtime_sessions backup_row
                      JOIN backup.apps backup_app ON backup_app.id = backup_row.app_id
                      WHERE current_app.process_name = backup_app.process_name
                        AND current_row.started_at < COALESCE(backup_row.ended_at, backup_row.last_observed_at, backup_row.started_at)
                        AND COALESCE(current_row.ended_at, current_row.last_observed_at, current_row.started_at) > backup_row.started_at
                  );
                """;
            command.Parameters.AddWithValue("$startedAfter", startedAfter.ToUniversalTime().ToString("O", System.Globalization.CultureInfo.InvariantCulture));
            return Convert.ToInt32(command.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
        }

        private static int CountMatchingSystemEventsAfter(SqliteConnection connection, DateTimeOffset startedAfter)
        {
            if (!TableExists(connection, "system_events") || !TableExists(connection, "backup.system_events"))
                return 0;

            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT COUNT(*)
                FROM system_events current_row
                WHERE current_row.occurred_at >= $startedAfter
                  AND EXISTS (
                      SELECT 1
                      FROM backup.system_events backup_row
                      WHERE backup_row.event_type = current_row.event_type
                        AND backup_row.occurred_at = current_row.occurred_at
                  );
                """;
            command.Parameters.AddWithValue("$startedAfter", startedAfter.ToUniversalTime().ToString("O", System.Globalization.CultureInfo.InvariantCulture));
            return Convert.ToInt32(command.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
        }

        private static int CountNewAppCandidates(SqliteConnection connection, DateTimeOffset startedAfter)
        {
            if (!TableExists(connection, "apps") || !TableExists(connection, "backup.apps"))
                return 0;

            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT COUNT(*)
                FROM apps current_app
                WHERE current_app.first_seen_at >= $startedAfter
                  AND NOT EXISTS (
                      SELECT 1
                      FROM backup.apps backup_app
                      WHERE backup_app.process_name = current_app.process_name
                        AND (
                            backup_app.executable_path = current_app.executable_path
                            OR backup_app.executable_path IS NULL
                            OR current_app.executable_path IS NULL
                        )
                  );
                """;
            command.Parameters.AddWithValue("$startedAfter", startedAfter.ToUniversalTime().ToString("O", System.Globalization.CultureInfo.InvariantCulture));
            return Convert.ToInt32(command.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
        }

        private static int CountAppMatchConflictCandidates(SqliteConnection connection, DateTimeOffset startedAfter)
        {
            if (!TableExists(connection, "apps") || !TableExists(connection, "backup.apps"))
                return 0;

            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT COUNT(*)
                FROM apps current_app
                WHERE current_app.first_seen_at >= $startedAfter
                  AND current_app.executable_path IS NOT NULL
                  AND EXISTS (
                      SELECT 1
                      FROM backup.apps backup_app
                      WHERE backup_app.process_name = current_app.process_name
                        AND backup_app.executable_path IS NOT NULL
                        AND backup_app.executable_path <> current_app.executable_path
                  );
                """;
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
            var schemaName = "main";
            var actualTableName = tableName;
            var separatorIndex = tableName.IndexOf('.', StringComparison.Ordinal);
            if (separatorIndex > 0 && separatorIndex < tableName.Length - 1)
            {
                schemaName = tableName[..separatorIndex];
                actualTableName = tableName[(separatorIndex + 1)..];
            }

            using var command = connection.CreateCommand();
            command.CommandText = $"""
                SELECT COUNT(*)
                FROM {schemaName}.sqlite_master
                WHERE type = 'table'
                  AND name = $tableName;
                """;
            command.Parameters.AddWithValue("$tableName", actualTableName);
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
        DataBackupRecordCounts CurrentCountsAfterBackup,
        DataBackupPreviewAnalysis PreviewAnalysis);

    internal sealed record DataBackupRestoreResult(IReadOnlyList<string> RestoredFiles);

    internal sealed record DataBackupDatabaseInspection(
        DataBackupRecordCounts BackupCounts,
        DataBackupRecordCounts CurrentCountsAfterBackup,
        DataBackupPreviewAnalysis PreviewAnalysis);

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

    internal sealed record DataBackupPreviewAnalysis(
        DataBackupRecordCounts CurrentCountsAfterBackup,
        DataBackupRecordCounts ImportableCounts,
        DataBackupRecordCounts ExcludedOverlapCounts,
        int NewAppCandidates,
        int AppMatchConflictCandidates)
    {
        public static DataBackupPreviewAnalysis Empty { get; } = new(
            DataBackupRecordCounts.Empty,
            DataBackupRecordCounts.Empty,
            DataBackupRecordCounts.Empty,
            NewAppCandidates: 0,
            AppMatchConflictCandidates: 0);
    }

    internal sealed record DataBackupMetadata(
        int SchemaVersion,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset CreatedAtLocal,
        string? AppVersion);
}
