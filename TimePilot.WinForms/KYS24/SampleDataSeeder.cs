using System.Globalization;
using Microsoft.Data.Sqlite;

namespace TimePilot.WinForms.KYS24
{
    internal static class SampleDataSeeder
    {
        private const string SampleAppVersion = "sample-data";
        private const string SampleProcessPrefix = "timepilot_sample_";

        private static readonly SampleApp[] SampleApps =
        [
            new("Sample Browser", $"{SampleProcessPrefix}browser"),
            new("Sample Editor", $"{SampleProcessPrefix}editor"),
            new("Sample IDE", $"{SampleProcessPrefix}ide"),
            new("Sample Chat", $"{SampleProcessPrefix}chat"),
            new("Sample Design Tool", $"{SampleProcessPrefix}design")
        ];

        public static void SeedDefault()
        {
            var now = DateTimeOffset.UtcNow;
            var systemBootedAt = now - TimeSpan.FromMilliseconds(Environment.TickCount64);

            using (var storage = TimePilotStorage.CreateDefault())
                storage.Initialize(now, systemBootedAt);

            Directory.CreateDirectory(AppDataPaths.DataDirectory);
            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();

            DeleteExistingSampleData(connection, transaction);
            var appIds = InsertSampleApps(connection, transaction, now);
            InsertSampleRuntimeSessions(connection, transaction, now);
            InsertSampleForegroundSessions(connection, transaction, appIds, now.ToLocalTime());
            InsertSampleProcessRuntimeSessions(connection, transaction, appIds, now.ToLocalTime());

            transaction.Commit();
        }

        public static void Clear()
        {
            var now = DateTimeOffset.UtcNow;
            var systemBootedAt = now - TimeSpan.FromMilliseconds(Environment.TickCount64);

            using (var storage = TimePilotStorage.CreateDefault())
                storage.Initialize(now, systemBootedAt);

            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();
            DeleteExistingSampleData(connection, transaction);
            transaction.Commit();
        }

        public static string GetStatusText()
        {
            if (!File.Exists(AppDataPaths.DatabasePath))
                return "TimePilot database was not found.";

            using var connection = OpenConnection();
            var appCount = CountRows(
                connection,
                "SELECT COUNT(*) FROM apps WHERE process_name LIKE $sampleProcessPrefix;",
                command => command.Parameters.AddWithValue("$sampleProcessPrefix", $"{SampleProcessPrefix}%"));
            var foregroundSessionCount = CountRows(
                connection,
                """
                SELECT COUNT(*)
                FROM foreground_sessions
                WHERE app_id IN (
                    SELECT id FROM apps WHERE process_name LIKE $sampleProcessPrefix
                );
                """,
                command => command.Parameters.AddWithValue("$sampleProcessPrefix", $"{SampleProcessPrefix}%"));
            var runtimeSessionCount = CountRows(
                connection,
                "SELECT COUNT(*) FROM app_runtime_sessions WHERE app_version = $sampleAppVersion;",
                command => command.Parameters.AddWithValue("$sampleAppVersion", SampleAppVersion));
            var processRuntimeSessionCount = CountRows(
                connection,
                """
                SELECT COUNT(*)
                FROM process_runtime_sessions
                WHERE app_id IN (
                    SELECT id FROM apps WHERE process_name LIKE $sampleProcessPrefix
                );
                """,
                command => command.Parameters.AddWithValue("$sampleProcessPrefix", $"{SampleProcessPrefix}%"));

            return string.Create(
                CultureInfo.InvariantCulture,
                $"Sample data status: apps={appCount}, foreground_sessions={foregroundSessionCount}, app_runtime_sessions={runtimeSessionCount}, process_runtime_sessions={processRuntimeSessionCount}");
        }

        private static SqliteConnection OpenConnection()
        {
            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = AppDataPaths.DatabasePath
            }.ToString();
            var connection = new SqliteConnection(connectionString);
            connection.Open();
            return connection;
        }

        private static void DeleteExistingSampleData(SqliteConnection connection, SqliteTransaction transaction)
        {
            ExecuteNonQuery(
                connection,
                transaction,
                """
                DELETE FROM foreground_sessions
                WHERE app_id IN (
                    SELECT id FROM apps WHERE process_name LIKE $sampleProcessPrefix
                );

                DELETE FROM idle_sessions
                WHERE foreground_app_id IN (
                    SELECT id FROM apps WHERE process_name LIKE $sampleProcessPrefix
                );

                DELETE FROM process_runtime_sessions
                WHERE app_id IN (
                    SELECT id FROM apps WHERE process_name LIKE $sampleProcessPrefix
                );

                DELETE FROM apps
                WHERE process_name LIKE $sampleProcessPrefix;

                DELETE FROM app_runtime_sessions
                WHERE app_version = $sampleAppVersion;
                """,
                command =>
                {
                    command.Parameters.AddWithValue("$sampleProcessPrefix", $"{SampleProcessPrefix}%");
                    command.Parameters.AddWithValue("$sampleAppVersion", SampleAppVersion);
                });
        }

        private static Dictionary<string, long> InsertSampleApps(
            SqliteConnection connection,
            SqliteTransaction transaction,
            DateTimeOffset now)
        {
            var appIds = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            foreach (var app in SampleApps)
            {
                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = """
                    INSERT INTO apps (
                        process_name,
                        display_name,
                        executable_path,
                        first_seen_at,
                        last_seen_at
                    )
                    VALUES ($processName, $displayName, $executablePath, $firstSeenAt, $lastSeenAt);
                    SELECT last_insert_rowid();
                    """;
                command.Parameters.AddWithValue("$processName", app.ProcessName);
                command.Parameters.AddWithValue("$displayName", app.DisplayName);
                command.Parameters.AddWithValue("$executablePath", DBNull.Value);
                command.Parameters.AddWithValue("$firstSeenAt", FormatTimestamp(now.AddMonths(-3)));
                command.Parameters.AddWithValue("$lastSeenAt", FormatTimestamp(now));
                appIds[app.ProcessName] = (long)command.ExecuteScalar()!;
            }

            return appIds;
        }

        private static void InsertSampleRuntimeSessions(
            SqliteConnection connection,
            SqliteTransaction transaction,
            DateTimeOffset now)
        {
            var localNow = now.ToLocalTime();
            var today = localNow.Date;
            var weekStart = today.AddDays(-GetDaysSinceMonday(today.DayOfWeek));
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var yearStart = new DateTime(today.Year, 1, 1);

            AddRuntimeSession(connection, transaction, LocalAt(today, 8, 45), LocalAt(today, 18, 20), now);
            AddRuntimeSession(connection, transaction, LocalAt(PreviousDateInRange(today, weekStart, 1), 9, 10), LocalAt(PreviousDateInRange(today, weekStart, 1), 17, 30), now);
            AddRuntimeSession(connection, transaction, LocalAt(PreviousDateInRange(today, monthStart, 8), 10, 0), LocalAt(PreviousDateInRange(today, monthStart, 8), 16, 40), now);
            AddRuntimeSession(connection, transaction, LocalAt(PreviousDateInRange(today, yearStart, 60), 13, 0), LocalAt(PreviousDateInRange(today, yearStart, 60), 19, 10), now);
        }

        private static void InsertSampleForegroundSessions(
            SqliteConnection connection,
            SqliteTransaction transaction,
            IReadOnlyDictionary<string, long> appIds,
            DateTimeOffset localNow)
        {
            var today = localNow.Date;
            var weekStart = today.AddDays(-GetDaysSinceMonday(today.DayOfWeek));
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var yearStart = new DateTime(today.Year, 1, 1);

            AddForegroundSession(connection, transaction, appIds[$"{SampleProcessPrefix}browser"], localNow.AddHours(-4), localNow.AddHours(-2.5), localNow);
            AddForegroundSession(connection, transaction, appIds[$"{SampleProcessPrefix}editor"], localNow.AddHours(-2.4), localNow.AddHours(-1.1), localNow);
            AddForegroundSession(connection, transaction, appIds[$"{SampleProcessPrefix}chat"], localNow.AddMinutes(-50), localNow.AddMinutes(-20), localNow);

            var weekDate = PreviousDateInRange(today, weekStart, 1);
            AddForegroundSession(connection, transaction, appIds[$"{SampleProcessPrefix}ide"], LocalAt(weekDate, 9, 30), LocalAt(weekDate, 12, 20), localNow);
            AddForegroundSession(connection, transaction, appIds[$"{SampleProcessPrefix}editor"], LocalAt(weekDate, 14, 0), LocalAt(weekDate, 17, 15), localNow);
            AddForegroundSession(connection, transaction, appIds[$"{SampleProcessPrefix}browser"], LocalAt(weekDate, 17, 20), LocalAt(weekDate, 18, 5), localNow);

            var monthDate = PreviousDateInRange(today, monthStart, 8);
            AddForegroundSession(connection, transaction, appIds[$"{SampleProcessPrefix}design"], LocalAt(monthDate, 10, 10), LocalAt(monthDate, 14, 45), localNow);
            AddForegroundSession(connection, transaction, appIds[$"{SampleProcessPrefix}browser"], LocalAt(monthDate, 15, 0), LocalAt(monthDate, 16, 20), localNow);

            var yearDate = PreviousDateInRange(today, yearStart, 60);
            AddForegroundSession(connection, transaction, appIds[$"{SampleProcessPrefix}ide"], LocalAt(yearDate, 13, 0), LocalAt(yearDate, 15, 30), localNow);
            AddForegroundSession(connection, transaction, appIds[$"{SampleProcessPrefix}design"], LocalAt(yearDate, 15, 40), LocalAt(yearDate, 18, 25), localNow);
        }

        private static void InsertSampleProcessRuntimeSessions(
            SqliteConnection connection,
            SqliteTransaction transaction,
            IReadOnlyDictionary<string, long> appIds,
            DateTimeOffset localNow)
        {
            var today = localNow.Date;
            var weekStart = today.AddDays(-GetDaysSinceMonday(today.DayOfWeek));
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var yearStart = new DateTime(today.Year, 1, 1);

            AddProcessRuntimeSession(connection, transaction, appIds[$"{SampleProcessPrefix}browser"], 9101, LocalAt(today, 8, 50), LocalAt(today, 18, 10), true, true, localNow);
            AddProcessRuntimeSession(connection, transaction, appIds[$"{SampleProcessPrefix}editor"], 9102, LocalAt(today, 9, 20), LocalAt(today, 17, 40), true, true, localNow);
            AddProcessRuntimeSession(connection, transaction, appIds[$"{SampleProcessPrefix}chat"], 9103, LocalAt(today, 10, 0), LocalAt(today, 18, 0), true, true, localNow);

            var weekDate = PreviousDateInRange(today, weekStart, 1);
            AddProcessRuntimeSession(connection, transaction, appIds[$"{SampleProcessPrefix}ide"], 9201, LocalAt(weekDate, 9, 5), LocalAt(weekDate, 17, 40), true, true, localNow);
            AddProcessRuntimeSession(connection, transaction, appIds[$"{SampleProcessPrefix}editor"], 9202, LocalAt(weekDate, 13, 45), LocalAt(weekDate, 17, 30), true, true, localNow);

            var monthDate = PreviousDateInRange(today, monthStart, 8);
            AddProcessRuntimeSession(connection, transaction, appIds[$"{SampleProcessPrefix}design"], 9301, LocalAt(monthDate, 10, 0), LocalAt(monthDate, 16, 50), true, true, localNow);
            AddProcessRuntimeSession(connection, transaction, appIds[$"{SampleProcessPrefix}browser"], 9302, LocalAt(monthDate, 14, 40), LocalAt(monthDate, 16, 30), true, true, localNow);

            var yearDate = PreviousDateInRange(today, yearStart, 60);
            AddProcessRuntimeSession(connection, transaction, appIds[$"{SampleProcessPrefix}ide"], 9401, LocalAt(yearDate, 12, 50), LocalAt(yearDate, 15, 45), true, true, localNow);
            AddProcessRuntimeSession(connection, transaction, appIds[$"{SampleProcessPrefix}design"], 9402, LocalAt(yearDate, 15, 20), LocalAt(yearDate, 18, 40), true, true, localNow);
        }

        private static void AddRuntimeSession(
            SqliteConnection connection,
            SqliteTransaction transaction,
            DateTimeOffset start,
            DateTimeOffset end,
            DateTimeOffset now)
        {
            var effectiveEnd = end > now ? now : end;
            if (effectiveEnd <= start)
                return;

            ExecuteNonQuery(
                connection,
                transaction,
                """
                INSERT INTO app_runtime_sessions (
                    started_at,
                    ended_at,
                    duration_ms,
                    last_heartbeat_at,
                    shutdown_reason,
                    system_booted_at,
                    app_version
                )
                VALUES (
                    $startedAt,
                    $endedAt,
                    $durationMs,
                    $lastHeartbeatAt,
                    $shutdownReason,
                    $systemBootedAt,
                    $appVersion
                );
                """,
                command =>
                {
                    command.Parameters.AddWithValue("$startedAt", FormatTimestamp(start));
                    command.Parameters.AddWithValue("$endedAt", FormatTimestamp(effectiveEnd));
                    command.Parameters.AddWithValue("$durationMs", (long)(effectiveEnd - start).TotalMilliseconds);
                    command.Parameters.AddWithValue("$lastHeartbeatAt", FormatTimestamp(effectiveEnd));
                    command.Parameters.AddWithValue("$shutdownReason", "sample");
                    command.Parameters.AddWithValue("$systemBootedAt", FormatTimestamp(start.AddMinutes(-20)));
                    command.Parameters.AddWithValue("$appVersion", SampleAppVersion);
                });
        }

        private static void AddForegroundSession(
            SqliteConnection connection,
            SqliteTransaction transaction,
            long appId,
            DateTimeOffset start,
            DateTimeOffset end,
            DateTimeOffset localNow)
        {
            var effectiveEnd = end > localNow ? localNow : end;
            if (effectiveEnd <= start)
                return;

            ExecuteNonQuery(
                connection,
                transaction,
                """
                INSERT INTO foreground_sessions (
                    app_id,
                    started_at,
                    ended_at,
                    duration_ms,
                    last_observed_at
                )
                VALUES (
                    $appId,
                    $startedAt,
                    $endedAt,
                    $durationMs,
                    $lastObservedAt
                );
                """,
                command =>
                {
                    command.Parameters.AddWithValue("$appId", appId);
                    command.Parameters.AddWithValue("$startedAt", FormatTimestamp(start));
                    command.Parameters.AddWithValue("$endedAt", FormatTimestamp(effectiveEnd));
                    command.Parameters.AddWithValue("$durationMs", (long)(effectiveEnd - start).TotalMilliseconds);
                    command.Parameters.AddWithValue("$lastObservedAt", FormatTimestamp(effectiveEnd));
                });
        }

        private static void AddProcessRuntimeSession(
            SqliteConnection connection,
            SqliteTransaction transaction,
            long appId,
            int processId,
            DateTimeOffset start,
            DateTimeOffset end,
            bool hasMainWindow,
            bool isCurrentSessionProcess,
            DateTimeOffset localNow)
        {
            var effectiveEnd = end > localNow ? localNow : end;
            if (effectiveEnd <= start)
                return;

            ExecuteNonQuery(
                connection,
                transaction,
                """
                INSERT INTO process_runtime_sessions (
                    app_id,
                    process_id,
                    started_at,
                    ended_at,
                    duration_ms,
                    first_observed_at,
                    last_observed_at,
                    tracking_scope,
                    has_main_window,
                    is_current_session_process
                )
                VALUES (
                    $appId,
                    $processId,
                    $startedAt,
                    $endedAt,
                    $durationMs,
                    $firstObservedAt,
                    $lastObservedAt,
                    $trackingScope,
                    $hasMainWindow,
                    $isCurrentSessionProcess
                );
                """,
                command =>
                {
                    command.Parameters.AddWithValue("$appId", appId);
                    command.Parameters.AddWithValue("$processId", processId);
                    command.Parameters.AddWithValue("$startedAt", FormatTimestamp(start));
                    command.Parameters.AddWithValue("$endedAt", FormatTimestamp(effectiveEnd));
                    command.Parameters.AddWithValue("$durationMs", (long)(effectiveEnd - start).TotalMilliseconds);
                    command.Parameters.AddWithValue("$firstObservedAt", FormatTimestamp(start));
                    command.Parameters.AddWithValue("$lastObservedAt", FormatTimestamp(effectiveEnd));
                    command.Parameters.AddWithValue("$trackingScope", (int)ProcessRuntimeTrackingScope.AllProcesses);
                    command.Parameters.AddWithValue("$hasMainWindow", hasMainWindow ? 1 : 0);
                    command.Parameters.AddWithValue("$isCurrentSessionProcess", isCurrentSessionProcess ? 1 : 0);
                });
        }

        private static void ExecuteNonQuery(
            SqliteConnection connection,
            SqliteTransaction transaction,
            string commandText,
            Action<SqliteCommand>? configure = null)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = commandText;
            configure?.Invoke(command);
            command.ExecuteNonQuery();
        }

        private static long CountRows(
            SqliteConnection connection,
            string commandText,
            Action<SqliteCommand>? configure = null)
        {
            using var command = connection.CreateCommand();
            command.CommandText = commandText;
            configure?.Invoke(command);
            return (long)command.ExecuteScalar()!;
        }

        private static DateTime PreviousDateInRange(DateTime today, DateTime rangeStart, int preferredDaysAgo)
        {
            var date = today.AddDays(-preferredDaysAgo);
            if (date >= rangeStart && date < today)
                return date;

            if (rangeStart < today)
                return rangeStart;

            return today;
        }

        private static DateTimeOffset LocalAt(DateTime date, int hour, int minute)
        {
            var localDateTime = date.Date.AddHours(hour).AddMinutes(minute);
            return new DateTimeOffset(localDateTime, TimeZoneInfo.Local.GetUtcOffset(localDateTime));
        }

        private static int GetDaysSinceMonday(DayOfWeek dayOfWeek)
        {
            return ((int)dayOfWeek + 6) % 7;
        }

        private static string FormatTimestamp(DateTimeOffset timestamp)
        {
            return timestamp.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
        }

        private sealed record SampleApp(string DisplayName, string ProcessName);
    }
}
