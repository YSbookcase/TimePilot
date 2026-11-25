using System.Globalization;
using System.Security.Cryptography;
using Microsoft.Data.Sqlite;

namespace TimePilot.WinForms.KYS24;

internal sealed record KeepLaterStagingResult(string DatabasePath, string Sha256, string PreparationSha256,
    IReadOnlyDictionary<string, long> RecordCounts);

// Offline builder only. Inputs must be closed standalone snapshots; this never replaces live data.
internal static class KeepLaterStagingBuilder
{
    private static readonly Dictionary<string, string[]> Columns = new(StringComparer.Ordinal)
    {
        ["app_categories"] = ["id", "name", "color", "sort_order", "is_builtin", "created_at", "updated_at", "source"],
        ["apps"] = ["id", "process_name", "display_name", "executable_path", "first_seen_at", "last_seen_at", "primary_category_id", "primary_category_source", "primary_category_updated_at", "recommended_category_id", "recommended_category_reason", "recommended_category_confidence", "recommended_category_updated_at", "user_alias", "is_excluded"],
        ["app_observations"] = ["id", "app_id", "process_name", "display_name", "executable_path", "normalized_executable_path", "file_description", "product_name", "company_name", "first_seen_at", "last_seen_at", "observed_count"],
        ["app_runtime_sessions"] = ["id", "started_at", "ended_at", "duration_ms", "last_heartbeat_at", "shutdown_reason", "system_booted_at", "app_version"],
        ["foreground_sessions"] = ["id", "app_id", "started_at", "ended_at", "duration_ms", "last_observed_at"],
        ["idle_sessions"] = ["id", "started_at", "ended_at", "duration_ms", "threshold_ms", "foreground_app_id"],
        ["process_runtime_sessions"] = ["id", "app_id", "process_id", "started_at", "ended_at", "duration_ms", "first_observed_at", "last_observed_at", "tracking_scope", "has_main_window", "is_current_session_process"],
        ["system_events"] = ["id", "event_type", "occurred_at", "app_runtime_session_id", "system_booted_at", "details"]
    };
    private static readonly string[] RecordTables = ["app_runtime_sessions", "foreground_sessions", "idle_sessions", "process_runtime_sessions", "system_events"];

    public static KeepLaterStagingResult Build(string backupSnapshot, string currentSnapshot, string outputPath,
        KeepLaterRestorePreparation approved)
    {
        var backupPath = CheckSnapshot(backupSnapshot);
        var currentPath = CheckSnapshot(currentSnapshot);
        if (!Path.IsPathFullyQualified(outputPath) || !string.Equals(Path.GetExtension(outputPath), ".db", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Choose an absolute path for a new staging DB.");
        var output = Path.GetFullPath(outputPath);
        CheckReparsePoints(output);
        if (Path.Exists(output) || output.Equals(backupPath, StringComparison.OrdinalIgnoreCase) || output.Equals(currentPath, StringComparison.OrdinalIgnoreCase))
            throw new IOException("Staging output must be a new file, not an input database.");
        CheckSidecars(output);
        if (approved.SettingsChoice != RestoreSettingsChoice.KeepCurrent || approved.HasUnresolvedIssues || approved.ReferenceAt is null)
            throw new InvalidOperationException("Staging requires current settings and a preparation without unresolved issues.");
        if (!approved.MatchesSnapshotHashes(Hash(backupPath), Hash(currentPath)))
            throw new InvalidOperationException("Snapshot contents changed. Recreate and review preparation first.");

        using var backupLease = new FileStream(backupPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var currentLease = new FileStream(currentPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var backup = Open(backupPath, true);
        using var current = Open(currentPath, true);
        ValidateSource(backup);
        ValidateSource(current);
        var reference = approved.ReferenceAt.Value;
        var preview = KeepLaterRestorePreviewReader.Read(backup, current, reference, approved.ObservedAt);
        var recomputed = KeepLaterRestorePreparation.Build(preview, CategoryImportService.ReadState(backup),
            CategoryImportService.ReadState(current), Hash(backupPath), Hash(currentPath), RestoreSettingsChoice.KeepCurrent);
        if (recomputed.HasUnresolvedIssues || approved.PlanSha256 != recomputed.PlanSha256)
            throw new InvalidOperationException("Preparation is stale or does not match the frozen snapshots.");

        var parent = Path.GetDirectoryName(output)!;
        Directory.CreateDirectory(parent);
        var staging = Path.Combine(parent, $"ActiveLogbook-staging-{Guid.NewGuid():N}.db");
        var ownsStaging = false;
        try
        {
            using (var reserved = new FileStream(staging, FileMode.CreateNew, FileAccess.Write, FileShare.None)) ownsStaging = true;
            using (var destination = Open(staging, false))
            {
                DataBackupService.BackupDatabaseWithRetry(backup, destination);
                Execute(destination, "PRAGMA journal_mode=DELETE;");
                using (var transaction = destination.BeginTransaction())
                {
                    // Delete children first. Metadata stays backup-based, including observations of matched apps.
                    foreach (var table in RecordTables.Reverse()) Execute(destination, $"DELETE FROM {table};", transaction);
                    var categoryMap = CopyCategories(current, destination, transaction, approved.Categories);
                    var appMap = CopyApps(current, destination, transaction, approved.Apps, categoryMap);
                    var addedApps = approved.Apps.Where(a => a.Action == RestoreMappingAction.Add).Select(a => a.CurrentId).ToHashSet();
                    foreach (var row in ReadRows(current, "app_observations"))
                    {
                        if (!addedApps.Contains((long)row["app_id"])) continue;
                        row["app_id"] = Map(row["app_id"], appMap, "app observation");
                        Insert(destination, transaction, "app_observations", row, preserveId: false);
                    }
                    var backupApps = CategoryImportService.ReadState(backup).Apps.ToDictionary(a => a.Id, a => a.Id);
                    CopyRecords(backup, destination, transaction, reference, before: true, backupApps);
                    CopyRecords(current, destination, transaction, reference, before: false, appMap);
                    ValidateResult(destination, transaction, approved);
                    transaction.Commit();
                }
                ValidateIntegrity(destination);
            }
            // A changed input or journal means this result must not be published, even if SQL succeeded.
            CheckSnapshot(backupPath);
            CheckSnapshot(currentPath);
            if (!approved.MatchesSnapshotHashes(Hash(backupPath), Hash(currentPath)))
                throw new InvalidOperationException("An input changed while building staging. The result was discarded.");
            var hash = Hash(staging);
            var counts = approved.Records.ToDictionary(r => r.Table, r => (long)r.BackupBefore + r.CurrentAtOrAfter, StringComparer.Ordinal);
            CheckReparsePoints(output);
            CheckSidecars(output);
            File.Move(staging, output, overwrite: false);
            return new(output, hash, approved.PlanSha256, new System.Collections.ObjectModel.ReadOnlyDictionary<string, long>(counts));
        }
        finally
        {
            if (ownsStaging)
                foreach (var suffix in new[] { "", "-journal", "-wal", "-shm" })
                {
                    try { File.Delete(staging + suffix); }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
                }
        }
    }

    private static Dictionary<long, long> CopyCategories(SqliteConnection current, SqliteConnection output,
        SqliteTransaction tx, IReadOnlyList<RestoreIdentityMapping> mappings)
    {
        var map = new Dictionary<long, long>();
        var byId = mappings.ToDictionary(m => m.CurrentId);
        foreach (var row in ReadRows(current, "app_categories"))
        {
            var id = (long)row["id"];
            var candidate = byId[id];
            if (candidate.Action == RestoreMappingAction.Match) map[id] = BackupId(candidate);
            else if (candidate.Action == RestoreMappingAction.Add)
            {
                row["source"] = AppCategorySource.Import;
                map[id] = Insert(output, tx, "app_categories", row, false);
            }
            else throw new InvalidDataException("Unresolved category mapping.");
        }
        return map;
    }

    private static Dictionary<long, long> CopyApps(SqliteConnection current, SqliteConnection output,
        SqliteTransaction tx, IReadOnlyList<RestoreIdentityMapping> mappings, Dictionary<long, long> categories)
    {
        var map = new Dictionary<long, long>();
        var byId = mappings.ToDictionary(m => m.CurrentId);
        foreach (var row in ReadRows(current, "apps"))
        {
            var id = (long)row["id"];
            var candidate = byId[id];
            if (candidate.Action == RestoreMappingAction.Match) map[id] = BackupId(candidate);
            else if (candidate.Action == RestoreMappingAction.Add)
            {
                row["primary_category_id"] = Map(row["primary_category_id"], categories, "primary category");
                row["recommended_category_id"] = Map(row["recommended_category_id"], categories, "recommended category");
                map[id] = Insert(output, tx, "apps", row, false);
            }
            else throw new InvalidDataException("Unresolved app mapping.");
        }
        return map;
    }

    private static long BackupId(RestoreIdentityMapping mapping) => mapping.TargetKey?.StartsWith("backup:", StringComparison.Ordinal) == true
        && long.TryParse(mapping.TargetKey.AsSpan(7), NumberStyles.None, CultureInfo.InvariantCulture, out var id)
        ? id : throw new InvalidDataException("Invalid backup identity key.");

    private static void CopyRecords(SqliteConnection source, SqliteConnection output, SqliteTransaction tx,
        DateTimeOffset cutoff, bool before, Dictionary<long, long> apps)
    {
        var runtimes = new Dictionary<long, long>();
        foreach (var table in RecordTables)
        {
            foreach (var row in ReadRows(source, table))
            {
                var point = table == "system_events";
                var start = DateTimeOffset.Parse((string)row[point ? "occurred_at" : "started_at"], CultureInfo.InvariantCulture);
                var end = point ? start : DateTimeOffset.Parse((string)row["ended_at"], CultureInfo.InvariantCulture);
                var retain = before ? start < cutoff && (point || end <= cutoff) : start >= cutoff;
                if (!retain) continue;
                var originalId = (long)row["id"];
                if (table is "foreground_sessions" or "process_runtime_sessions") row["app_id"] = Map(row["app_id"], apps, "session app");
                if (table == "idle_sessions") row["foreground_app_id"] = Map(row["foreground_app_id"], apps, "idle app");
                if (table == "system_events") row["app_runtime_session_id"] = Map(row["app_runtime_session_id"], runtimes, "retained runtime parent");
                var insertedId = Insert(output, tx, table, row, preserveId: before);
                if (table == "app_runtime_sessions") runtimes[originalId] = insertedId;
            }
        }
    }

    private static object Map(object value, Dictionary<long, long> map, string field) => value is DBNull ? DBNull.Value
        : value is long id && map.TryGetValue(id, out var mapped) ? mapped
        : throw new InvalidDataException($"Cannot map {field}. No record will be silently omitted.");

    private static IEnumerable<Dictionary<string, object>> ReadRows(SqliteConnection source, string table)
    {
        using var command = source.CreateCommand();
        command.CommandText = $"SELECT {string.Join(',', Columns[table])} FROM {table} ORDER BY id;";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var row = new Dictionary<string, object>(StringComparer.Ordinal);
            for (var i = 0; i < reader.FieldCount; i++) row.Add(reader.GetName(i), reader.GetValue(i));
            yield return row;
        }
    }

    private static long Insert(SqliteConnection output, SqliteTransaction tx, string table, Dictionary<string, object> row, bool preserveId)
    {
        var columns = Columns[table].Where(c => preserveId || c != "id").ToArray();
        using var command = output.CreateCommand();
        command.Transaction = tx;
        command.CommandText = $"INSERT INTO {table} ({string.Join(',', columns)}) VALUES ({string.Join(',', columns.Select((_, i) => "$p" + i))}) RETURNING id;";
        for (var i = 0; i < columns.Length; i++) command.Parameters.AddWithValue("$p" + i, row[columns[i]]);
        return (long)command.ExecuteScalar()!;
    }

    private static void ValidateSource(SqliteConnection source)
    {
        ValidateIntegrity(source);
        using (var schema = source.CreateCommand())
        {
            schema.CommandText = "SELECT type,name FROM sqlite_master WHERE name NOT LIKE 'sqlite_%' AND type <> 'index';";
            using var reader = schema.ExecuteReader();
            while (reader.Read())
                if (reader.GetString(0) != "table" || !Columns.ContainsKey(reader.GetString(1)))
                    throw new InvalidDataException("Unsupported table, view or trigger. Staging requires the known standalone schema.");
        }
        foreach (var table in Columns)
        {
            using var command = source.CreateCommand();
            command.CommandText = $"PRAGMA table_xinfo({table.Key});";
            using var reader = command.ExecuteReader();
            var actual = new HashSet<string>(StringComparer.Ordinal);
            while (reader.Read())
            {
                if (reader.GetInt64(6) != 0) throw new InvalidDataException("Generated columns are not supported.");
                if (reader.GetString(1) == "id" && (reader.GetInt64(5) != 1 || !reader.GetString(2).Equals("INTEGER", StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidDataException("Staging requires an INTEGER primary key named id.");
                actual.Add(reader.GetString(1));
            }
            if (!actual.SetEquals(table.Value)) throw new InvalidDataException($"Unsupported columns in {table.Key}.");
        }
        foreach (var table in Columns.Keys)
        {
            var expected = table switch
            {
                "apps" => new[] { "primary_category_id:app_categories:id", "recommended_category_id:app_categories:id" },
                "app_observations" or "foreground_sessions" or "process_runtime_sessions" => ["app_id:apps:id"],
                "idle_sessions" => ["foreground_app_id:apps:id"],
                "system_events" => ["app_runtime_session_id:app_runtime_sessions:id"],
                _ => Array.Empty<string>()
            };
            using var command = source.CreateCommand();
            command.CommandText = $"PRAGMA foreign_key_list({table});";
            using var reader = command.ExecuteReader();
            var actual = new List<string>();
            while (reader.Read())
            {
                if (reader.GetInt64(1) != 0 || reader.GetString(5) != "NO ACTION" || reader.GetString(6) != "NO ACTION")
                    throw new InvalidDataException("Unsupported foreign key behavior.");
                actual.Add($"{reader.GetString(3)}:{reader.GetString(2)}:{reader.GetString(4)}");
            }
            if (!actual.Order().SequenceEqual(expected.Order())) throw new InvalidDataException($"Unsupported foreign keys in {table}.");
        }
    }

    private static void ValidateResult(SqliteConnection output, SqliteTransaction tx, KeepLaterRestorePreparation plan)
    {
        foreach (var table in plan.Records)
        {
            using var count = output.CreateCommand();
            count.Transaction = tx;
            count.CommandText = $"SELECT COUNT(*) FROM {table.Table};";
            if ((long)count.ExecuteScalar()! != (long)table.BackupBefore + table.CurrentAtOrAfter)
                throw new InvalidDataException("Staging record counts differ from preparation.");
        }
        CheckForeignKeys(output, tx);
    }

    private static void ValidateIntegrity(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA integrity_check;";
        using (var reader = command.ExecuteReader())
            if (!reader.Read() || reader.GetString(0) != "ok" || reader.Read()) throw new InvalidDataException("Database integrity check failed.");
        CheckForeignKeys(connection);
    }

    private static void CheckForeignKeys(SqliteConnection connection, SqliteTransaction? tx = null)
    {
        using var command = connection.CreateCommand();
        command.Transaction = tx;
        command.CommandText = "PRAGMA foreign_key_check;";
        using var reader = command.ExecuteReader();
        if (reader.Read()) throw new InvalidDataException("Database foreign key validation failed.");
    }

    private static SqliteConnection Open(string path, bool readOnly)
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, Pooling = false,
            Mode = readOnly ? SqliteOpenMode.ReadOnly : SqliteOpenMode.ReadWrite, ForeignKeys = true }.ToString());
        try
        {
            connection.Open();
            Execute(connection, readOnly ? "PRAGMA trusted_schema=OFF; PRAGMA query_only=ON;" : "PRAGMA trusted_schema=OFF;");
            return connection;
        }
        catch { connection.Dispose(); throw; }
    }
    private static void Execute(SqliteConnection connection, string sql, SqliteTransaction? tx = null)
    {
        using var command = connection.CreateCommand();
        command.Transaction = tx;
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
    private static string CheckSnapshot(string path)
    {
        if (!Path.IsPathFullyQualified(path)) throw new ArgumentException("Snapshot path must be absolute.");
        var fullPath = Path.GetFullPath(path);
        CheckReparsePoints(fullPath);
        if (!File.Exists(fullPath)) throw new FileNotFoundException("Snapshot not found.", fullPath);
        CheckSidecars(fullPath);
        return fullPath;
    }
    private static void CheckSidecars(string path)
    {
        if (new[] { "-wal", "-shm", "-journal" }.Any(s => Path.Exists(path + s)))
            throw new InvalidOperationException("Snapshot and output paths must not have WAL, SHM or journal sidecars.");
    }
    private static void CheckReparsePoints(string path)
    {
        for (var current = path; !string.IsNullOrEmpty(current); current = Path.GetDirectoryName(current))
            if (Path.Exists(current) && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Symbolic links and junctions are not supported for staging.");
    }
    internal static string Hash(string path)
    {
        using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Convert.ToHexString(SHA256.HashData(file));
    }
}
