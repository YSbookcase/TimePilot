using System.Drawing;
using System.Globalization;
using Microsoft.Data.Sqlite;

namespace TimePilot.WinForms.KYS24
{
    internal sealed class TimePilotStorage : IDisposable
    {
        private readonly string databasePath;
        private readonly string connectionString;
        private long? runtimeSessionId;
        private bool disposed;
        private static readonly TimeSpan CurrentTimelineSessionTolerance = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan SystemBootTimeTolerance = TimeSpan.FromSeconds(60);

        public TimePilotStorage(string databasePath)
        {
            this.databasePath = databasePath;
            connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = databasePath
            }.ToString();
        }

        public static TimePilotStorage CreateDefault()
        {
            return new TimePilotStorage(AppDataPaths.DatabasePath);
        }

        public void Initialize(DateTimeOffset now, DateTimeOffset systemBootedAt)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);

            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                CREATE TABLE IF NOT EXISTS app_categories (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL UNIQUE COLLATE NOCASE,
                    color TEXT NULL,
                    sort_order INTEGER NOT NULL DEFAULT 0,
                    is_builtin INTEGER NOT NULL DEFAULT 1,
                    created_at TEXT NOT NULL,
                    updated_at TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS apps (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    process_name TEXT NOT NULL UNIQUE COLLATE NOCASE,
                    display_name TEXT NOT NULL,
                    executable_path TEXT NULL,
                    first_seen_at TEXT NOT NULL,
                    last_seen_at TEXT NOT NULL,
                    primary_category_id INTEGER NULL,
                    user_alias TEXT NULL,
                    is_excluded INTEGER NOT NULL DEFAULT 0,
                    FOREIGN KEY (primary_category_id) REFERENCES app_categories(id)
                );

                CREATE TABLE IF NOT EXISTS app_runtime_sessions (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    started_at TEXT NOT NULL,
                    ended_at TEXT NULL,
                    duration_ms INTEGER NULL,
                    last_heartbeat_at TEXT NULL,
                    shutdown_reason TEXT NULL,
                    system_booted_at TEXT NULL,
                    app_version TEXT NULL
                );

                CREATE TABLE IF NOT EXISTS foreground_sessions (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    app_id INTEGER NOT NULL,
                    started_at TEXT NOT NULL,
                    ended_at TEXT NULL,
                    duration_ms INTEGER NULL,
                    last_observed_at TEXT NULL,
                    FOREIGN KEY (app_id) REFERENCES apps(id)
                );

                CREATE TABLE IF NOT EXISTS idle_sessions (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    started_at TEXT NOT NULL,
                    ended_at TEXT NULL,
                    duration_ms INTEGER NULL,
                    threshold_ms INTEGER NOT NULL,
                    foreground_app_id INTEGER NULL,
                    FOREIGN KEY (foreground_app_id) REFERENCES apps(id)
                );

                CREATE TABLE IF NOT EXISTS process_runtime_sessions (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    app_id INTEGER NOT NULL,
                    process_id INTEGER NOT NULL,
                    started_at TEXT NOT NULL,
                    ended_at TEXT NULL,
                    duration_ms INTEGER NULL,
                    first_observed_at TEXT NOT NULL,
                    last_observed_at TEXT NOT NULL,
                    tracking_scope INTEGER NULL,
                    has_main_window INTEGER NULL,
                    is_current_session_process INTEGER NULL,
                    FOREIGN KEY (app_id) REFERENCES apps(id)
                );

                CREATE TABLE IF NOT EXISTS system_events (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    event_type TEXT NOT NULL,
                    occurred_at TEXT NOT NULL,
                    app_runtime_session_id INTEGER NULL,
                    system_booted_at TEXT NULL,
                    details TEXT NULL,
                    FOREIGN KEY (app_runtime_session_id) REFERENCES app_runtime_sessions(id)
                );

                CREATE INDEX IF NOT EXISTS idx_foreground_sessions_started_at
                    ON foreground_sessions(started_at);

                CREATE INDEX IF NOT EXISTS idx_idle_sessions_started_at
                    ON idle_sessions(started_at);

                CREATE INDEX IF NOT EXISTS idx_app_runtime_sessions_started_at
                    ON app_runtime_sessions(started_at);

                CREATE INDEX IF NOT EXISTS idx_process_runtime_sessions_started_at
                    ON process_runtime_sessions(started_at);

                CREATE INDEX IF NOT EXISTS idx_process_runtime_sessions_process_id
                    ON process_runtime_sessions(process_id);

                CREATE INDEX IF NOT EXISTS idx_system_events_occurred_at
                    ON system_events(occurred_at);

                """;
            command.ExecuteNonQuery();

            EnsureAppsColumns(connection);
            EnsureRuntimeSessionColumns(connection);
            EnsureForegroundSessionColumns(connection);
            EnsureProcessRuntimeSessionColumns(connection);
            RenameBuiltinAppCategoriesToCanonical(connection, now);
            SeedDefaultAppCategories(connection, now);
            MarkUnexpectedRuntimeSessions(now, systemBootedAt);
            MarkUnexpectedProcessRuntimeSessions(now);
        }

        public void BeginRuntimeSession(DateTimeOffset startedAt, DateTimeOffset systemBootedAt, string? appVersion)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO app_runtime_sessions (
                    started_at,
                    last_heartbeat_at,
                    shutdown_reason,
                    system_booted_at,
                    app_version
                )
                VALUES ($startedAt, $lastHeartbeatAt, $shutdownReason, $systemBootedAt, $appVersion);
                SELECT last_insert_rowid();
                """;
            command.Parameters.AddWithValue("$startedAt", FormatTimestamp(startedAt));
            command.Parameters.AddWithValue("$lastHeartbeatAt", FormatTimestamp(startedAt));
            command.Parameters.AddWithValue("$shutdownReason", "running");
            command.Parameters.AddWithValue("$systemBootedAt", FormatTimestamp(systemBootedAt));
            command.Parameters.AddWithValue("$appVersion", (object?)appVersion ?? DBNull.Value);
            runtimeSessionId = (long)command.ExecuteScalar()!;
        }

        public void UpdateRuntimeHeartbeat(DateTimeOffset observedAt)
        {
            if (runtimeSessionId is not { } sessionId)
                return;

            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                UPDATE app_runtime_sessions
                SET last_heartbeat_at = $lastHeartbeatAt
                WHERE id = $id AND ended_at IS NULL;
                """;
            command.Parameters.AddWithValue("$lastHeartbeatAt", FormatTimestamp(observedAt));
            command.Parameters.AddWithValue("$id", sessionId);
            command.ExecuteNonQuery();
        }

        public void EndRuntimeSession(DateTimeOffset endedAt, string shutdownReason)
        {
            if (runtimeSessionId is not { } sessionId)
                return;

            using var connection = OpenConnection();
            EndRuntimeSession(connection, sessionId, endedAt, shutdownReason);
            runtimeSessionId = null;
        }

        public long StartForegroundSession(AppMetadata app, DateTimeOffset startedAt)
        {
            using var connection = OpenConnection();
            var appId = GetOrCreateAppId(connection, app, startedAt);

            using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO foreground_sessions (
                    app_id,
                    started_at,
                    last_observed_at
                )
                VALUES ($appId, $startedAt, $lastObservedAt);
                SELECT last_insert_rowid();
                """;
            command.Parameters.AddWithValue("$appId", appId);
            command.Parameters.AddWithValue("$startedAt", FormatTimestamp(startedAt));
            command.Parameters.AddWithValue("$lastObservedAt", FormatTimestamp(startedAt));
            return (long)command.ExecuteScalar()!;
        }

        public void UpdateAppMetadata(AppMetadata app, DateTimeOffset observedAt)
        {
            using var connection = OpenConnection();
            _ = GetOrCreateAppId(connection, app, observedAt);
        }

        public void UpdateForegroundSessionObservation(long sessionId, AppMetadata app, DateTimeOffset observedAt)
        {
            using var connection = OpenConnection();
            _ = GetOrCreateAppId(connection, app, observedAt);

            using var command = connection.CreateCommand();
            command.CommandText = """
                UPDATE foreground_sessions
                SET last_observed_at = $lastObservedAt
                WHERE id = $id AND ended_at IS NULL;
                """;
            command.Parameters.AddWithValue("$lastObservedAt", FormatTimestamp(observedAt));
            command.Parameters.AddWithValue("$id", sessionId);
            command.ExecuteNonQuery();
        }

        public void EndForegroundSession(long sessionId, DateTimeOffset endedAt)
        {
            using var connection = OpenConnection();
            using var selectCommand = connection.CreateCommand();
            selectCommand.CommandText = """
                SELECT started_at, last_observed_at
                FROM foreground_sessions
                WHERE id = $id AND ended_at IS NULL;
                """;
            selectCommand.Parameters.AddWithValue("$id", sessionId);
            DateTimeOffset startedAt;
            DateTimeOffset lastObservedAt;
            using (var reader = selectCommand.ExecuteReader())
            {
                if (!reader.Read())
                    return;

                startedAt = ParseTimestamp(reader.GetString(0));
                lastObservedAt = reader.IsDBNull(1)
                    ? startedAt
                    : ParseTimestamp(reader.GetString(1));
            }

            var effectiveEnd = lastObservedAt <= endedAt ? lastObservedAt : endedAt;
            var durationMs = Math.Max(0, (long)(effectiveEnd - startedAt).TotalMilliseconds);

            using var updateCommand = connection.CreateCommand();
            updateCommand.CommandText = """
                UPDATE foreground_sessions
                SET ended_at = $endedAt,
                    duration_ms = $durationMs
                WHERE id = $id AND ended_at IS NULL;
                """;
            updateCommand.Parameters.AddWithValue("$endedAt", FormatTimestamp(effectiveEnd));
            updateCommand.Parameters.AddWithValue("$durationMs", durationMs);
            updateCommand.Parameters.AddWithValue("$id", sessionId);
            updateCommand.ExecuteNonQuery();
        }

        public long StartIdleSession(DateTimeOffset startedAt, int thresholdMs, AppMetadata? foregroundApp)
        {
            using var connection = OpenConnection();
            long? foregroundAppId = foregroundApp is null || string.IsNullOrWhiteSpace(foregroundApp.ProcessName)
                ? null
                : GetOrCreateAppId(connection, foregroundApp, startedAt);

            using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO idle_sessions (
                    started_at,
                    threshold_ms,
                    foreground_app_id
                )
                VALUES ($startedAt, $thresholdMs, $foregroundAppId);
                SELECT last_insert_rowid();
                """;
            command.Parameters.AddWithValue("$startedAt", FormatTimestamp(startedAt));
            command.Parameters.AddWithValue("$thresholdMs", thresholdMs);
            command.Parameters.AddWithValue("$foregroundAppId", (object?)foregroundAppId ?? DBNull.Value);
            return (long)command.ExecuteScalar()!;
        }

        public void EndIdleSession(long sessionId, DateTimeOffset endedAt)
        {
            using var connection = OpenConnection();
            using var selectCommand = connection.CreateCommand();
            selectCommand.CommandText = """
                SELECT started_at
                FROM idle_sessions
                WHERE id = $id AND ended_at IS NULL;
                """;
            selectCommand.Parameters.AddWithValue("$id", sessionId);
            var startedAtValue = selectCommand.ExecuteScalar();
            if (startedAtValue is not string startedAtText)
                return;

            var startedAt = ParseTimestamp(startedAtText);
            var durationMs = Math.Max(0, (long)(endedAt - startedAt).TotalMilliseconds);

            using var updateCommand = connection.CreateCommand();
            updateCommand.CommandText = """
                UPDATE idle_sessions
                SET ended_at = $endedAt,
                    duration_ms = $durationMs
                WHERE id = $id AND ended_at IS NULL;
                """;
            updateCommand.Parameters.AddWithValue("$endedAt", FormatTimestamp(endedAt));
            updateCommand.Parameters.AddWithValue("$durationMs", durationMs);
            updateCommand.Parameters.AddWithValue("$id", sessionId);
            updateCommand.ExecuteNonQuery();
        }

        public long StartProcessRuntimeSession(
            AppMetadata app,
            int processId,
            ProcessRuntimeTrackingScope trackingScope,
            bool hasMainWindow,
            bool isCurrentSessionProcess,
            DateTimeOffset observedAt)
        {
            using var connection = OpenConnection();
            var appId = GetOrCreateAppId(connection, app, observedAt);

            using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO process_runtime_sessions (
                    app_id,
                    process_id,
                    started_at,
                    first_observed_at,
                    last_observed_at,
                    tracking_scope,
                    has_main_window,
                    is_current_session_process
                )
                VALUES ($appId, $processId, $startedAt, $firstObservedAt, $lastObservedAt, $trackingScope, $hasMainWindow, $isCurrentSessionProcess);
                SELECT last_insert_rowid();
                """;
            command.Parameters.AddWithValue("$appId", appId);
            command.Parameters.AddWithValue("$processId", processId);
            command.Parameters.AddWithValue("$startedAt", FormatTimestamp(observedAt));
            command.Parameters.AddWithValue("$firstObservedAt", FormatTimestamp(observedAt));
            command.Parameters.AddWithValue("$lastObservedAt", FormatTimestamp(observedAt));
            command.Parameters.AddWithValue("$trackingScope", (int)trackingScope);
            command.Parameters.AddWithValue("$hasMainWindow", hasMainWindow ? 1 : 0);
            command.Parameters.AddWithValue("$isCurrentSessionProcess", isCurrentSessionProcess ? 1 : 0);
            return (long)command.ExecuteScalar()!;
        }

        public void UpdateProcessRuntimeSession(
            long sessionId,
            AppMetadata app,
            ProcessRuntimeTrackingScope trackingScope,
            bool hasMainWindow,
            bool isCurrentSessionProcess,
            DateTimeOffset observedAt)
        {
            using var connection = OpenConnection();
            _ = GetOrCreateAppId(connection, app, observedAt);

            using var command = connection.CreateCommand();
            command.CommandText = """
                UPDATE process_runtime_sessions
                SET last_observed_at = $lastObservedAt,
                    tracking_scope = $trackingScope,
                    has_main_window = CASE WHEN $hasMainWindow = 1 THEN 1 ELSE has_main_window END,
                    is_current_session_process = CASE WHEN $isCurrentSessionProcess = 1 THEN 1 ELSE is_current_session_process END
                WHERE id = $id AND ended_at IS NULL;
                """;
            command.Parameters.AddWithValue("$lastObservedAt", FormatTimestamp(observedAt));
            command.Parameters.AddWithValue("$trackingScope", (int)trackingScope);
            command.Parameters.AddWithValue("$hasMainWindow", hasMainWindow ? 1 : 0);
            command.Parameters.AddWithValue("$isCurrentSessionProcess", isCurrentSessionProcess ? 1 : 0);
            command.Parameters.AddWithValue("$id", sessionId);
            command.ExecuteNonQuery();
        }

        public void EndProcessRuntimeSession(long sessionId, DateTimeOffset endedAt)
        {
            using var connection = OpenConnection();
            EndProcessRuntimeSession(connection, sessionId, endedAt);
        }

        public void RecordSystemEvent(
            string eventType,
            DateTimeOffset occurredAt,
            DateTimeOffset systemBootedAt,
            string? details)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO system_events (
                    event_type,
                    occurred_at,
                    app_runtime_session_id,
                    system_booted_at,
                    details
                )
                VALUES ($eventType, $occurredAt, $appRuntimeSessionId, $systemBootedAt, $details);
                """;
            command.Parameters.AddWithValue("$eventType", eventType);
            command.Parameters.AddWithValue("$occurredAt", FormatTimestamp(occurredAt));
            command.Parameters.AddWithValue("$appRuntimeSessionId", (object?)runtimeSessionId ?? DBNull.Value);
            command.Parameters.AddWithValue("$systemBootedAt", FormatTimestamp(systemBootedAt));
            command.Parameters.AddWithValue("$details", (object?)details ?? DBNull.Value);
            command.ExecuteNonQuery();
        }

        public void ClearUsageData()
        {
            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();

            foreach (var tableName in new[]
            {
                "process_runtime_sessions",
                "system_events",
                "foreground_sessions",
                "idle_sessions",
                "app_runtime_sessions",
                "apps"
            })
            {
                using var deleteCommand = connection.CreateCommand();
                deleteCommand.Transaction = transaction;
                deleteCommand.CommandText = $"DELETE FROM {tableName};";
                deleteCommand.ExecuteNonQuery();
            }

            using var sequenceCommand = connection.CreateCommand();
            sequenceCommand.Transaction = transaction;
            sequenceCommand.CommandText = """
                DELETE FROM sqlite_sequence
                WHERE name IN (
                    'process_runtime_sessions',
                    'system_events',
                    'foreground_sessions',
                    'idle_sessions',
                    'app_runtime_sessions',
                    'apps'
                );
                """;
            sequenceCommand.ExecuteNonQuery();

            transaction.Commit();
        }

        public bool HasRecentRepeatedShortUnexpectedRuntimeSessions(int requiredCount, TimeSpan maxDuration)
        {
            if (requiredCount <= 0)
                return false;

            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT started_at, ended_at, duration_ms, shutdown_reason
                FROM app_runtime_sessions
                WHERE ended_at IS NOT NULL
                ORDER BY started_at DESC
                LIMIT $requiredCount;
                """;
            command.Parameters.AddWithValue("$requiredCount", requiredCount);

            var matchedCount = 0;
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var shutdownReason = reader.IsDBNull(3) ? null : reader.GetString(3);
                if (!string.Equals(shutdownReason, "unexpected", StringComparison.OrdinalIgnoreCase))
                    return false;

                var startedAt = ParseTimestamp(reader.GetString(0));
                var endedAt = reader.IsDBNull(1) ? startedAt : ParseTimestamp(reader.GetString(1));
                var durationMs = reader.IsDBNull(2)
                    ? Math.Max(0, (long)(endedAt - startedAt).TotalMilliseconds)
                    : reader.GetInt64(2);

                if (durationMs > maxDuration.TotalMilliseconds)
                    return false;

                matchedCount++;
            }

            return matchedCount >= requiredCount;
        }

        public IReadOnlyList<AppRuntimeSessionDiagnostic> GetRecentRuntimeSessionDiagnostics(int limit)
        {
            if (limit <= 0)
                return Array.Empty<AppRuntimeSessionDiagnostic>();

            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT started_at,
                       ended_at,
                       last_heartbeat_at,
                       duration_ms,
                       shutdown_reason,
                       system_booted_at,
                       app_version
                FROM app_runtime_sessions
                WHERE ended_at IS NOT NULL
                ORDER BY started_at DESC
                LIMIT $limit;
                """;
            command.Parameters.AddWithValue("$limit", limit);

            var sessions = new List<AppRuntimeSessionDiagnostic>();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                sessions.Add(new AppRuntimeSessionDiagnostic(
                    ParseTimestamp(reader.GetString(0)),
                    reader.IsDBNull(1) ? null : ParseTimestamp(reader.GetString(1)),
                    reader.IsDBNull(2) ? null : ParseTimestamp(reader.GetString(2)),
                    reader.IsDBNull(3) ? null : reader.GetInt64(3),
                    reader.IsDBNull(4) ? null : reader.GetString(4),
                    reader.IsDBNull(5) ? null : ParseTimestamp(reader.GetString(5)),
                    reader.IsDBNull(6) ? null : reader.GetString(6)));
            }

            return sessions;
        }

        public IReadOnlyList<SystemEventDiagnostic> GetRecentSystemEventDiagnostics(int limit)
        {
            if (limit <= 0)
                return Array.Empty<SystemEventDiagnostic>();

            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT occurred_at,
                       event_type,
                       details
                FROM system_events
                ORDER BY occurred_at DESC
                LIMIT $limit;
                """;
            command.Parameters.AddWithValue("$limit", limit);

            var events = new List<SystemEventDiagnostic>();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                events.Add(new SystemEventDiagnostic(
                    ParseTimestamp(reader.GetString(0)),
                    reader.GetString(1),
                    reader.IsDBNull(2) ? null : reader.GetString(2)));
            }

            return events;
        }

        public IReadOnlyList<AppCategoryOption> GetAppCategoryOptions()
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT id, name, color, sort_order, is_builtin
                FROM app_categories
                ORDER BY sort_order, name COLLATE NOCASE;
                """;

            var categories = new List<AppCategoryOption>();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                categories.Add(new AppCategoryOption(
                    reader.GetInt64(0),
                    reader.GetString(1),
                    reader.IsDBNull(2) ? null : reader.GetString(2),
                    reader.GetInt32(3),
                    reader.GetInt32(4) != 0));
            }

            return categories;
        }

        public IReadOnlyList<AppCategoryEditorRow> GetAppCategoryEditorRows()
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT
                    c.id,
                    c.name,
                    c.color,
                    c.sort_order,
                    c.is_builtin,
                    CAST(COUNT(a.id) AS INTEGER) AS app_count
                FROM app_categories c
                LEFT JOIN apps a ON a.primary_category_id = c.id
                GROUP BY c.id, c.name, c.color, c.sort_order, c.is_builtin
                ORDER BY c.sort_order, c.name COLLATE NOCASE;
                """;

            var rows = new List<AppCategoryEditorRow>();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                rows.Add(new AppCategoryEditorRow(
                    reader.GetInt64(0),
                    reader.GetString(1),
                    reader.IsDBNull(2) ? null : reader.GetString(2),
                    reader.GetInt32(3),
                    reader.GetInt32(4) != 0,
                    reader.GetInt32(5)));
            }

            return rows;
        }

        public long CreateCustomAppCategory(string name, string? color)
        {
            var normalizedName = NormalizeAppCategoryName(name);
            var normalizedColor = NormalizeAppCategoryColor(color);
            using var connection = OpenConnection();
            EnsureAppCategoryNameIsUnique(connection, normalizedName, null);
            var now = FormatTimestamp(DateTimeOffset.UtcNow);
            var sortOrder = GetNextCustomAppCategorySortOrder(connection);

            using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO app_categories (
                    name,
                    color,
                    sort_order,
                    is_builtin,
                    created_at,
                    updated_at
                )
                VALUES ($name, $color, $sortOrder, 0, $createdAt, $updatedAt)
                RETURNING id;
                """;
            command.Parameters.AddWithValue("$name", normalizedName);
            command.Parameters.AddWithValue("$color", (object?)normalizedColor ?? DBNull.Value);
            command.Parameters.AddWithValue("$sortOrder", sortOrder);
            command.Parameters.AddWithValue("$createdAt", now);
            command.Parameters.AddWithValue("$updatedAt", now);
            return (long)command.ExecuteScalar()!;
        }

        public void UpdateCustomAppCategory(long categoryId, string name, string? color)
        {
            var normalizedName = NormalizeAppCategoryName(name);
            var normalizedColor = NormalizeAppCategoryColor(color);
            using var connection = OpenConnection();
            EnsureAppCategoryNameIsUnique(connection, normalizedName, categoryId);
            using var command = connection.CreateCommand();
            command.CommandText = """
                UPDATE app_categories
                SET name = $name,
                    color = $color,
                    updated_at = $updatedAt
                WHERE id = $categoryId
                  AND is_builtin = 0;
                """;
            command.Parameters.AddWithValue("$name", normalizedName);
            command.Parameters.AddWithValue("$color", (object?)normalizedColor ?? DBNull.Value);
            command.Parameters.AddWithValue("$updatedAt", FormatTimestamp(DateTimeOffset.UtcNow));
            command.Parameters.AddWithValue("$categoryId", categoryId);
            command.ExecuteNonQuery();
        }

        public void UpdateAppCategoryColor(long categoryId, string? color)
        {
            var normalizedColor = NormalizeAppCategoryColor(color);
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                UPDATE app_categories
                SET color = $color,
                    updated_at = $updatedAt
                WHERE id = $categoryId;
                """;
            command.Parameters.AddWithValue("$color", (object?)normalizedColor ?? DBNull.Value);
            command.Parameters.AddWithValue("$updatedAt", FormatTimestamp(DateTimeOffset.UtcNow));
            command.Parameters.AddWithValue("$categoryId", categoryId);
            command.ExecuteNonQuery();
        }

        public void DeleteCustomAppCategory(long categoryId)
        {
            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();

            using (var clearCommand = connection.CreateCommand())
            {
                clearCommand.Transaction = transaction;
                clearCommand.CommandText = """
                    UPDATE apps
                    SET primary_category_id = NULL
                    WHERE primary_category_id = $categoryId
                      AND EXISTS (
                          SELECT 1
                          FROM app_categories c
                          WHERE c.id = $categoryId
                            AND c.is_builtin = 0
                      );
                    """;
                clearCommand.Parameters.AddWithValue("$categoryId", categoryId);
                clearCommand.ExecuteNonQuery();
            }

            using (var deleteCommand = connection.CreateCommand())
            {
                deleteCommand.Transaction = transaction;
                deleteCommand.CommandText = """
                    DELETE FROM app_categories
                    WHERE id = $categoryId
                      AND is_builtin = 0;
                    """;
                deleteCommand.Parameters.AddWithValue("$categoryId", categoryId);
                deleteCommand.ExecuteNonQuery();
            }

            transaction.Commit();
        }

        public void SetAppPrimaryCategory(long appId, long? categoryId)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                UPDATE apps
                SET primary_category_id = $categoryId
                WHERE id = $appId;
                """;
            command.Parameters.AddWithValue("$categoryId", (object?)categoryId ?? DBNull.Value);
            command.Parameters.AddWithValue("$appId", appId);
            command.ExecuteNonQuery();
        }

        public IReadOnlyList<AppCategoryManagementRow> GetAppCategoryManagementRows(DateTimeOffset now)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT
                    a.id,
                    a.display_name,
                    a.process_name,
                    a.executable_path,
                    a.primary_category_id,
                    c.name,
                    f.total_active_ms,
                    f.switch_count,
                    f.last_observed_at,
                    r.total_runtime_ms,
                    r.segment_count,
                    r.last_observed_at
                FROM apps a
                LEFT JOIN app_categories c ON c.id = a.primary_category_id
                LEFT JOIN (
                    SELECT
                        app_id,
                        SUM(MAX(0, CAST((julianday(COALESCE(ended_at, last_observed_at, started_at)) - julianday(started_at)) * 86400000 AS INTEGER))) AS total_active_ms,
                        COUNT(*) AS switch_count,
                        MAX(COALESCE(ended_at, last_observed_at, started_at)) AS last_observed_at
                    FROM foreground_sessions
                    GROUP BY app_id
                ) f ON f.app_id = a.id
                LEFT JOIN (
                    SELECT
                        app_id,
                        SUM(MAX(0, CAST((julianday(COALESCE(ended_at, last_observed_at, started_at)) - julianday(started_at)) * 86400000 AS INTEGER))) AS total_runtime_ms,
                        COUNT(*) AS segment_count,
                        MAX(COALESCE(ended_at, last_observed_at, started_at)) AS last_observed_at
                    FROM process_runtime_sessions
                    GROUP BY app_id
                ) r ON r.app_id = a.id
                ORDER BY COALESCE(f.last_observed_at, r.last_observed_at, '') DESC,
                         a.display_name COLLATE NOCASE;
                """;
            command.Parameters.AddWithValue("$now", FormatTimestamp(now));

            var rows = new List<AppCategoryManagementRow>();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var foregroundLastObservedAt = reader.IsDBNull(8) ? (DateTimeOffset?)null : ParseTimestamp(reader.GetString(8));
                var runtimeLastObservedAt = reader.IsDBNull(11) ? (DateTimeOffset?)null : ParseTimestamp(reader.GetString(11));
                var lastObservedAt = MaxNullable(foregroundLastObservedAt, runtimeLastObservedAt);

                rows.Add(new AppCategoryManagementRow(
                    reader.GetInt64(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.IsDBNull(3) ? null : reader.GetString(3),
                    reader.IsDBNull(4) ? (long?)null : reader.GetInt64(4),
                    reader.IsDBNull(5) ? null : reader.GetString(5),
                    lastObservedAt,
                    reader.IsDBNull(6) ? 0 : reader.GetInt64(6),
                    reader.IsDBNull(9) ? 0 : reader.GetInt64(9),
                    reader.IsDBNull(7) ? 0 : Convert.ToInt32(reader.GetInt64(7)),
                    reader.IsDBNull(10) ? 0 : Convert.ToInt32(reader.GetInt64(10))));
            }

            return rows;
        }

        public IReadOnlyList<RawDataExportTable> GetRawDataExportTables()
        {
            return
            [
                GetRawDataExportTable(
                    "app_categories",
                    [
                        "id",
                        "name",
                        "color",
                        "sort_order",
                        "is_builtin",
                        "created_at",
                        "updated_at"
                    ]),
                GetRawDataExportTable(
                    "apps",
                    [
                        "id",
                        "process_name",
                        "display_name",
                        "executable_path",
                        "first_seen_at",
                        "last_seen_at",
                        "primary_category_id",
                        "user_alias",
                        "is_excluded"
                    ]),
                GetRawDataExportTable(
                    "app_runtime_sessions",
                    [
                        "id",
                        "started_at",
                        "ended_at",
                        "duration_ms",
                        "last_heartbeat_at",
                        "shutdown_reason",
                        "system_booted_at",
                        "app_version"
                    ]),
                GetRawDataExportTable(
                    "foreground_sessions",
                    [
                        "id",
                        "app_id",
                        "started_at",
                        "ended_at",
                        "duration_ms",
                        "last_observed_at"
                    ]),
                GetRawDataExportTable(
                    "idle_sessions",
                    [
                        "id",
                        "started_at",
                        "ended_at",
                        "duration_ms",
                        "threshold_ms",
                        "foreground_app_id"
                    ]),
                GetRawDataExportTable(
                    "process_runtime_sessions",
                    [
                        "id",
                        "app_id",
                        "process_id",
                        "started_at",
                        "ended_at",
                        "duration_ms",
                        "first_observed_at",
                        "last_observed_at",
                        "tracking_scope",
                        "has_main_window",
                        "is_current_session_process"
                    ]),
                GetRawDataExportTable(
                    "system_events",
                    [
                        "id",
                        "event_type",
                        "occurred_at",
                        "app_runtime_session_id",
                        "system_booted_at",
                        "details"
                    ])
            ];
        }

        private RawDataExportTable GetRawDataExportTable(string tableName, IReadOnlyList<string> columns)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = $"""
                SELECT {string.Join(", ", columns)}
                FROM {tableName}
                ORDER BY id;
                """;

            var rows = new List<IReadOnlyList<string>>();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var row = new string[columns.Count];
                for (var i = 0; i < columns.Count; i++)
                {
                    row[i] = reader.IsDBNull(i)
                        ? ""
                        : Convert.ToString(reader.GetValue(i), CultureInfo.InvariantCulture) ?? "";
                }

                rows.Add(row);
            }

            return new RawDataExportTable(tableName, $"{tableName}.csv", columns, rows);
        }

        public IReadOnlyList<ProcessRuntimeSessionStartResult> ApplyProcessRuntimeSessionChanges(
            IReadOnlyList<ProcessRuntimeSessionStart> starts,
            IReadOnlyList<ProcessRuntimeSessionUpdate> updates,
            IReadOnlyList<long> endedSessionIds,
            DateTimeOffset observedAt)
        {
            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();
            var startResults = new List<ProcessRuntimeSessionStartResult>();

            foreach (var update in updates)
            {
                _ = GetOrCreateAppId(connection, update.App, observedAt, transaction);
                UpdateProcessRuntimeSession(
                    connection,
                    transaction,
                    update.SessionId,
                    update.TrackingScope,
                    update.HasMainWindow,
                    update.IsCurrentSessionProcess,
                    observedAt);
            }

            foreach (var sessionId in endedSessionIds.Distinct())
            {
                EndProcessRuntimeSession(connection, sessionId, observedAt, transaction);
            }

            foreach (var start in starts)
            {
                var appId = GetOrCreateAppId(connection, start.App, observedAt, transaction);
                var sessionId = StartProcessRuntimeSession(
                    connection,
                    transaction,
                    appId,
                    start.ProcessId,
                    start.TrackingScope,
                    start.HasMainWindow,
                    start.IsCurrentSessionProcess,
                    observedAt);

                startResults.Add(new ProcessRuntimeSessionStartResult(
                    start.ProcessId,
                    sessionId,
                    start.App.ProcessName));
            }

            transaction.Commit();
            return startResults;
        }

        public IReadOnlyList<ForegroundUsageSummary> GetForegroundUsageForDay(DateTimeOffset now)
        {
            var localDayStart = now.ToLocalTime().Date;
            return GetForegroundUsageForDate(localDayStart);
        }

        public IReadOnlyList<ForegroundUsageSummary> GetForegroundUsageForDate(DateTime localDate)
        {
            var (dayStart, dayEnd) = GetLocalDayRange(localDate);
            return GetForegroundUsageForPeriod(dayStart, dayEnd);
        }

        public IReadOnlyList<ForegroundUsageSummary> GetForegroundUsageForPeriod(
            DateTimeOffset periodStart,
            DateTimeOffset periodEnd)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT
                    a.id,
                    a.display_name,
                    a.process_name,
                    a.executable_path,
                    a.primary_category_id,
                    c.name,
                    fs.started_at,
                    fs.ended_at,
                    fs.last_observed_at
                FROM foreground_sessions fs
                INNER JOIN apps a ON a.id = fs.app_id
                LEFT JOIN app_categories c ON c.id = a.primary_category_id
                WHERE fs.started_at < $periodEnd
                  AND COALESCE(fs.ended_at, fs.last_observed_at, fs.started_at) > $periodStart;
                """;
            command.Parameters.AddWithValue("$periodStart", FormatTimestamp(periodStart));
            command.Parameters.AddWithValue("$periodEnd", FormatTimestamp(periodEnd));

            var totals = new Dictionary<long, UsageAggregation>();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var appId = reader.GetInt64(0);
                var appName = reader.GetString(1);
                var processName = reader.GetString(2);
                var executablePath = reader.IsDBNull(3) ? null : reader.GetString(3);
                var primaryCategoryId = reader.IsDBNull(4) ? (long?)null : reader.GetInt64(4);
                var categoryName = reader.IsDBNull(5) ? null : reader.GetString(5);
                var startedAt = ParseTimestamp(reader.GetString(6));
                var endedAt = reader.IsDBNull(7)
                    ? reader.IsDBNull(8) ? startedAt : ParseTimestamp(reader.GetString(8))
                    : ParseTimestamp(reader.GetString(7));
                var effectiveStart = Max(startedAt, periodStart);
                var effectiveEnd = Min(endedAt, periodEnd);
                var durationMs = Math.Max(0, (long)(effectiveEnd - effectiveStart).TotalMilliseconds);
                if (durationMs <= 0)
                    continue;

                if (!totals.TryGetValue(appId, out var aggregation))
                {
                    aggregation = new UsageAggregation(
                        appId,
                        appName,
                        processName,
                        effectiveStart,
                        effectiveEnd,
                        executablePath,
                        primaryCategoryId,
                        categoryName);
                    totals[appId] = aggregation;
                }

                aggregation.ExecutablePath ??= executablePath;
                aggregation.PrimaryCategoryId ??= primaryCategoryId;
                aggregation.CategoryName ??= categoryName;
                aggregation.ActiveUsageMs += durationMs;
                aggregation.SwitchCount++;
                aggregation.FirstStartedAt = Min(aggregation.FirstStartedAt, effectiveStart);
                aggregation.LastObservedAt = Max(aggregation.LastObservedAt, effectiveEnd);
            }

            AddIdleRecordedTimeToUsageAggregations(connection, totals, periodStart, periodEnd, DateTimeOffset.Now);

            return totals
                .Select(x => new ForegroundUsageSummary(
                    x.Value.AppId,
                    x.Value.AppName,
                    x.Value.ProcessName,
                    x.Value.ExecutablePath,
                    x.Value.PrimaryCategoryId,
                    x.Value.CategoryName,
                    x.Value.ActiveUsageMs,
                    x.Value.IdleRecordedMs,
                    x.Value.SwitchCount,
                    x.Value.FirstStartedAt,
                    x.Value.LastObservedAt))
                .OrderByDescending(x => x.ActiveUsageMs)
                .ToList();
        }

        public IdleUsageSummary GetIdleUsageForPeriod(DateTimeOffset periodStart, DateTimeOffset periodEnd)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT
                    started_at,
                    ended_at
                FROM idle_sessions
                WHERE started_at < $periodEnd
                  AND COALESCE(ended_at, $periodEnd) > $periodStart;
                """;
            command.Parameters.AddWithValue("$periodStart", FormatTimestamp(periodStart));
            command.Parameters.AddWithValue("$periodEnd", FormatTimestamp(periodEnd));

            long idleMs = 0;
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var startedAt = ParseTimestamp(reader.GetString(0));
                var endedAt = reader.IsDBNull(1) ? periodEnd : ParseTimestamp(reader.GetString(1));
                var effectiveStart = Max(startedAt, periodStart);
                var effectiveEnd = Min(endedAt, periodEnd);
                if (effectiveEnd <= effectiveStart)
                    continue;

                idleMs += Math.Max(0, (long)(effectiveEnd - effectiveStart).TotalMilliseconds);
            }

            return new IdleUsageSummary(idleMs);
        }

        public IReadOnlyList<DailyUsageTrendRow> GetDailyUsageTrendForPeriod(
            DateTimeOffset periodStart,
            DateTimeOffset periodEnd)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT
                    a.display_name,
                    fs.started_at,
                    fs.ended_at,
                    fs.last_observed_at
                FROM foreground_sessions fs
                INNER JOIN apps a ON a.id = fs.app_id
                WHERE fs.started_at < $periodEnd
                  AND COALESCE(fs.ended_at, fs.last_observed_at, fs.started_at) > $periodStart;
                """;
            command.Parameters.AddWithValue("$periodStart", FormatTimestamp(periodStart));
            command.Parameters.AddWithValue("$periodEnd", FormatTimestamp(periodEnd));

            var totals = new Dictionary<DateTime, DailyUsageTrendAggregation>();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var appName = reader.GetString(0);
                var startedAt = ParseTimestamp(reader.GetString(1));
                var endedAt = reader.IsDBNull(2)
                    ? reader.IsDBNull(3) ? startedAt : ParseTimestamp(reader.GetString(3))
                    : ParseTimestamp(reader.GetString(2));
                var effectiveStart = Max(startedAt, periodStart);
                var effectiveEnd = Min(endedAt, periodEnd);
                if (effectiveEnd <= effectiveStart)
                    continue;

                AddDailyUsageTrend(totals, appName, effectiveStart, effectiveEnd);
            }

            return totals
                .OrderByDescending(x => x.Key)
                .Select(x =>
                {
                    var topApp = x.Value.AppTotals
                        .OrderByDescending(app => app.Value)
                        .ThenBy(app => app.Key, StringComparer.CurrentCultureIgnoreCase)
                        .FirstOrDefault();

                    return new DailyUsageTrendRow(
                        x.Key,
                        x.Value.ActiveUsageMs,
                        topApp.Key ?? "",
                        topApp.Value);
                })
                .ToList();
        }

        public IReadOnlyList<ActivityTimelineRow> GetActivityTimelineForDay(DateTimeOffset now)
        {
            var localDayStart = now.ToLocalTime().Date;
            return GetActivityTimelineForDate(localDayStart, now);
        }

        public IReadOnlyList<ActivityTimelineRow> GetActivityTimelineForDate(DateTime localDate, DateTimeOffset now)
        {
            var (dayStart, dayEnd) = GetLocalDayRange(localDate);
            var rows = new List<ActivityTimelineRow>();

            using var connection = OpenConnection();
            AddUntrackedTimelineRows(connection, rows, dayStart, dayEnd, now);
            AddForegroundTimelineRows(connection, rows, dayStart, dayEnd, now);
            AddIdleTimelineRows(connection, rows, dayStart, dayEnd, now);

            return rows
                .OrderBy(x => x.StartedAt)
                .ToList();
        }

        public IReadOnlyList<CategoryTimelineSegment> GetCategoryTimelineSegmentsForDate(
            DateTime localDate,
            DateTimeOffset now,
            TimeSpan bucketSize,
            bool wholeDay = false)
        {
            if (bucketSize <= TimeSpan.Zero && !wholeDay)
                bucketSize = TimeSpan.FromMinutes(30);

            var (dayStart, dayEnd) = GetLocalDayRange(localDate);
            if (dayEnd > now)
                dayEnd = now;

            if (dayEnd <= dayStart)
                return Array.Empty<CategoryTimelineSegment>();

            if (wholeDay)
                bucketSize = TimeSpan.FromDays(1);

            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT
                    COALESCE(c.name, $uncategorized),
                    c.color,
                    fs.started_at,
                    fs.ended_at,
                    fs.last_observed_at
                FROM foreground_sessions fs
                INNER JOIN apps a ON a.id = fs.app_id
                LEFT JOIN app_categories c ON c.id = a.primary_category_id
                WHERE fs.started_at < $dayEnd
                  AND COALESCE(fs.ended_at, fs.last_observed_at, fs.started_at) > $dayStart;
                """;
            command.Parameters.AddWithValue("$uncategorized", UiText.Main.Uncategorized);
            command.Parameters.AddWithValue("$dayStart", FormatTimestamp(dayStart));
            command.Parameters.AddWithValue("$dayEnd", FormatTimestamp(dayEnd));

            var bucketCount = Math.Max(1, (int)Math.Ceiling(TimeSpan.FromDays(1).TotalMilliseconds / bucketSize.TotalMilliseconds));
            var buckets = new Dictionary<int, Dictionary<string, CategoryBucketTotal>>();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var categoryName = reader.GetString(0);
                var color = reader.IsDBNull(1) ? null : reader.GetString(1);
                var startedAt = ParseTimestamp(reader.GetString(2));
                var endedAt = reader.IsDBNull(3)
                    ? reader.IsDBNull(4) ? startedAt : ParseTimestamp(reader.GetString(4))
                    : ParseTimestamp(reader.GetString(3));
                var effectiveStart = Max(startedAt, dayStart);
                var effectiveEnd = Min(endedAt, dayEnd);
                if (effectiveEnd <= effectiveStart)
                    continue;

                AddCategoryBucketDurations(
                    buckets,
                    dayStart,
                    bucketSize,
                    bucketCount,
                    categoryName,
                    color,
                    effectiveStart,
                    effectiveEnd);
            }

            if (wholeDay)
            {
                return buckets.TryGetValue(0, out var totals)
                    ? [CreateWholeDayCategoryTimelineSegment(dayStart, dayEnd, totals)]
                    : Array.Empty<CategoryTimelineSegment>();
            }

            return buckets
                .OrderBy(x => x.Key)
                .Select(x => CreateCategoryTimelineSegment(dayStart, bucketSize, x.Key, x.Value))
                .ToList();
        }

        public bool HasActivityDataForDate(DateTime localDate, DateTimeOffset now)
        {
            var (dayStart, dayEnd) = GetLocalDayRange(localDate);

            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT EXISTS (
                    SELECT 1
                    FROM foreground_sessions
                    WHERE started_at < $dayEnd
                      AND COALESCE(ended_at, last_observed_at, started_at) > $dayStart
                    LIMIT 1
                )
                OR EXISTS (
                    SELECT 1
                    FROM idle_sessions
                    WHERE started_at < $dayEnd
                      AND COALESCE(ended_at, started_at) > $dayStart
                    LIMIT 1
                )
                OR EXISTS (
                    SELECT 1
                    FROM app_runtime_sessions
                    WHERE started_at < $dayEnd
                      AND COALESCE(ended_at, last_heartbeat_at, $now) > $dayStart
                    LIMIT 1
                )
                OR EXISTS (
                    SELECT 1
                    FROM process_runtime_sessions
                    WHERE started_at < $dayEnd
                      AND COALESCE(ended_at, last_observed_at, started_at) > $dayStart
                    LIMIT 1
                );
                """;
            command.Parameters.AddWithValue("$dayStart", FormatTimestamp(dayStart));
            command.Parameters.AddWithValue("$dayEnd", FormatTimestamp(dayEnd));
            command.Parameters.AddWithValue("$now", FormatTimestamp(now));

            return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture) != 0;
        }

        public IReadOnlyList<DateTime> GetActivityDates(DateTime rangeStart, DateTime rangeEnd, DateTimeOffset now)
        {
            var localStart = rangeStart.Date;
            var localEnd = rangeEnd.Date;
            var today = now.ToLocalTime().Date;
            if (localEnd > today.AddDays(1))
                localEnd = today.AddDays(1);

            if (localEnd <= localStart)
                return Array.Empty<DateTime>();

            var (periodStart, _) = GetLocalDayRange(localStart);
            var (_, periodEnd) = GetLocalDayRange(localEnd.AddDays(-1));
            var dates = new HashSet<DateTime>();

            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT started_at, COALESCE(ended_at, last_observed_at, started_at)
                FROM foreground_sessions
                WHERE started_at < $periodEnd
                  AND COALESCE(ended_at, last_observed_at, started_at) > $periodStart
                UNION ALL
                SELECT started_at, COALESCE(ended_at, started_at)
                FROM idle_sessions
                WHERE started_at < $periodEnd
                  AND COALESCE(ended_at, started_at) > $periodStart
                UNION ALL
                SELECT started_at, COALESCE(ended_at, last_heartbeat_at, $now)
                FROM app_runtime_sessions
                WHERE started_at < $periodEnd
                  AND COALESCE(ended_at, last_heartbeat_at, $now) > $periodStart
                UNION ALL
                SELECT started_at, COALESCE(ended_at, last_observed_at, started_at)
                FROM process_runtime_sessions
                WHERE started_at < $periodEnd
                  AND COALESCE(ended_at, last_observed_at, started_at) > $periodStart;
                """;
            command.Parameters.AddWithValue("$periodStart", FormatTimestamp(periodStart));
            command.Parameters.AddWithValue("$periodEnd", FormatTimestamp(periodEnd));
            command.Parameters.AddWithValue("$now", FormatTimestamp(now));

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var startedAt = ParseTimestamp(reader.GetString(0));
                var endedAt = ParseTimestamp(reader.GetString(1));
                AddActivityDates(dates, Max(startedAt, periodStart), Min(endedAt, periodEnd));
            }

            return dates
                .Where(date => date >= localStart && date < localEnd)
                .OrderBy(date => date)
                .ToList();
        }

        public RuntimeCoverageSummary GetRuntimeCoverageForDay(DateTimeOffset now)
        {
            var localDayStart = now.ToLocalTime().Date;
            var dayStart = new DateTimeOffset(localDayStart, TimeZoneInfo.Local.GetUtcOffset(localDayStart));
            var dayEnd = Min(dayStart.AddDays(1), now);

            if (dayEnd <= dayStart)
                return new RuntimeCoverageSummary(0, 0, 0, 0, null);

            using var connection = OpenConnection();
            var runtimeIntervals = GetRuntimeIntervalsForDay(connection, dayStart, dayEnd, now);
            var mergedIntervals = MergeIntervals(runtimeIntervals);
            var totalWindowMs = Math.Max(0, (long)(dayEnd - dayStart).TotalMilliseconds);
            var trackedRuntimeMs = mergedIntervals
                .Sum(interval => Math.Max(0, (long)(interval.End - interval.Start).TotalMilliseconds));
            var missingRuntimeMs = Math.Max(0, totalWindowMs - trackedRuntimeMs);
            var longestMissingRuntimeMs = GetLongestGapMs(mergedIntervals, dayStart, dayEnd);
            var bootBeforeTimePilotMs = GetBootBeforeTimePilotMs(connection, dayStart, dayEnd);

            return new RuntimeCoverageSummary(
                totalWindowMs,
                trackedRuntimeMs,
                missingRuntimeMs,
                longestMissingRuntimeMs,
                bootBeforeTimePilotMs);
        }

        public IReadOnlyList<TimelineRange> GetWindowsRuntimeRangesForDate(DateTime localDate, DateTimeOffset now)
        {
            var dayStart = new DateTimeOffset(localDate.Date, TimeZoneInfo.Local.GetUtcOffset(localDate.Date));
            var dayEndDate = localDate.Date.AddDays(1);
            var dayEnd = new DateTimeOffset(dayEndDate, TimeZoneInfo.Local.GetUtcOffset(dayEndDate));

            if (dayEnd > now)
                dayEnd = now;

            if (dayEnd <= dayStart)
                return Array.Empty<TimelineRange>();

            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT system_booted_at,
                       started_at,
                       COALESCE(ended_at, last_heartbeat_at, $now)
                FROM app_runtime_sessions
                WHERE COALESCE(system_booted_at, started_at) < $dayEnd
                  AND COALESCE(ended_at, last_heartbeat_at, $now) > $dayStart
                ORDER BY COALESCE(system_booted_at, started_at);
                """;
            command.Parameters.AddWithValue("$dayStart", FormatTimestamp(dayStart));
            command.Parameters.AddWithValue("$dayEnd", FormatTimestamp(dayEnd));
            command.Parameters.AddWithValue("$now", FormatTimestamp(now));

            var intervals = new List<(DateTimeOffset Start, DateTimeOffset End)>();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var startedAt = reader.IsDBNull(0)
                    ? ParseTimestamp(reader.GetString(1))
                    : ParseTimestamp(reader.GetString(0));
                var endedAt = ParseTimestamp(reader.GetString(2));
                if (endedAt <= startedAt)
                    continue;

                intervals.Add((Max(startedAt, dayStart), Min(endedAt, dayEnd)));
            }

            return MergeIntervals(intervals)
                .Select(interval => new TimelineRange(interval.Start, interval.End))
                .ToList();
        }

        public IReadOnlyList<SystemTimelineEvent> GetSystemTimelineEventsForDate(DateTime localDate, DateTimeOffset now)
        {
            var dayStart = new DateTimeOffset(localDate.Date, TimeZoneInfo.Local.GetUtcOffset(localDate.Date));
            var dayEndDate = localDate.Date.AddDays(1);
            var dayEnd = new DateTimeOffset(dayEndDate, TimeZoneInfo.Local.GetUtcOffset(dayEndDate));

            if (dayEnd > now)
                dayEnd = now;

            if (dayEnd <= dayStart)
                return Array.Empty<SystemTimelineEvent>();

            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT occurred_at,
                       event_type,
                       details
                FROM system_events
                WHERE occurred_at >= $dayStart
                  AND occurred_at < $dayEnd
                ORDER BY occurred_at;
                """;
            command.Parameters.AddWithValue("$dayStart", FormatTimestamp(dayStart));
            command.Parameters.AddWithValue("$dayEnd", FormatTimestamp(dayEnd));

            var events = new List<SystemTimelineEvent>();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                events.Add(new SystemTimelineEvent(
                    ParseTimestamp(reader.GetString(0)),
                    reader.GetString(1),
                    reader.IsDBNull(2) ? null : reader.GetString(2)));
            }

            return events;
        }

        public IReadOnlyList<SystemTimelineRange> GetSystemTimelineRangesForDate(DateTime localDate, DateTimeOffset now)
        {
            var dayStart = new DateTimeOffset(localDate.Date, TimeZoneInfo.Local.GetUtcOffset(localDate.Date));
            var dayEndDate = localDate.Date.AddDays(1);
            var dayEnd = new DateTimeOffset(dayEndDate, TimeZoneInfo.Local.GetUtcOffset(dayEndDate));

            if (dayEnd > now)
                dayEnd = now;

            if (dayEnd <= dayStart)
                return Array.Empty<SystemTimelineRange>();

            var lookbackStart = dayStart.AddDays(-1);
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT occurred_at,
                       event_type
                FROM system_events
                WHERE occurred_at >= $lookbackStart
                  AND occurred_at < $dayEnd
                ORDER BY occurred_at;
                """;
            command.Parameters.AddWithValue("$lookbackStart", FormatTimestamp(lookbackStart));
            command.Parameters.AddWithValue("$dayEnd", FormatTimestamp(dayEnd));

            var ranges = new List<SystemTimelineRange>();
            DateTimeOffset? sleepStartedAt = null;
            DateTimeOffset? lockStartedAt = null;
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var occurredAt = ParseTimestamp(reader.GetString(0));
                var eventType = reader.GetString(1).ToLowerInvariant();
                switch (eventType)
                {
                    case "suspend":
                        sleepStartedAt = occurredAt;
                        break;
                    case "resume":
                        AddSystemTimelineRange(ranges, sleepStartedAt, occurredAt, dayStart, dayEnd, SystemTimelineRangeType.SleepEstimate);
                        sleepStartedAt = null;
                        break;
                    case "lock":
                        lockStartedAt = occurredAt;
                        break;
                    case "unlock":
                    case "logon":
                        AddSystemTimelineRange(ranges, lockStartedAt, occurredAt, dayStart, dayEnd, SystemTimelineRangeType.LockSession);
                        lockStartedAt = null;
                        break;
                    case "logoff":
                    case "system-shutdown":
                        lockStartedAt = null;
                        sleepStartedAt = null;
                        break;
                }
            }

            return ranges;
        }

        private static void AddSystemTimelineRange(
            ICollection<SystemTimelineRange> ranges,
            DateTimeOffset? startedAt,
            DateTimeOffset endedAt,
            DateTimeOffset dayStart,
            DateTimeOffset dayEnd,
            SystemTimelineRangeType rangeType)
        {
            if (startedAt is not { } start || endedAt <= start)
                return;

            var effectiveStart = Max(start, dayStart);
            var effectiveEnd = Min(endedAt, dayEnd);
            if (effectiveEnd <= effectiveStart)
                return;

            ranges.Add(new SystemTimelineRange(effectiveStart, effectiveEnd, rangeType));
        }

        private void AddUntrackedTimelineRows(
            SqliteConnection connection,
            List<ActivityTimelineRow> rows,
            DateTimeOffset dayStart,
            DateTimeOffset dayEnd,
            DateTimeOffset now)
        {
            var trackedIntervals = GetRuntimeIntervalsForDay(connection, dayStart, dayEnd, now);

            var cursor = dayStart;
            foreach (var interval in MergeIntervals(trackedIntervals))
            {
                if (interval.Start > cursor)
                    AddTimelineRow(
                        rows,
                        UiText.Main.Untracked,
                        cursor,
                        interval.Start,
                        interval.Start,
                        UiText.Main.TimePilotUntracked,
                        null);

                if (interval.End > cursor)
                    cursor = interval.End;
            }

            var gapEnd = Min(now, dayEnd);
            if (gapEnd > cursor)
                AddTimelineRow(
                    rows,
                    UiText.Main.Untracked,
                    cursor,
                    gapEnd,
                    gapEnd,
                    UiText.Main.TimePilotUntracked,
                    null);
        }

        private static IReadOnlyList<(DateTimeOffset Start, DateTimeOffset End)> GetRuntimeIntervalsForDay(
            SqliteConnection connection,
            DateTimeOffset dayStart,
            DateTimeOffset dayEnd,
            DateTimeOffset now)
        {
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT started_at, ended_at
                FROM app_runtime_sessions
                WHERE started_at < $dayEnd
                  AND COALESCE(ended_at, $now) > $dayStart
                ORDER BY started_at;
                """;
            command.Parameters.AddWithValue("$dayStart", FormatTimestamp(dayStart));
            command.Parameters.AddWithValue("$dayEnd", FormatTimestamp(dayEnd));
            command.Parameters.AddWithValue("$now", FormatTimestamp(now));

            var intervals = new List<(DateTimeOffset Start, DateTimeOffset End)>();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var startedAt = ParseTimestamp(reader.GetString(0));
                var endedAt = reader.IsDBNull(1) ? now : ParseTimestamp(reader.GetString(1));
                var effectiveStart = Max(startedAt, dayStart);
                var effectiveEnd = Min(endedAt, dayEnd);

                if (effectiveEnd > effectiveStart)
                    intervals.Add((effectiveStart, effectiveEnd));
            }

            return intervals;
        }

        private static long GetLongestGapMs(
            IReadOnlyList<(DateTimeOffset Start, DateTimeOffset End)> intervals,
            DateTimeOffset windowStart,
            DateTimeOffset windowEnd)
        {
            var longestGapMs = 0L;
            var cursor = windowStart;

            foreach (var interval in intervals)
            {
                if (interval.Start > cursor)
                    longestGapMs = Math.Max(longestGapMs, (long)(interval.Start - cursor).TotalMilliseconds);

                if (interval.End > cursor)
                    cursor = interval.End;
            }

            if (windowEnd > cursor)
                longestGapMs = Math.Max(longestGapMs, (long)(windowEnd - cursor).TotalMilliseconds);

            return longestGapMs;
        }

        private static long? GetBootBeforeTimePilotMs(
            SqliteConnection connection,
            DateTimeOffset dayStart,
            DateTimeOffset dayEnd)
        {
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT system_booted_at, started_at
                FROM app_runtime_sessions
                WHERE system_booted_at IS NOT NULL
                  AND started_at >= $dayStart
                  AND started_at < $dayEnd
                ORDER BY started_at
                LIMIT 1;
                """;
            command.Parameters.AddWithValue("$dayStart", FormatTimestamp(dayStart));
            command.Parameters.AddWithValue("$dayEnd", FormatTimestamp(dayEnd));

            using var reader = command.ExecuteReader();
            if (!reader.Read())
                return null;

            var systemBootedAt = ParseTimestamp(reader.GetString(0));
            var startedAt = ParseTimestamp(reader.GetString(1));
            var effectiveBootedAt = Max(systemBootedAt, dayStart);
            var effectiveStartedAt = Min(startedAt, dayEnd);

            if (effectiveStartedAt <= effectiveBootedAt)
                return null;

            return (long)(effectiveStartedAt - effectiveBootedAt).TotalMilliseconds;
        }

        public IReadOnlyList<ProcessRuntimeSummaryRow> GetProcessRuntimeUsageForDay(DateTimeOffset now)
        {
            var localDayStart = now.ToLocalTime().Date;
            return GetProcessRuntimeUsageForDate(localDayStart, now);
        }

        public IReadOnlyList<ProcessRuntimeSummaryRow> GetProcessRuntimeUsageForDate(DateTime localDate, DateTimeOffset now)
        {
            var (dayStart, dayEnd) = GetLocalDayRange(localDate);

            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT
                    a.id,
                    a.display_name,
                    a.process_name,
                    a.executable_path,
                    a.primary_category_id,
                    c.name,
                    prs.started_at,
                    prs.ended_at,
                    prs.last_observed_at,
                    prs.has_main_window,
                    prs.is_current_session_process
                FROM process_runtime_sessions prs
                INNER JOIN apps a ON a.id = prs.app_id
                LEFT JOIN app_categories c ON c.id = a.primary_category_id
                WHERE prs.started_at < $dayEnd
                  AND COALESCE(prs.ended_at, prs.last_observed_at, prs.started_at) > $dayStart;
                """;
            command.Parameters.AddWithValue("$dayStart", FormatTimestamp(dayStart));
            command.Parameters.AddWithValue("$dayEnd", FormatTimestamp(dayEnd));

            var totals = new Dictionary<long, ProcessRuntimeAggregation>();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var appId = reader.GetInt64(0);
                var appName = reader.GetString(1);
                var processName = reader.GetString(2);
                var executablePath = reader.IsDBNull(3) ? null : reader.GetString(3);
                var primaryCategoryId = reader.IsDBNull(4) ? (long?)null : reader.GetInt64(4);
                var categoryName = reader.IsDBNull(5) ? null : reader.GetString(5);
                var startedAt = ParseTimestamp(reader.GetString(6));
                var hasRunningSession = reader.IsDBNull(7);
                var endedAt = hasRunningSession ? (DateTimeOffset?)null : ParseTimestamp(reader.GetString(7));
                var lastObservedAt = reader.IsDBNull(8)
                    ? endedAt ?? startedAt
                    : ParseTimestamp(reader.GetString(8));
                var runtimeEnd = endedAt ?? now;
                var hasMainWindow = !reader.IsDBNull(9) && reader.GetInt32(9) == 1;
                var isCurrentSessionProcess = !reader.IsDBNull(10) && reader.GetInt32(10) == 1;
                var effectiveStart = Max(startedAt, dayStart);
                var effectiveEnd = Min(runtimeEnd, dayEnd);
                var effectiveLastObservedAt = Min(Max(lastObservedAt, dayStart), dayEnd);
                if (effectiveEnd <= effectiveStart && !hasRunningSession)
                    continue;

                if (!totals.TryGetValue(appId, out var aggregation))
                {
                    aggregation = new ProcessRuntimeAggregation(
                        appName,
                        processName,
                        effectiveStart,
                        effectiveLastObservedAt,
                        executablePath,
                        primaryCategoryId,
                        categoryName);
                    totals[appId] = aggregation;
                }

                aggregation.ExecutablePath ??= executablePath;
                aggregation.PrimaryCategoryId ??= primaryCategoryId;
                aggregation.CategoryName ??= categoryName;
                aggregation.AddRuntimeInterval(effectiveStart, effectiveEnd);
                aggregation.HasRunningSession |= hasRunningSession;
                aggregation.HasMainWindow |= hasMainWindow;
                aggregation.IsCurrentSessionProcess |= isCurrentSessionProcess;
                aggregation.FirstObservedAt = Min(aggregation.FirstObservedAt, effectiveStart);
                aggregation.LastObservedAt = Max(aggregation.LastObservedAt, effectiveLastObservedAt);
            }

            AddActiveUsageToRuntimeAggregations(connection, totals, dayStart, dayEnd);
            AddIdleRecordedTimeToRuntimeAggregations(connection, totals, dayStart, dayEnd, now);

            return totals
                .Select(x => new ProcessRuntimeSummaryRow(
                    x.Key,
                    x.Value.AppName,
                    x.Value.ProcessName,
                    x.Value.ExecutablePath,
                    x.Value.PrimaryCategoryId,
                    x.Value.CategoryName,
                    x.Value.GetMergedRuntimeMs(),
                    x.Value.ActiveUsageMs,
                    x.Value.IdleRecordedMs,
                    x.Value.GetMergedRuntimeMs() > 0
                        ? Math.Min(1, (double)x.Value.ActiveUsageMs / x.Value.GetMergedRuntimeMs())
                        : null,
                    x.Value.GetMergedRuntimeSegmentCount(),
                    x.Value.HasRunningSession,
                    x.Value.HasMainWindow,
                    x.Value.IsCurrentSessionProcess,
                    null,
                    x.Value.FirstObservedAt,
                    x.Value.LastObservedAt))
                .OrderByDescending(x => x.RuntimeMs)
                .ToList();
        }

        public IReadOnlyList<ProcessRuntimeSegmentRow> GetProcessRuntimeSegmentsForDay(long appId, DateTimeOffset now)
        {
            var localDayStart = now.ToLocalTime().Date;
            return GetProcessRuntimeSegmentsForDate(appId, localDayStart, now);
        }

        public IReadOnlyList<ProcessRuntimeSegmentRow> GetProcessRuntimeSegmentsForDate(
            long appId,
            DateTime localDate,
            DateTimeOffset now)
        {
            var (dayStart, dayEnd) = GetLocalDayRange(localDate);

            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT
                    process_id,
                    started_at,
                    ended_at,
                    last_observed_at,
                    has_main_window,
                    is_current_session_process
                FROM process_runtime_sessions
                WHERE app_id = $appId
                  AND started_at < $dayEnd
                  AND COALESCE(ended_at, last_observed_at, started_at) > $dayStart
                ORDER BY started_at DESC;
                """;
            command.Parameters.AddWithValue("$appId", appId);
            command.Parameters.AddWithValue("$dayStart", FormatTimestamp(dayStart));
            command.Parameters.AddWithValue("$dayEnd", FormatTimestamp(dayEnd));

            var rows = new List<ProcessRuntimeSegmentRow>();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var processId = reader.GetInt32(0);
                var startedAt = ParseTimestamp(reader.GetString(1));
                DateTimeOffset? endedAt = reader.IsDBNull(2) ? null : ParseTimestamp(reader.GetString(2));
                var observedEnd = endedAt ?? now;
                var effectiveStart = Max(startedAt, dayStart);
                var effectiveEnd = Min(observedEnd, dayEnd);
                var durationMs = Math.Max(0, (long)(effectiveEnd - effectiveStart).TotalMilliseconds);
                if (durationMs <= 0 && endedAt is not null)
                    continue;

                rows.Add(new ProcessRuntimeSegmentRow(
                    effectiveStart,
                    endedAt,
                    durationMs,
                    processId,
                    !reader.IsDBNull(4) && reader.GetInt32(4) == 1,
                    !reader.IsDBNull(5) && reader.GetInt32(5) == 1));
            }

            return rows;
        }

        public IReadOnlyList<ProcessRuntimeSegmentExportRow> GetProcessRuntimeSegmentExportsForDay(DateTimeOffset now)
        {
            var localDayStart = now.ToLocalTime().Date;
            return GetProcessRuntimeSegmentExportsForDate(localDayStart, now);
        }

        public IReadOnlyList<ProcessRuntimeSegmentExportRow> GetProcessRuntimeSegmentExportsForDate(DateTime localDate, DateTimeOffset now)
        {
            var dayStart = new DateTimeOffset(localDate.Date, TimeZoneInfo.Local.GetUtcOffset(localDate.Date));
            var dayEnd = dayStart.AddDays(1);

            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT
                    a.display_name,
                    c.name,
                    a.process_name,
                    prs.started_at,
                    prs.ended_at,
                    prs.last_observed_at,
                    prs.has_main_window,
                    prs.is_current_session_process
                FROM process_runtime_sessions prs
                INNER JOIN apps a ON a.id = prs.app_id
                LEFT JOIN app_categories c ON c.id = a.primary_category_id
                WHERE prs.started_at < $dayEnd
                  AND COALESCE(prs.ended_at, prs.last_observed_at, prs.started_at) > $dayStart
                ORDER BY prs.started_at DESC;
                """;
            command.Parameters.AddWithValue("$dayStart", FormatTimestamp(dayStart));
            command.Parameters.AddWithValue("$dayEnd", FormatTimestamp(dayEnd));

            var rows = new List<ProcessRuntimeSegmentExportRow>();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var appName = reader.GetString(0);
                var categoryName = reader.IsDBNull(1) ? null : reader.GetString(1);
                var processName = reader.GetString(2);
                var startedAt = ParseTimestamp(reader.GetString(3));
                DateTimeOffset? endedAt = reader.IsDBNull(4) ? null : ParseTimestamp(reader.GetString(4));
                var lastObservedAt = reader.IsDBNull(5) ? startedAt : ParseTimestamp(reader.GetString(5));
                var observedEnd = endedAt ?? Min(lastObservedAt, now);
                var effectiveStart = Max(startedAt, dayStart);
                var effectiveEnd = Min(observedEnd, dayEnd);
                var durationMs = Math.Max(0, (long)(effectiveEnd - effectiveStart).TotalMilliseconds);
                if (durationMs <= 0 && endedAt is not null)
                    continue;

                DateTimeOffset? exportedEnd = endedAt is null && now < dayEnd && observedEnd == now
                    ? null
                    : effectiveEnd;

                rows.Add(new ProcessRuntimeSegmentExportRow(
                    appName,
                    categoryName,
                    processName,
                    effectiveStart,
                    exportedEnd,
                    durationMs,
                    !reader.IsDBNull(6) && reader.GetInt32(6) == 1,
                    !reader.IsDBNull(7) && reader.GetInt32(7) == 1));
            }

            return rows;
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
        }

        private static void EnsureAppsColumns(SqliteConnection connection)
        {
            if (!ColumnExists(connection, "apps", "executable_path"))
            {
                using var command = connection.CreateCommand();
                command.CommandText = "ALTER TABLE apps ADD COLUMN executable_path TEXT NULL;";
                command.ExecuteNonQuery();
            }

            AddColumnIfMissing(connection, "apps", "primary_category_id", "INTEGER NULL");

            using var indexCommand = connection.CreateCommand();
            indexCommand.CommandText = """
                CREATE INDEX IF NOT EXISTS idx_apps_primary_category_id
                    ON apps(primary_category_id);
                """;
            indexCommand.ExecuteNonQuery();
        }

        private static void SeedDefaultAppCategories(SqliteConnection connection, DateTimeOffset now)
        {
            var timestamp = FormatTimestamp(now);
            foreach (var category in AppCategoryDisplay.BuiltinCategories)
            {
                using var command = connection.CreateCommand();
                command.CommandText = """
                    INSERT INTO app_categories (
                        name,
                        color,
                        sort_order,
                        is_builtin,
                        created_at,
                        updated_at
                    )
                    VALUES ($name, $color, $sortOrder, 1, $createdAt, $updatedAt)
                    ON CONFLICT(name) DO UPDATE SET
                        sort_order = excluded.sort_order,
                        updated_at = excluded.updated_at
                    WHERE app_categories.is_builtin = 1;
                    """;
                command.Parameters.AddWithValue("$name", category.CanonicalName);
                command.Parameters.AddWithValue("$color", category.Color);
                command.Parameters.AddWithValue("$sortOrder", category.SortOrder);
                command.Parameters.AddWithValue("$createdAt", timestamp);
                command.Parameters.AddWithValue("$updatedAt", timestamp);
                command.ExecuteNonQuery();
            }
        }

        private static void RenameBuiltinAppCategoriesToCanonical(SqliteConnection connection, DateTimeOffset now)
        {
            var timestamp = FormatTimestamp(now);
            foreach (var category in AppCategoryDisplay.BuiltinCategories)
            {
                using var command = connection.CreateCommand();
                command.CommandText = """
                    UPDATE app_categories
                    SET name = $canonicalName,
                        updated_at = $updatedAt
                    WHERE is_builtin = 1
                      AND sort_order = $sortOrder
                      AND NOT EXISTS (
                          SELECT 1
                          FROM app_categories existing
                          WHERE existing.name = $canonicalName
                            AND existing.id <> app_categories.id
                      );
                    """;
                command.Parameters.AddWithValue("$canonicalName", category.CanonicalName);
                command.Parameters.AddWithValue("$color", category.Color);
                command.Parameters.AddWithValue("$sortOrder", category.SortOrder);
                command.Parameters.AddWithValue("$updatedAt", timestamp);
                command.ExecuteNonQuery();
            }
        }

        private static int GetNextCustomAppCategorySortOrder(SqliteConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT COALESCE(MAX(sort_order), 100) + 10
                FROM app_categories;
                """;
            return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
        }

        private static string NormalizeAppCategoryName(string name)
        {
            var normalized = name.Trim();
            if (string.IsNullOrWhiteSpace(normalized))
                throw new ArgumentException("Category name is required.", nameof(name));

            return normalized;
        }

        private static void EnsureAppCategoryNameIsUnique(SqliteConnection connection, string name, long? currentCategoryId)
        {
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT id, name, sort_order, is_builtin
                FROM app_categories;
                """;

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var categoryId = reader.GetInt64(0);
                if (currentCategoryId.HasValue && categoryId == currentCategoryId.Value)
                    continue;

                var existingName = reader.GetString(1);
                var sortOrder = reader.GetInt32(2);
                var isBuiltin = reader.GetInt32(3) != 0;
                if (IsSameAppCategoryName(name, existingName, sortOrder, isBuiltin))
                    throw new InvalidOperationException("A category with the same display name already exists.");
            }
        }

        private static bool IsSameAppCategoryName(string name, string existingName, int sortOrder, bool isBuiltin)
        {
            if (string.Equals(name, existingName, StringComparison.OrdinalIgnoreCase))
                return true;

            var displayName = AppCategoryDisplay.GetDisplayName(existingName, sortOrder);
            if (string.Equals(name, displayName, StringComparison.OrdinalIgnoreCase))
                return true;

            if (!isBuiltin)
                return false;

            var builtinCategory = AppCategoryDisplay.BuiltinCategories.FirstOrDefault(x =>
                x.SortOrder == sortOrder
                || string.Equals(x.CanonicalName, existingName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(x.KoreanName, existingName, StringComparison.OrdinalIgnoreCase));
            return builtinCategory is not null
                && (string.Equals(name, builtinCategory.CanonicalName, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(name, builtinCategory.KoreanName, StringComparison.OrdinalIgnoreCase));
        }

        private static string? NormalizeAppCategoryColor(string? color)
        {
            var normalized = color?.Trim();
            if (string.IsNullOrWhiteSpace(normalized))
                return null;

            try
            {
                var parsed = ColorTranslator.FromHtml(normalized);
                return ColorTranslator.ToHtml(Color.FromArgb(parsed.R, parsed.G, parsed.B));
            }
            catch
            {
                throw new ArgumentException("Category color must be a valid HTML color.", nameof(color));
            }
        }

        private static void EnsureRuntimeSessionColumns(SqliteConnection connection)
        {
            AddColumnIfMissing(connection, "app_runtime_sessions", "system_booted_at", "TEXT NULL");
        }

        private static void EnsureForegroundSessionColumns(SqliteConnection connection)
        {
            if (!ColumnExists(connection, "foreground_sessions", "last_observed_at"))
            {
                using var command = connection.CreateCommand();
                command.CommandText = "ALTER TABLE foreground_sessions ADD COLUMN last_observed_at TEXT NULL;";
                command.ExecuteNonQuery();
            }
        }

        private static void EnsureProcessRuntimeSessionColumns(SqliteConnection connection)
        {
            AddColumnIfMissing(connection, "process_runtime_sessions", "tracking_scope", "INTEGER NULL");
            AddColumnIfMissing(connection, "process_runtime_sessions", "has_main_window", "INTEGER NULL");
            AddColumnIfMissing(connection, "process_runtime_sessions", "is_current_session_process", "INTEGER NULL");
        }

        private static void AddColumnIfMissing(
            SqliteConnection connection,
            string tableName,
            string columnName,
            string columnDefinition)
        {
            if (ColumnExists(connection, tableName, columnName))
                return;

            using var command = connection.CreateCommand();
            command.CommandText = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnDefinition};";
            command.ExecuteNonQuery();
        }

        private static bool ColumnExists(SqliteConnection connection, string tableName, string columnName)
        {
            using var command = connection.CreateCommand();
            command.CommandText = $"PRAGMA table_info({tableName});";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private void MarkUnexpectedRuntimeSessions(DateTimeOffset now, DateTimeOffset currentSystemBootedAt)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT id, started_at, last_heartbeat_at, system_booted_at
                FROM app_runtime_sessions
                WHERE ended_at IS NULL;
                """;

            var openSessions = new List<(
                long Id,
                DateTimeOffset StartedAt,
                DateTimeOffset EndedAt,
                DateTimeOffset? SystemBootedAt)>();
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var startedAt = ParseTimestamp(reader.GetString(1));
                    var endedAt = reader.IsDBNull(2)
                        ? now
                        : ParseTimestamp(reader.GetString(2));
                    var systemBootedAt = reader.IsDBNull(3)
                        ? (DateTimeOffset?)null
                        : ParseTimestamp(reader.GetString(3));
                    openSessions.Add((reader.GetInt64(0), startedAt, endedAt, systemBootedAt));
                }
            }

            foreach (var session in openSessions)
            {
                var shutdownReason = IsSameSystemBootSession(session.SystemBootedAt, currentSystemBootedAt)
                    ? "unexpected"
                    : "system-shutdown";
                EndRuntimeSession(connection, session.Id, session.EndedAt, shutdownReason);
            }
        }

        private static bool IsSameSystemBootSession(
            DateTimeOffset? previousSystemBootedAt,
            DateTimeOffset currentSystemBootedAt)
        {
            if (previousSystemBootedAt is null)
                return true;

            var difference = (previousSystemBootedAt.Value - currentSystemBootedAt).Duration();
            return difference <= SystemBootTimeTolerance;
        }

        private void MarkUnexpectedProcessRuntimeSessions(DateTimeOffset now)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT id, last_observed_at
                FROM process_runtime_sessions
                WHERE ended_at IS NULL;
                """;

            var openSessions = new List<(long Id, DateTimeOffset EndedAt)>();
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var endedAt = reader.IsDBNull(1)
                        ? now
                        : ParseTimestamp(reader.GetString(1));
                    openSessions.Add((reader.GetInt64(0), endedAt));
                }
            }

            foreach (var session in openSessions)
            {
                EndProcessRuntimeSession(connection, session.Id, session.EndedAt);
            }
        }

        private void EndRuntimeSession(SqliteConnection connection, long sessionId, DateTimeOffset endedAt, string shutdownReason)
        {
            using var selectCommand = connection.CreateCommand();
            selectCommand.CommandText = """
                SELECT started_at
                FROM app_runtime_sessions
                WHERE id = $id AND ended_at IS NULL;
                """;
            selectCommand.Parameters.AddWithValue("$id", sessionId);
            var startedAtValue = selectCommand.ExecuteScalar();
            if (startedAtValue is not string startedAtText)
                return;

            var startedAt = DateTimeOffset.Parse(startedAtText, null, System.Globalization.DateTimeStyles.RoundtripKind);
            var durationMs = Math.Max(0, (long)(endedAt - startedAt).TotalMilliseconds);

            using var updateCommand = connection.CreateCommand();
            updateCommand.CommandText = """
                UPDATE app_runtime_sessions
                SET ended_at = $endedAt,
                    duration_ms = $durationMs,
                    last_heartbeat_at = $endedAt,
                    shutdown_reason = $shutdownReason
                WHERE id = $id AND ended_at IS NULL;
                """;
            updateCommand.Parameters.AddWithValue("$endedAt", FormatTimestamp(endedAt));
            updateCommand.Parameters.AddWithValue("$durationMs", durationMs);
            updateCommand.Parameters.AddWithValue("$shutdownReason", shutdownReason);
            updateCommand.Parameters.AddWithValue("$id", sessionId);
            updateCommand.ExecuteNonQuery();
        }

        private void EndProcessRuntimeSession(
            SqliteConnection connection,
            long sessionId,
            DateTimeOffset endedAt,
            SqliteTransaction? transaction = null)
        {
            using var selectCommand = connection.CreateCommand();
            selectCommand.Transaction = transaction;
            selectCommand.CommandText = """
                SELECT started_at, last_observed_at
                FROM process_runtime_sessions
                WHERE id = $id AND ended_at IS NULL;
                """;
            selectCommand.Parameters.AddWithValue("$id", sessionId);
            DateTimeOffset startedAt;
            DateTimeOffset lastObservedAt;
            using (var reader = selectCommand.ExecuteReader())
            {
                if (!reader.Read())
                    return;

                startedAt = ParseTimestamp(reader.GetString(0));
                lastObservedAt = ParseTimestamp(reader.GetString(1));
            }

            var effectiveEnd = lastObservedAt <= endedAt ? lastObservedAt : endedAt;
            var durationMs = Math.Max(0, (long)(effectiveEnd - startedAt).TotalMilliseconds);

            using var updateCommand = connection.CreateCommand();
            updateCommand.Transaction = transaction;
            updateCommand.CommandText = """
                UPDATE process_runtime_sessions
                SET ended_at = $endedAt,
                    duration_ms = $durationMs
                WHERE id = $id AND ended_at IS NULL;
                """;
            updateCommand.Parameters.AddWithValue("$endedAt", FormatTimestamp(effectiveEnd));
            updateCommand.Parameters.AddWithValue("$durationMs", durationMs);
            updateCommand.Parameters.AddWithValue("$id", sessionId);
            updateCommand.ExecuteNonQuery();
        }

        private long StartProcessRuntimeSession(
            SqliteConnection connection,
            SqliteTransaction transaction,
            long appId,
            int processId,
            ProcessRuntimeTrackingScope trackingScope,
            bool hasMainWindow,
            bool isCurrentSessionProcess,
            DateTimeOffset observedAt)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO process_runtime_sessions (
                    app_id,
                    process_id,
                    started_at,
                    first_observed_at,
                    last_observed_at,
                    tracking_scope,
                    has_main_window,
                    is_current_session_process
                )
                VALUES ($appId, $processId, $startedAt, $firstObservedAt, $lastObservedAt, $trackingScope, $hasMainWindow, $isCurrentSessionProcess);
                SELECT last_insert_rowid();
                """;
            command.Parameters.AddWithValue("$appId", appId);
            command.Parameters.AddWithValue("$processId", processId);
            command.Parameters.AddWithValue("$startedAt", FormatTimestamp(observedAt));
            command.Parameters.AddWithValue("$firstObservedAt", FormatTimestamp(observedAt));
            command.Parameters.AddWithValue("$lastObservedAt", FormatTimestamp(observedAt));
            command.Parameters.AddWithValue("$trackingScope", (int)trackingScope);
            command.Parameters.AddWithValue("$hasMainWindow", hasMainWindow ? 1 : 0);
            command.Parameters.AddWithValue("$isCurrentSessionProcess", isCurrentSessionProcess ? 1 : 0);
            return (long)command.ExecuteScalar()!;
        }

        private void UpdateProcessRuntimeSession(
            SqliteConnection connection,
            SqliteTransaction transaction,
            long sessionId,
            ProcessRuntimeTrackingScope trackingScope,
            bool hasMainWindow,
            bool isCurrentSessionProcess,
            DateTimeOffset observedAt)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                UPDATE process_runtime_sessions
                SET last_observed_at = $lastObservedAt,
                    tracking_scope = $trackingScope,
                    has_main_window = CASE WHEN $hasMainWindow = 1 THEN 1 ELSE has_main_window END,
                    is_current_session_process = CASE WHEN $isCurrentSessionProcess = 1 THEN 1 ELSE is_current_session_process END
                WHERE id = $id AND ended_at IS NULL;
                """;
            command.Parameters.AddWithValue("$lastObservedAt", FormatTimestamp(observedAt));
            command.Parameters.AddWithValue("$trackingScope", (int)trackingScope);
            command.Parameters.AddWithValue("$hasMainWindow", hasMainWindow ? 1 : 0);
            command.Parameters.AddWithValue("$isCurrentSessionProcess", isCurrentSessionProcess ? 1 : 0);
            command.Parameters.AddWithValue("$id", sessionId);
            command.ExecuteNonQuery();
        }

        private long GetOrCreateAppId(
            SqliteConnection connection,
            AppMetadata app,
            DateTimeOffset observedAt,
            SqliteTransaction? transaction = null)
        {
            using var insertCommand = connection.CreateCommand();
            insertCommand.Transaction = transaction;
            insertCommand.CommandText = """
                INSERT INTO apps (
                    process_name,
                    display_name,
                    executable_path,
                    first_seen_at,
                    last_seen_at
                )
                VALUES ($processName, $displayName, $executablePath, $firstSeenAt, $lastSeenAt)
                ON CONFLICT(process_name) DO UPDATE SET
                    display_name = excluded.display_name,
                    executable_path = COALESCE(excluded.executable_path, apps.executable_path),
                    last_seen_at = excluded.last_seen_at;
                """;
            insertCommand.Parameters.AddWithValue("$processName", app.ProcessName);
            insertCommand.Parameters.AddWithValue("$displayName", app.DisplayName);
            insertCommand.Parameters.AddWithValue("$executablePath", (object?)app.ExecutablePath ?? DBNull.Value);
            insertCommand.Parameters.AddWithValue("$firstSeenAt", FormatTimestamp(observedAt));
            insertCommand.Parameters.AddWithValue("$lastSeenAt", FormatTimestamp(observedAt));
            insertCommand.ExecuteNonQuery();

            using var selectCommand = connection.CreateCommand();
            selectCommand.Transaction = transaction;
            selectCommand.CommandText = """
                SELECT id
                FROM apps
                WHERE process_name = $processName;
                """;
            selectCommand.Parameters.AddWithValue("$processName", app.ProcessName);
            return (long)selectCommand.ExecuteScalar()!;
        }

        private void AddForegroundTimelineRows(
            SqliteConnection connection,
            List<ActivityTimelineRow> rows,
            DateTimeOffset dayStart,
            DateTimeOffset dayEnd,
            DateTimeOffset now)
        {
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT
                    a.id,
                    a.display_name,
                    a.process_name,
                    a.executable_path,
                    a.primary_category_id,
                    c.name,
                    fs.started_at,
                    fs.ended_at,
                    fs.last_observed_at
                FROM foreground_sessions fs
                INNER JOIN apps a ON a.id = fs.app_id
                LEFT JOIN app_categories c ON c.id = a.primary_category_id
                WHERE fs.started_at < $dayEnd
                  AND COALESCE(fs.ended_at, fs.last_observed_at, fs.started_at) > $dayStart;
                """;
            command.Parameters.AddWithValue("$dayStart", FormatTimestamp(dayStart));
            command.Parameters.AddWithValue("$dayEnd", FormatTimestamp(dayEnd));

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var appId = reader.GetInt64(0);
                var appName = reader.GetString(1);
                var processName = reader.GetString(2);
                var executablePath = reader.IsDBNull(3) ? null : reader.GetString(3);
                long? primaryCategoryId = reader.IsDBNull(4) ? null : reader.GetInt64(4);
                var categoryName = reader.IsDBNull(5) ? null : reader.GetString(5);
                var startedAt = ParseTimestamp(reader.GetString(6));
                DateTimeOffset? endedAt = reader.IsDBNull(7) ? null : ParseTimestamp(reader.GetString(7));
                var observedEnd = endedAt
                    ?? (reader.IsDBNull(8) ? startedAt : ParseTimestamp(reader.GetString(8)));
                var effectiveStart = Max(startedAt, dayStart);
                var effectiveEnd = Min(observedEnd, dayEnd);
                DateTimeOffset? displayEnd = IsCurrentTimelineSession(endedAt, observedEnd, dayEnd, now)
                    ? null
                    : effectiveEnd;
                AddTimelineRow(
                    rows,
                    UiText.Main.Active,
                    effectiveStart,
                    displayEnd,
                    effectiveEnd,
                    appName,
                    executablePath,
                    processName,
                    appId,
                    primaryCategoryId,
                    categoryName);
            }
        }

        private void AddIdleTimelineRows(
            SqliteConnection connection,
            List<ActivityTimelineRow> rows,
            DateTimeOffset dayStart,
            DateTimeOffset dayEnd,
            DateTimeOffset now)
        {
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT
                    a.id,
                    COALESCE(a.display_name, 'Idle'),
                    a.process_name,
                    a.executable_path,
                    a.primary_category_id,
                    c.name,
                    i.started_at,
                    i.ended_at
                FROM idle_sessions i
                LEFT JOIN apps a ON a.id = i.foreground_app_id
                LEFT JOIN app_categories c ON c.id = a.primary_category_id
                WHERE i.started_at < $dayEnd
                  AND COALESCE(i.ended_at, $now) > $dayStart;
                """;
            command.Parameters.AddWithValue("$dayStart", FormatTimestamp(dayStart));
            command.Parameters.AddWithValue("$dayEnd", FormatTimestamp(dayEnd));
            command.Parameters.AddWithValue("$now", FormatTimestamp(now));

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                long? appId = reader.IsDBNull(0) ? null : reader.GetInt64(0);
                var foregroundAppName = reader.GetString(1);
                var processName = reader.IsDBNull(2) ? "" : reader.GetString(2);
                var executablePath = reader.IsDBNull(3) ? null : reader.GetString(3);
                long? primaryCategoryId = reader.IsDBNull(4) ? null : reader.GetInt64(4);
                var categoryName = reader.IsDBNull(5) ? null : reader.GetString(5);
                var startedAt = ParseTimestamp(reader.GetString(6));
                DateTimeOffset? endedAt = reader.IsDBNull(7) ? null : ParseTimestamp(reader.GetString(7));
                var effectiveStart = Max(startedAt, dayStart);
                var effectiveEnd = Min(endedAt ?? now, dayEnd);
                DateTimeOffset? displayEnd = IsCurrentTimelineSession(endedAt, effectiveEnd, dayEnd, now)
                    ? null
                    : effectiveEnd;
                AddTimelineRow(
                    rows,
                    UiText.Main.Idle,
                    effectiveStart,
                    displayEnd,
                    effectiveEnd,
                    foregroundAppName,
                    executablePath,
                    processName,
                    appId,
                    primaryCategoryId,
                    categoryName);
            }
        }

        private static bool IsCurrentTimelineSession(
            DateTimeOffset? endedAt,
            DateTimeOffset observedEnd,
            DateTimeOffset dayEnd,
            DateTimeOffset now)
        {
            if (endedAt is not null)
                return false;

            if (now >= dayEnd)
                return false;

            return now - observedEnd <= CurrentTimelineSessionTolerance;
        }

        private static void AddTimelineRow(
            List<ActivityTimelineRow> rows,
            string activityType,
            DateTimeOffset effectiveStart,
            DateTimeOffset? displayEnd,
            DateTimeOffset effectiveEnd,
            string displayName,
            string? executablePath,
            string processName = "",
            long? appId = null,
            long? primaryCategoryId = null,
            string? categoryName = null)
        {
            var durationMs = Math.Max(0, (long)(effectiveEnd - effectiveStart).TotalMilliseconds);
            if (durationMs <= 0)
                return;

            rows.Add(new ActivityTimelineRow(
                activityType,
                effectiveStart,
                displayEnd,
                durationMs,
                displayName,
                executablePath,
                ProcessName: processName,
                AppId: appId,
                PrimaryCategoryId: primaryCategoryId,
                CategoryName: categoryName));
        }

        private static void AddActiveUsageToRuntimeAggregations(
            SqliteConnection connection,
            Dictionary<long, ProcessRuntimeAggregation> totals,
            DateTimeOffset dayStart,
            DateTimeOffset dayEnd)
        {
            if (totals.Count == 0)
                return;

            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT
                    fs.app_id,
                    fs.started_at,
                    fs.ended_at,
                    fs.last_observed_at
                FROM foreground_sessions fs
                WHERE fs.started_at < $dayEnd
                  AND COALESCE(fs.ended_at, fs.last_observed_at, fs.started_at) > $dayStart;
                """;
            command.Parameters.AddWithValue("$dayStart", FormatTimestamp(dayStart));
            command.Parameters.AddWithValue("$dayEnd", FormatTimestamp(dayEnd));

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var appId = reader.GetInt64(0);
                if (!totals.TryGetValue(appId, out var aggregation))
                    continue;

                var startedAt = ParseTimestamp(reader.GetString(1));
                var observedEnd = reader.IsDBNull(2)
                    ? reader.IsDBNull(3) ? startedAt : ParseTimestamp(reader.GetString(3))
                    : ParseTimestamp(reader.GetString(2));
                var effectiveStart = Max(startedAt, dayStart);
                var effectiveEnd = Min(observedEnd, dayEnd);
                var activeUsageMs = Math.Max(0, (long)(effectiveEnd - effectiveStart).TotalMilliseconds);
                aggregation.ActiveUsageMs += activeUsageMs;
            }
        }

        private static void AddIdleRecordedTimeToUsageAggregations(
            SqliteConnection connection,
            Dictionary<long, UsageAggregation> totals,
            DateTimeOffset periodStart,
            DateTimeOffset periodEnd,
            DateTimeOffset now)
        {
            if (totals.Count == 0)
                return;

            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT
                    i.foreground_app_id,
                    i.started_at,
                    i.ended_at
                FROM idle_sessions i
                WHERE i.foreground_app_id IS NOT NULL
                  AND i.started_at < $periodEnd
                  AND COALESCE(i.ended_at, $now) > $periodStart;
                """;
            command.Parameters.AddWithValue("$periodStart", FormatTimestamp(periodStart));
            command.Parameters.AddWithValue("$periodEnd", FormatTimestamp(periodEnd));
            command.Parameters.AddWithValue("$now", FormatTimestamp(now));

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var appId = reader.GetInt64(0);
                if (!totals.TryGetValue(appId, out var aggregation))
                    continue;

                var startedAt = ParseTimestamp(reader.GetString(1));
                var endedAt = reader.IsDBNull(2) ? now : ParseTimestamp(reader.GetString(2));
                var effectiveStart = Max(startedAt, periodStart);
                var effectiveEnd = Min(endedAt, periodEnd);
                var idleRecordedMs = Math.Max(0, (long)(effectiveEnd - effectiveStart).TotalMilliseconds);
                aggregation.IdleRecordedMs += idleRecordedMs;
            }
        }

        private static void AddIdleRecordedTimeToRuntimeAggregations(
            SqliteConnection connection,
            Dictionary<long, ProcessRuntimeAggregation> totals,
            DateTimeOffset dayStart,
            DateTimeOffset dayEnd,
            DateTimeOffset now)
        {
            if (totals.Count == 0)
                return;

            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT
                    i.foreground_app_id,
                    i.started_at,
                    i.ended_at
                FROM idle_sessions i
                WHERE i.foreground_app_id IS NOT NULL
                  AND i.started_at < $dayEnd
                  AND COALESCE(i.ended_at, $now) > $dayStart;
                """;
            command.Parameters.AddWithValue("$dayStart", FormatTimestamp(dayStart));
            command.Parameters.AddWithValue("$dayEnd", FormatTimestamp(dayEnd));
            command.Parameters.AddWithValue("$now", FormatTimestamp(now));

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var appId = reader.GetInt64(0);
                if (!totals.TryGetValue(appId, out var aggregation))
                    continue;

                var startedAt = ParseTimestamp(reader.GetString(1));
                var endedAt = reader.IsDBNull(2) ? now : ParseTimestamp(reader.GetString(2));
                var effectiveStart = Max(startedAt, dayStart);
                var effectiveEnd = Min(endedAt, dayEnd);
                var idleRecordedMs = Math.Max(0, (long)(effectiveEnd - effectiveStart).TotalMilliseconds);
                aggregation.IdleRecordedMs += idleRecordedMs;
            }
        }

        private SqliteConnection OpenConnection()
        {
            var connection = new SqliteConnection(connectionString);
            connection.Open();
            return connection;
        }

        private static string FormatTimestamp(DateTimeOffset timestamp)
        {
            return timestamp.ToUniversalTime().ToString("O");
        }

        private static DateTimeOffset ParseTimestamp(string timestamp)
        {
            return DateTimeOffset.Parse(timestamp, null, System.Globalization.DateTimeStyles.RoundtripKind);
        }

        private static DateTimeOffset Min(DateTimeOffset left, DateTimeOffset right)
        {
            return left <= right ? left : right;
        }

        private static DateTimeOffset Max(DateTimeOffset left, DateTimeOffset right)
        {
            return left >= right ? left : right;
        }

        private static DateTimeOffset? MaxNullable(DateTimeOffset? left, DateTimeOffset? right)
        {
            return (left, right) switch
            {
                ({ } leftValue, { } rightValue) => Max(leftValue, rightValue),
                ({ } leftValue, null) => leftValue,
                (null, { } rightValue) => rightValue,
                _ => null
            };
        }

        private static (DateTimeOffset Start, DateTimeOffset End) GetLocalDayRange(DateTime localDate)
        {
            var dayStartDate = localDate.Date;
            var dayEndDate = dayStartDate.AddDays(1);
            return (
                new DateTimeOffset(dayStartDate, TimeZoneInfo.Local.GetUtcOffset(dayStartDate)),
                new DateTimeOffset(dayEndDate, TimeZoneInfo.Local.GetUtcOffset(dayEndDate)));
        }

        private static void AddActivityDates(HashSet<DateTime> dates, DateTimeOffset start, DateTimeOffset end)
        {
            if (end <= start)
                return;

            var cursor = start;
            while (cursor < end)
            {
                var localDate = cursor.ToLocalTime().Date;
                dates.Add(localDate);

                var (_, dayEnd) = GetLocalDayRange(localDate);
                cursor = Min(end, dayEnd);
            }
        }

        private static void AddDailyUsageTrend(
            Dictionary<DateTime, DailyUsageTrendAggregation> totals,
            string appName,
            DateTimeOffset start,
            DateTimeOffset end)
        {
            var cursor = start;
            while (cursor < end)
            {
                var localDate = cursor.ToLocalTime().Date;
                var (_, dayEnd) = GetLocalDayRange(localDate);
                var segmentEnd = Min(end, dayEnd);
                var durationMs = Math.Max(0, (long)(segmentEnd - cursor).TotalMilliseconds);

                if (durationMs > 0)
                {
                    if (!totals.TryGetValue(localDate, out var aggregation))
                    {
                        aggregation = new DailyUsageTrendAggregation();
                        totals[localDate] = aggregation;
                    }

                    aggregation.ActiveUsageMs += durationMs;
                    aggregation.AppTotals.TryGetValue(appName, out var appTotalMs);
                    aggregation.AppTotals[appName] = appTotalMs + durationMs;
                }

                cursor = segmentEnd;
            }
        }

        private static void AddCategoryBucketDurations(
            Dictionary<int, Dictionary<string, CategoryBucketTotal>> buckets,
            DateTimeOffset dayStart,
            TimeSpan bucketSize,
            int bucketCount,
            string categoryName,
            string? color,
            DateTimeOffset start,
            DateTimeOffset end)
        {
            var cursor = start;
            while (cursor < end)
            {
                var bucketIndex = (int)Math.Floor((cursor - dayStart).TotalMilliseconds / bucketSize.TotalMilliseconds);
                bucketIndex = Math.Clamp(bucketIndex, 0, bucketCount - 1);
                var bucketStart = dayStart + TimeSpan.FromTicks(bucketSize.Ticks * bucketIndex);
                var bucketEnd = Min(bucketStart + bucketSize, dayStart.AddDays(1));
                var segmentEnd = Min(end, bucketEnd);
                var durationMs = Math.Max(0, (long)(segmentEnd - cursor).TotalMilliseconds);

                if (durationMs > 0)
                {
                    if (!buckets.TryGetValue(bucketIndex, out var bucket))
                    {
                        bucket = new Dictionary<string, CategoryBucketTotal>(StringComparer.OrdinalIgnoreCase);
                        buckets[bucketIndex] = bucket;
                    }

                    if (!bucket.TryGetValue(categoryName, out var total))
                    {
                        total = new CategoryBucketTotal(categoryName, color);
                        bucket[categoryName] = total;
                    }

                    total.ActiveUsageMs += durationMs;
                }

                cursor = segmentEnd;
            }
        }

        private static CategoryTimelineSegment CreateCategoryTimelineSegment(
            DateTimeOffset dayStart,
            TimeSpan bucketSize,
            int bucketIndex,
            IReadOnlyDictionary<string, CategoryBucketTotal> totals)
        {
            var startedAt = dayStart + TimeSpan.FromTicks(bucketSize.Ticks * bucketIndex);
            var endedAt = Min(startedAt + bucketSize, dayStart.AddDays(1));
            var totalMs = totals.Values.Sum(x => x.ActiveUsageMs);
            var ordered = totals.Values
                .OrderByDescending(x => x.ActiveUsageMs)
                .ThenBy(x => x.CategoryName)
                .ToList();
            var top = ordered.First();
            var topShare = (double)top.ActiveUsageMs / Math.Max(1, totalMs);
            var isDistributed = topShare < 0.5;
            var detailParts = ordered
                .Take(3)
                .Select(x => $"{AppCategoryDisplay.GetDisplayName(x.CategoryName)} {((double)x.ActiveUsageMs / Math.Max(1, totalMs)).ToString("P0", CultureInfo.CurrentCulture)}");
            var detailText = string.Join(
                ", ",
                isDistributed
                    ? detailParts.Prepend(UiText.Main.TimelineCategoryDistributed)
                    : detailParts);

            return new CategoryTimelineSegment(
                startedAt,
                endedAt,
                AppCategoryDisplay.GetDisplayName(top.CategoryName),
                top.Color,
                isDistributed,
                totalMs,
                detailText);
        }

        private static CategoryTimelineSegment CreateWholeDayCategoryTimelineSegment(
            DateTimeOffset dayStart,
            DateTimeOffset dayEnd,
            IReadOnlyDictionary<string, CategoryBucketTotal> totals)
        {
            var totalMs = totals.Values.Sum(x => x.ActiveUsageMs);
            var ordered = totals.Values
                .OrderByDescending(x => x.ActiveUsageMs)
                .ThenBy(x => x.CategoryName)
                .ToList();
            var top = ordered.First();
            var topShare = (double)top.ActiveUsageMs / Math.Max(1, totalMs);
            var isDistributed = topShare < 0.5;
            var topCategoryName = AppCategoryDisplay.GetDisplayName(top.CategoryName);
            var categoryName = UiText.Main.TimelineOverallCategoryLabel(topCategoryName, topShare, isDistributed);
            var detailText = string.Join(
                " | ",
                UiText.Main.TimelineCategoryRecordedActiveBasis,
                string.Join(
                    ", ",
                    ordered
                        .Take(4)
                        .Select(x => $"{AppCategoryDisplay.GetDisplayName(x.CategoryName)} {((double)x.ActiveUsageMs / Math.Max(1, totalMs)).ToString("P0", CultureInfo.CurrentCulture)}")));

            return new CategoryTimelineSegment(
                dayStart,
                dayEnd,
                categoryName,
                top.Color,
                isDistributed,
                totalMs,
                detailText);
        }

        private static IReadOnlyList<(DateTimeOffset Start, DateTimeOffset End)> MergeIntervals(
            IReadOnlyList<(DateTimeOffset Start, DateTimeOffset End)> intervals)
        {
            if (intervals.Count == 0)
                return [];

            var ordered = intervals
                .OrderBy(interval => interval.Start)
                .ToList();
            var merged = new List<(DateTimeOffset Start, DateTimeOffset End)> { ordered[0] };

            foreach (var interval in ordered.Skip(1))
            {
                var last = merged[^1];
                if (interval.Start <= last.End)
                {
                    merged[^1] = (last.Start, Max(last.End, interval.End));
                    continue;
                }

                merged.Add(interval);
            }

            return merged;
        }

        private sealed class UsageAggregation
        {
            public UsageAggregation(
                long appId,
                string appName,
                string processName,
                DateTimeOffset firstStartedAt,
                DateTimeOffset lastObservedAt,
                string? executablePath,
                long? primaryCategoryId,
                string? categoryName)
            {
                AppId = appId;
                AppName = appName;
                ProcessName = processName;
                FirstStartedAt = firstStartedAt;
                LastObservedAt = lastObservedAt;
                ExecutablePath = executablePath;
                PrimaryCategoryId = primaryCategoryId;
                CategoryName = categoryName;
            }

            public long AppId { get; }

            public string AppName { get; }

            public string ProcessName { get; }

            public long ActiveUsageMs { get; set; }

            public long IdleRecordedMs { get; set; }

            public string? ExecutablePath { get; set; }

            public long? PrimaryCategoryId { get; set; }

            public string? CategoryName { get; set; }

            public int SwitchCount { get; set; }

            public DateTimeOffset FirstStartedAt { get; set; }

            public DateTimeOffset LastObservedAt { get; set; }
        }

        private sealed class DailyUsageTrendAggregation
        {
            public long ActiveUsageMs { get; set; }

            public Dictionary<string, long> AppTotals { get; } = new(StringComparer.OrdinalIgnoreCase);
        }

        private sealed class CategoryBucketTotal
        {
            public CategoryBucketTotal(string categoryName, string? color)
            {
                CategoryName = categoryName;
                Color = color;
            }

            public string CategoryName { get; }

            public string? Color { get; }

            public long ActiveUsageMs { get; set; }
        }

        private sealed class ProcessRuntimeAggregation
        {
            private readonly List<(DateTimeOffset Start, DateTimeOffset End)> runtimeIntervals = new();

            public ProcessRuntimeAggregation(
                string appName,
                string processName,
                DateTimeOffset firstObservedAt,
                DateTimeOffset lastObservedAt,
                string? executablePath,
                long? primaryCategoryId,
                string? categoryName)
            {
                AppName = appName;
                ProcessName = processName;
                FirstObservedAt = firstObservedAt;
                LastObservedAt = lastObservedAt;
                ExecutablePath = executablePath;
                PrimaryCategoryId = primaryCategoryId;
                CategoryName = categoryName;
            }

            public string AppName { get; }

            public string ProcessName { get; }

            public long ActiveUsageMs { get; set; }

            public long IdleRecordedMs { get; set; }

            public string? ExecutablePath { get; set; }

            public long? PrimaryCategoryId { get; set; }

            public string? CategoryName { get; set; }

            public bool HasRunningSession { get; set; }

            public bool HasMainWindow { get; set; }

            public bool IsCurrentSessionProcess { get; set; }

            public DateTimeOffset FirstObservedAt { get; set; }

            public DateTimeOffset LastObservedAt { get; set; }

            public void AddRuntimeInterval(DateTimeOffset start, DateTimeOffset end)
            {
                runtimeIntervals.Add((start, end));
            }

            public long GetMergedRuntimeMs()
            {
                return GetMergedRuntimeIntervals()
                    .Sum(interval => Math.Max(0, (long)(interval.End - interval.Start).TotalMilliseconds));
            }

            public int GetMergedRuntimeSegmentCount()
            {
                return GetMergedRuntimeIntervals().Count;
            }

            private IReadOnlyList<(DateTimeOffset Start, DateTimeOffset End)> GetMergedRuntimeIntervals()
            {
                if (runtimeIntervals.Count == 0)
                    return Array.Empty<(DateTimeOffset Start, DateTimeOffset End)>();

                var merged = new List<(DateTimeOffset Start, DateTimeOffset End)>();
                foreach (var interval in runtimeIntervals.OrderBy(x => x.Start))
                {
                    if (merged.Count == 0)
                    {
                        merged.Add(interval);
                        continue;
                    }

                    var last = merged[^1];
                    if (interval.Start <= last.End)
                    {
                        merged[^1] = (last.Start, Max(last.End, interval.End));
                        continue;
                    }

                    merged.Add(interval);
                }

                return merged;
            }
        }
    }
}
