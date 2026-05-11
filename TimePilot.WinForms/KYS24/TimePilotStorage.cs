using Microsoft.Data.Sqlite;

namespace TimePilot.WinForms.KYS24
{
    internal sealed class TimePilotStorage : IDisposable
    {
        private readonly string databasePath;
        private readonly string connectionString;
        private long? runtimeSessionId;
        private bool disposed;

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
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var dataDirectory = Path.Combine(appDataPath, "TimePilot");
            return new TimePilotStorage(Path.Combine(dataDirectory, "timepilot.db"));
        }

        public void Initialize(DateTimeOffset now)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);

            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                CREATE TABLE IF NOT EXISTS apps (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    process_name TEXT NOT NULL UNIQUE COLLATE NOCASE,
                    display_name TEXT NOT NULL,
                    executable_path TEXT NULL,
                    first_seen_at TEXT NOT NULL,
                    last_seen_at TEXT NOT NULL,
                    user_alias TEXT NULL,
                    is_excluded INTEGER NOT NULL DEFAULT 0
                );

                CREATE TABLE IF NOT EXISTS app_runtime_sessions (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    started_at TEXT NOT NULL,
                    ended_at TEXT NULL,
                    duration_ms INTEGER NULL,
                    last_heartbeat_at TEXT NULL,
                    shutdown_reason TEXT NULL,
                    app_version TEXT NULL
                );

                CREATE TABLE IF NOT EXISTS foreground_sessions (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    app_id INTEGER NOT NULL,
                    started_at TEXT NOT NULL,
                    ended_at TEXT NULL,
                    duration_ms INTEGER NULL,
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
                    FOREIGN KEY (app_id) REFERENCES apps(id)
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
                """;
            command.ExecuteNonQuery();

            EnsureAppsColumns(connection);
            MarkUnexpectedRuntimeSessions(now);
            MarkUnexpectedProcessRuntimeSessions(now);
        }

        public void BeginRuntimeSession(DateTimeOffset startedAt, string? appVersion)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO app_runtime_sessions (
                    started_at,
                    last_heartbeat_at,
                    shutdown_reason,
                    app_version
                )
                VALUES ($startedAt, $lastHeartbeatAt, $shutdownReason, $appVersion);
                SELECT last_insert_rowid();
                """;
            command.Parameters.AddWithValue("$startedAt", FormatTimestamp(startedAt));
            command.Parameters.AddWithValue("$lastHeartbeatAt", FormatTimestamp(startedAt));
            command.Parameters.AddWithValue("$shutdownReason", "running");
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
                    started_at
                )
                VALUES ($appId, $startedAt);
                SELECT last_insert_rowid();
                """;
            command.Parameters.AddWithValue("$appId", appId);
            command.Parameters.AddWithValue("$startedAt", FormatTimestamp(startedAt));
            return (long)command.ExecuteScalar()!;
        }

        public void UpdateAppMetadata(AppMetadata app, DateTimeOffset observedAt)
        {
            using var connection = OpenConnection();
            _ = GetOrCreateAppId(connection, app, observedAt);
        }

        public void EndForegroundSession(long sessionId, DateTimeOffset endedAt)
        {
            using var connection = OpenConnection();
            using var selectCommand = connection.CreateCommand();
            selectCommand.CommandText = """
                SELECT started_at
                FROM foreground_sessions
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
                UPDATE foreground_sessions
                SET ended_at = $endedAt,
                    duration_ms = $durationMs
                WHERE id = $id AND ended_at IS NULL;
                """;
            updateCommand.Parameters.AddWithValue("$endedAt", FormatTimestamp(endedAt));
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

        public long StartProcessRuntimeSession(AppMetadata app, int processId, DateTimeOffset observedAt)
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
                    last_observed_at
                )
                VALUES ($appId, $processId, $startedAt, $firstObservedAt, $lastObservedAt);
                SELECT last_insert_rowid();
                """;
            command.Parameters.AddWithValue("$appId", appId);
            command.Parameters.AddWithValue("$processId", processId);
            command.Parameters.AddWithValue("$startedAt", FormatTimestamp(observedAt));
            command.Parameters.AddWithValue("$firstObservedAt", FormatTimestamp(observedAt));
            command.Parameters.AddWithValue("$lastObservedAt", FormatTimestamp(observedAt));
            return (long)command.ExecuteScalar()!;
        }

        public void UpdateProcessRuntimeSession(long sessionId, AppMetadata app, DateTimeOffset observedAt)
        {
            using var connection = OpenConnection();
            _ = GetOrCreateAppId(connection, app, observedAt);

            using var command = connection.CreateCommand();
            command.CommandText = """
                UPDATE process_runtime_sessions
                SET last_observed_at = $lastObservedAt
                WHERE id = $id AND ended_at IS NULL;
                """;
            command.Parameters.AddWithValue("$lastObservedAt", FormatTimestamp(observedAt));
            command.Parameters.AddWithValue("$id", sessionId);
            command.ExecuteNonQuery();
        }

        public void EndProcessRuntimeSession(long sessionId, DateTimeOffset endedAt)
        {
            using var connection = OpenConnection();
            EndProcessRuntimeSession(connection, sessionId, endedAt);
        }

        public IReadOnlyList<ForegroundUsageSummary> GetForegroundUsageForDay(DateTimeOffset now)
        {
            var localDayStart = now.ToLocalTime().Date;
            var dayStart = new DateTimeOffset(localDayStart, TimeZoneInfo.Local.GetUtcOffset(localDayStart));
            var dayEnd = dayStart.AddDays(1);

            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT
                    a.display_name,
                    a.executable_path,
                    fs.started_at,
                    fs.ended_at
                FROM foreground_sessions fs
                INNER JOIN apps a ON a.id = fs.app_id
                WHERE fs.started_at < $dayEnd
                  AND COALESCE(fs.ended_at, $now) > $dayStart;
                """;
            command.Parameters.AddWithValue("$dayStart", FormatTimestamp(dayStart));
            command.Parameters.AddWithValue("$dayEnd", FormatTimestamp(dayEnd));
            command.Parameters.AddWithValue("$now", FormatTimestamp(now));

            var totals = new Dictionary<string, UsageAggregation>(StringComparer.OrdinalIgnoreCase);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var appName = reader.GetString(0);
                var executablePath = reader.IsDBNull(1) ? null : reader.GetString(1);
                var startedAt = ParseTimestamp(reader.GetString(2));
                var endedAt = reader.IsDBNull(3) ? now : ParseTimestamp(reader.GetString(3));
                var effectiveStart = Max(startedAt, dayStart);
                var effectiveEnd = Min(endedAt, dayEnd);
                var durationMs = Math.Max(0, (long)(effectiveEnd - effectiveStart).TotalMilliseconds);
                if (durationMs <= 0)
                    continue;

                if (!totals.TryGetValue(appName, out var aggregation))
                {
                    aggregation = new UsageAggregation(effectiveStart, effectiveEnd, executablePath);
                    totals[appName] = aggregation;
                }

                aggregation.ExecutablePath ??= executablePath;
                aggregation.ActiveUsageMs += durationMs;
                aggregation.SwitchCount++;
                aggregation.FirstStartedAt = Min(aggregation.FirstStartedAt, effectiveStart);
                aggregation.LastObservedAt = Max(aggregation.LastObservedAt, effectiveEnd);
            }

            return totals
                .Select(x => new ForegroundUsageSummary(
                    x.Key,
                    x.Value.ExecutablePath,
                    x.Value.ActiveUsageMs,
                    x.Value.SwitchCount,
                    x.Value.FirstStartedAt,
                    x.Value.LastObservedAt))
                .OrderByDescending(x => x.ActiveUsageMs)
                .ToList();
        }

        public IReadOnlyList<ActivityTimelineRow> GetActivityTimelineForDay(DateTimeOffset now)
        {
            var localDayStart = now.ToLocalTime().Date;
            var dayStart = new DateTimeOffset(localDayStart, TimeZoneInfo.Local.GetUtcOffset(localDayStart));
            var dayEnd = dayStart.AddDays(1);
            var rows = new List<ActivityTimelineRow>();

            using var connection = OpenConnection();
            AddForegroundTimelineRows(connection, rows, dayStart, dayEnd, now);
            AddIdleTimelineRows(connection, rows, dayStart, dayEnd, now);

            return rows
                .OrderBy(x => x.StartedAt)
                .ToList();
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

        private void MarkUnexpectedRuntimeSessions(DateTimeOffset now)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT id, started_at, last_heartbeat_at
                FROM app_runtime_sessions
                WHERE ended_at IS NULL;
                """;

            var openSessions = new List<(long Id, DateTimeOffset StartedAt, DateTimeOffset EndedAt)>();
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var startedAt = DateTimeOffset.Parse(reader.GetString(1), null, System.Globalization.DateTimeStyles.RoundtripKind);
                    var endedAt = reader.IsDBNull(2)
                        ? now
                        : DateTimeOffset.Parse(reader.GetString(2), null, System.Globalization.DateTimeStyles.RoundtripKind);
                    openSessions.Add((reader.GetInt64(0), startedAt, endedAt));
                }
            }

            foreach (var session in openSessions)
            {
                EndRuntimeSession(connection, session.Id, session.EndedAt, "unexpected");
            }
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

        private void EndProcessRuntimeSession(SqliteConnection connection, long sessionId, DateTimeOffset endedAt)
        {
            using var selectCommand = connection.CreateCommand();
            selectCommand.CommandText = """
                SELECT started_at
                FROM process_runtime_sessions
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
                UPDATE process_runtime_sessions
                SET ended_at = $endedAt,
                    duration_ms = $durationMs,
                    last_observed_at = $endedAt
                WHERE id = $id AND ended_at IS NULL;
                """;
            updateCommand.Parameters.AddWithValue("$endedAt", FormatTimestamp(endedAt));
            updateCommand.Parameters.AddWithValue("$durationMs", durationMs);
            updateCommand.Parameters.AddWithValue("$id", sessionId);
            updateCommand.ExecuteNonQuery();
        }

        private long GetOrCreateAppId(SqliteConnection connection, AppMetadata app, DateTimeOffset observedAt)
        {
            using var insertCommand = connection.CreateCommand();
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
                    a.display_name,
                    a.executable_path,
                    fs.started_at,
                    fs.ended_at
                FROM foreground_sessions fs
                INNER JOIN apps a ON a.id = fs.app_id
                WHERE fs.started_at < $dayEnd
                  AND COALESCE(fs.ended_at, $now) > $dayStart;
                """;
            command.Parameters.AddWithValue("$dayStart", FormatTimestamp(dayStart));
            command.Parameters.AddWithValue("$dayEnd", FormatTimestamp(dayEnd));
            command.Parameters.AddWithValue("$now", FormatTimestamp(now));

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var appName = reader.GetString(0);
                var executablePath = reader.IsDBNull(1) ? null : reader.GetString(1);
                var startedAt = ParseTimestamp(reader.GetString(2));
                DateTimeOffset? endedAt = reader.IsDBNull(3) ? null : ParseTimestamp(reader.GetString(3));
                var effectiveStart = Max(startedAt, dayStart);
                var effectiveEnd = Min(endedAt ?? now, dayEnd);
                AddTimelineRow(rows, "활성", effectiveStart, endedAt, effectiveEnd, appName, executablePath);
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
                    COALESCE(a.display_name, 'Idle'),
                    a.executable_path,
                    i.started_at,
                    i.ended_at
                FROM idle_sessions i
                LEFT JOIN apps a ON a.id = i.foreground_app_id
                WHERE i.started_at < $dayEnd
                  AND COALESCE(i.ended_at, $now) > $dayStart;
                """;
            command.Parameters.AddWithValue("$dayStart", FormatTimestamp(dayStart));
            command.Parameters.AddWithValue("$dayEnd", FormatTimestamp(dayEnd));
            command.Parameters.AddWithValue("$now", FormatTimestamp(now));

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var foregroundAppName = reader.GetString(0);
                var executablePath = reader.IsDBNull(1) ? null : reader.GetString(1);
                var startedAt = ParseTimestamp(reader.GetString(2));
                DateTimeOffset? endedAt = reader.IsDBNull(3) ? null : ParseTimestamp(reader.GetString(3));
                var effectiveStart = Max(startedAt, dayStart);
                var effectiveEnd = Min(endedAt ?? now, dayEnd);
                AddTimelineRow(rows, "유휴", effectiveStart, endedAt, effectiveEnd, foregroundAppName, executablePath);
            }
        }

        private static void AddTimelineRow(
            List<ActivityTimelineRow> rows,
            string activityType,
            DateTimeOffset effectiveStart,
            DateTimeOffset? originalEnd,
            DateTimeOffset effectiveEnd,
            string displayName,
            string? executablePath)
        {
            var durationMs = Math.Max(0, (long)(effectiveEnd - effectiveStart).TotalMilliseconds);
            if (durationMs <= 0)
                return;

            rows.Add(new ActivityTimelineRow(
                activityType,
                effectiveStart,
                originalEnd,
                durationMs,
                displayName,
                executablePath));
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

        private sealed class UsageAggregation
        {
            public UsageAggregation(DateTimeOffset firstStartedAt, DateTimeOffset lastObservedAt, string? executablePath)
            {
                FirstStartedAt = firstStartedAt;
                LastObservedAt = lastObservedAt;
                ExecutablePath = executablePath;
            }

            public long ActiveUsageMs { get; set; }

            public string? ExecutablePath { get; set; }

            public int SwitchCount { get; set; }

            public DateTimeOffset FirstStartedAt { get; set; }

            public DateTimeOffset LastObservedAt { get; set; }
        }
    }
}
