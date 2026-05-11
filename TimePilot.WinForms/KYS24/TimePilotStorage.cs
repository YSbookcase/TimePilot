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

                CREATE INDEX IF NOT EXISTS idx_foreground_sessions_started_at
                    ON foreground_sessions(started_at);

                CREATE INDEX IF NOT EXISTS idx_idle_sessions_started_at
                    ON idle_sessions(started_at);

                CREATE INDEX IF NOT EXISTS idx_app_runtime_sessions_started_at
                    ON app_runtime_sessions(started_at);
                """;
            command.ExecuteNonQuery();

            MarkUnexpectedRuntimeSessions(now);
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

        public long StartForegroundSession(string processName, DateTimeOffset startedAt)
        {
            using var connection = OpenConnection();
            var appId = GetOrCreateAppId(connection, processName, startedAt);

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

        public long StartIdleSession(DateTimeOffset startedAt, int thresholdMs, string? foregroundProcessName)
        {
            using var connection = OpenConnection();
            long? foregroundAppId = string.IsNullOrWhiteSpace(foregroundProcessName)
                ? null
                : GetOrCreateAppId(connection, foregroundProcessName, startedAt);

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
                var startedAt = ParseTimestamp(reader.GetString(1));
                var endedAt = reader.IsDBNull(2) ? now : ParseTimestamp(reader.GetString(2));
                var effectiveStart = Max(startedAt, dayStart);
                var effectiveEnd = Min(endedAt, dayEnd);
                var durationMs = Math.Max(0, (long)(effectiveEnd - effectiveStart).TotalMilliseconds);
                if (durationMs <= 0)
                    continue;

                if (!totals.TryGetValue(appName, out var aggregation))
                {
                    aggregation = new UsageAggregation(effectiveStart, effectiveEnd);
                    totals[appName] = aggregation;
                }

                aggregation.ActiveUsageMs += durationMs;
                aggregation.FirstStartedAt = Min(aggregation.FirstStartedAt, effectiveStart);
                aggregation.LastObservedAt = Max(aggregation.LastObservedAt, effectiveEnd);
            }

            return totals
                .Select(x => new ForegroundUsageSummary(
                    x.Key,
                    x.Value.ActiveUsageMs,
                    x.Value.FirstStartedAt,
                    x.Value.LastObservedAt))
                .OrderByDescending(x => x.ActiveUsageMs)
                .ToList();
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
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

        private long GetOrCreateAppId(SqliteConnection connection, string processName, DateTimeOffset observedAt)
        {
            using var insertCommand = connection.CreateCommand();
            insertCommand.CommandText = """
                INSERT INTO apps (
                    process_name,
                    display_name,
                    first_seen_at,
                    last_seen_at
                )
                VALUES ($processName, $displayName, $firstSeenAt, $lastSeenAt)
                ON CONFLICT(process_name) DO UPDATE SET
                    last_seen_at = excluded.last_seen_at;
                """;
            insertCommand.Parameters.AddWithValue("$processName", processName);
            insertCommand.Parameters.AddWithValue("$displayName", processName);
            insertCommand.Parameters.AddWithValue("$firstSeenAt", FormatTimestamp(observedAt));
            insertCommand.Parameters.AddWithValue("$lastSeenAt", FormatTimestamp(observedAt));
            insertCommand.ExecuteNonQuery();

            using var selectCommand = connection.CreateCommand();
            selectCommand.CommandText = """
                SELECT id
                FROM apps
                WHERE process_name = $processName;
                """;
            selectCommand.Parameters.AddWithValue("$processName", processName);
            return (long)selectCommand.ExecuteScalar()!;
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
            public UsageAggregation(DateTimeOffset firstStartedAt, DateTimeOffset lastObservedAt)
            {
                FirstStartedAt = firstStartedAt;
                LastObservedAt = lastObservedAt;
            }

            public long ActiveUsageMs { get; set; }

            public DateTimeOffset FirstStartedAt { get; set; }

            public DateTimeOffset LastObservedAt { get; set; }
        }
    }
}
