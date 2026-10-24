using System.Globalization;
using Microsoft.Data.Sqlite;
using TimePilot.Analytics.Contracts;

namespace TimePilot.Analytics.Sqlite;

/// <summary>A Windows, offline rollback-journal reader. Dispose promptly to release the source lease.</summary>
public sealed class SqliteDailyAnalyticsReader : IDailyAnalyticsReader
{
    public const int MaximumRowsPerQuery = 250_000;
    private readonly FileStream sourceLease;
    private readonly SqliteConnection connection;
    private readonly object gate = new();
    private readonly DateTimeOffset sourceCapturedAt;
    private bool disposed;

    public DateTimeOffset SourceCapturedAt => sourceCapturedAt;

    private SqliteDailyAnalyticsReader(FileStream sourceLease, SqliteConnection connection, DateTimeOffset sourceCapturedAt)
    {
        this.sourceLease = sourceLease;
        this.connection = connection;
        this.sourceCapturedAt = sourceCapturedAt.ToUniversalTime();
    }

    /// <summary>Open an imported offline snapshot with its trusted capture timestamp, not the current time.</summary>
    public static IDailyAnalyticsReader Open(string databasePath, DateTimeOffset sourceCapturedAt)
    {
        if (sourceCapturedAt == default || sourceCapturedAt == DateTimeOffset.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(sourceCapturedAt), "A known snapshot capture timestamp is required.");
        return OpenCore(databasePath, sourceCapturedAt);
    }

    internal static IDailyAnalyticsReader OpenForPreparation(string databasePath) =>
        OpenCore(databasePath, DateTimeOffset.MaxValue);

    private static SqliteDailyAnalyticsReader OpenCore(string databasePath, DateTimeOffset sourceCapturedAt)
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("The source write-exclusion lease requires Windows.");
        if (string.IsNullOrWhiteSpace(databasePath) || !Path.IsPathFullyQualified(databasePath))
            throw new ArgumentException("An explicit absolute existing database path is required.", nameof(databasePath));

        FileStream? lease = null;
        SqliteConnection? db = null;
        try
        {
            var path = Path.GetFullPath(databasePath);
            // Deny existing and new write handles, including journal-mode switches, while reading.
            lease = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            Span<byte> header = stackalloc byte[100];
            if (lease.Read(header) != header.Length || !header[..16].SequenceEqual("SQLite format 3\0"u8))
                throw new AnalyticsReadException(AnalyticsError.InvalidData, "Not a supported SQLite database.");
            if (header[18] != 1 || header[19] != 1
                || File.Exists(path + "-wal") || File.Exists(path + "-shm") || File.Exists(path + "-journal"))
                throw new AnalyticsReadException(AnalyticsError.UnsupportedJournal,
                    "Use an offline rollback-journal snapshot without WAL, SHM or journal sidecars. The reader never checkpoints or converts the source.");

            db = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = path,
                Mode = SqliteOpenMode.ReadOnly,
                Cache = SqliteCacheMode.Private,
                Pooling = false,
                DefaultTimeout = 2
            }.ToString());
            db.Open();
            using (var command = db.CreateCommand())
            {
                command.CommandText = "PRAGMA query_only=ON; PRAGMA trusted_schema=OFF;";
                command.ExecuteNonQuery();
            }
            ValidateSchema(db);
            return new SqliteDailyAnalyticsReader(lease, db, sourceCapturedAt);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SqliteException)
        {
            db?.Dispose();
            lease?.Dispose();
            throw new AnalyticsReadException(ex is SqliteException ? AnalyticsError.InvalidData : AnalyticsError.SourceUnavailable,
                "The explicit source cannot be read. No source is created, repaired or substituted.", ex);
        }
        catch
        {
            db?.Dispose();
            lease?.Dispose();
            throw;
        }
    }

    private static void ValidateSchema(SqliteConnection db)
    {
        using var version = db.CreateCommand();
        version.CommandText = "PRAGMA user_version;";
        if (Convert.ToInt64(version.ExecuteScalar(), CultureInfo.InvariantCulture) != 0)
            throw new AnalyticsReadException(AnalyticsError.UnsupportedSchema, "Only the unversioned Community daily-v1 schema profile is supported.");

        var required = new Dictionary<string, string[]>
        {
            ["apps"] = ["id", "display_name", "process_name", "user_alias", "primary_category_id"],
            ["app_categories"] = ["id", "name"],
            ["foreground_sessions"] = ["app_id", "started_at", "ended_at", "last_observed_at"],
            ["idle_sessions"] = ["started_at", "ended_at"],
            ["app_runtime_sessions"] = ["started_at", "ended_at", "last_heartbeat_at"]
        };
        foreach (var (table, columns) in required)
        {
            using var kind = db.CreateCommand();
            kind.CommandText = "SELECT type FROM sqlite_schema WHERE name=$name;";
            kind.Parameters.AddWithValue("$name", table);
            if (!Equals(kind.ExecuteScalar(), "table"))
                throw new AnalyticsReadException(AnalyticsError.UnsupportedSchema, $"Required table missing: {table}.");
            using var command = db.CreateCommand();
            command.CommandText = $"PRAGMA table_info({table});";
            using var reader = command.ExecuteReader();
            var found = new HashSet<string>(StringComparer.Ordinal);
            while (reader.Read())
                found.Add(reader.GetString(1));
            if (columns.Any(column => !found.Contains(column)))
                throw new AnalyticsReadException(AnalyticsError.UnsupportedSchema, $"Required columns missing: {table}.");
        }
    }

    public DailySnapshot Read(DailyQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(query.TimeZone);
        var count = query.EndDateExclusive.DayNumber - query.StartDate.DayNumber;
        if (count is < 1 or > AnalyticsContract.MaximumDays)
            throw new ArgumentOutOfRangeException(nameof(query), "Request between 1 and 366 calendar days.");
        if (query.ObservedAt > sourceCapturedAt)
            throw new ArgumentOutOfRangeException(nameof(query), "ObservedAt must not exceed the snapshot capture timestamp.");
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                using var transaction = connection.BeginTransaction(deferred: true);
                var start = Boundary(query.StartDate, query.TimeZone);
                var end = Min(Boundary(query.EndDateExclusive, query.TimeZone), query.ObservedAt);
                var foreground = ReadSpans(transaction, """
                    SELECT f.started_at, COALESCE(f.ended_at, f.last_observed_at, f.started_at) AS effective_end,
                           a.id, COALESCE(NULLIF(TRIM(a.user_alias), ''), NULLIF(TRIM(a.display_name), ''), a.process_name),
                           a.primary_category_id, c.name
                    FROM foreground_sessions f JOIN apps a ON a.id=f.app_id
                    LEFT JOIN app_categories c ON c.id=a.primary_category_id
                    """, start, end, cancellationToken);
                var idle = ReadSpans(transaction,
                    "SELECT started_at, ended_at AS effective_end FROM idle_sessions", start, end, cancellationToken);
                var runtime = ReadSpans(transaction,
                    "SELECT started_at, COALESCE(ended_at, last_heartbeat_at, started_at) AS effective_end FROM app_runtime_sessions",
                    start, end, cancellationToken);
                var days = new List<DailyActivity>();
                for (var date = query.StartDate; date < query.EndDateExclusive; date = date.AddDays(1))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var dayStart = Boundary(date, query.TimeZone);
                    var dayEnd = Boundary(date.AddDays(1), query.TimeZone);
                    var observedEnd = Min(dayEnd, query.ObservedAt);
                    var apps = foreground.GroupBy(span => (span.AppId, span.Name, span.CategoryId))
                        .Select(group => new AppActivity(group.Key.AppId!.Value, group.Key.Name!, group.Key.CategoryId,
                            group.Sum(span => Milliseconds(span, dayStart, observedEnd))))
                        .Where(app => app.ActiveMilliseconds > 0)
                        .OrderByDescending(app => app.ActiveMilliseconds).ThenBy(app => app.AppId).ToArray();
                    var categories = apps.GroupBy(app => app.CategoryId)
                        .Select(group => new CategoryActivity(group.Key,
                            foreground.First(span => span.CategoryId == group.Key).CategoryName,
                            group.Sum(app => app.ActiveMilliseconds)))
                        .OrderByDescending(category => category.ActiveMilliseconds).ThenBy(category => category.CategoryId).ToArray();
                    days.Add(new DailyActivity(date, dayStart, dayEnd, apps.Sum(app => app.ActiveMilliseconds),
                        UnionMilliseconds(idle, dayStart, observedEnd), apps.FirstOrDefault(),
                        Array.AsReadOnly(apps), Array.AsReadOnly(categories),
                        new RecorderCalendarCoverage(Math.Max(0, (long)(observedEnd - dayStart).TotalMilliseconds),
                            UnionMilliseconds(runtime, dayStart, observedEnd))));
                }
                return new DailySnapshot(AnalyticsContract.Version, AnalyticsContract.SchemaProfile, sourceCapturedAt, query, days.AsReadOnly());
            }
            catch (SqliteException ex)
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw new AnalyticsReadException(AnalyticsError.InvalidData, "The daily source query failed without modifying the source.", ex);
            }
            catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException)
            {
                throw new AnalyticsReadException(AnalyticsError.InvalidData, "A source value has an invalid type or range.", ex);
            }
        }
    }

    private List<SpanRow> ReadSpans(SqliteTransaction transaction, string sql, DateTimeOffset start,
        DateTimeOffset end, CancellationToken token)
    {
        var rows = new List<SpanRow>();
        if (end <= start)
            return rows;
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        // SQL is private and fixed by the adapter. Read once per table, never once per day.
        command.CommandText = $"""
            SELECT * FROM ({sql})
            WHERE (julianday(started_at) IS NULL OR julianday(started_at) < julianday($end))
              AND (effective_end IS NULL OR julianday(effective_end) IS NULL OR julianday(effective_end) > julianday($start))
            LIMIT {MaximumRowsPerQuery + 1};
            """;
        command.Parameters.AddWithValue("$start", start.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$end", end.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
        using var registration = token.Register(command.Cancel);
        using var reader = command.ExecuteReader();
        var visited = 0;
        while (reader.Read())
        {
            token.ThrowIfCancellationRequested();
            if (++visited > MaximumRowsPerQuery)
                throw new AnalyticsReadException(AnalyticsError.LimitExceeded, "Too many source rows; request a smaller snapshot.");
            var spanStart = Parse(reader.GetString(0));
            var spanEnd = reader.IsDBNull(1) ? end : Parse(reader.GetString(1));
            if (spanEnd < spanStart)
                throw new AnalyticsReadException(AnalyticsError.InvalidData, "A source interval ends before it starts.");
            if (spanEnd <= start)
                continue;
            rows.Add(new SpanRow(Max(spanStart, start), Min(spanEnd, end),
                reader.FieldCount > 2 ? reader.GetInt64(2) : null,
                reader.FieldCount > 2 ? reader.GetString(3) : null,
                reader.FieldCount > 2 && !reader.IsDBNull(4) ? reader.GetInt64(4) : null,
                reader.FieldCount > 2 && !reader.IsDBNull(5) ? reader.GetString(5) : null));
        }
        return rows;
    }

    private static DateTimeOffset Parse(string text)
    {
        if (!DateTimeOffset.TryParseExact(text, "O", CultureInfo.InvariantCulture, DateTimeStyles.None, out var value))
            throw new AnalyticsReadException(AnalyticsError.InvalidData, "Source timestamps must be ISO 8601 round-trip values with an offset.");
        return value;
    }

    private static DateTimeOffset Boundary(DateOnly date, TimeZoneInfo zone)
    {
        var local = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        // Some zones skip midnight or a whole date. Use the first valid minute.
        for (var minutes = 0; zone.IsInvalidTime(local); minutes++)
        {
            if (minutes >= 1440)
                throw new ArgumentException("The requested timezone has an unsupported calendar transition.");
            local = local.AddMinutes(1);
        }
        var offset = zone.IsAmbiguousTime(local) ? zone.GetAmbiguousTimeOffsets(local).Max() : zone.GetUtcOffset(local);
        return new DateTimeOffset(local, offset);
    }

    private static long Milliseconds(SpanRow span, DateTimeOffset start, DateTimeOffset end) =>
        Math.Max(0, (long)(Min(span.End, end) - Max(span.Start, start)).TotalMilliseconds);

    private static long UnionMilliseconds(List<SpanRow> spans, DateTimeOffset start, DateTimeOffset end)
    {
        var cursor = start;
        long total = 0;
        foreach (var span in spans.OrderBy(span => span.Start))
        {
            var left = Max(cursor, Max(start, span.Start));
            var right = Min(end, span.End);
            if (right <= left)
                continue;
            total += (long)(right - left).TotalMilliseconds;
            cursor = right;
        }
        return total;
    }

    private static DateTimeOffset Min(DateTimeOffset a, DateTimeOffset b) => a < b ? a : b;
    private static DateTimeOffset Max(DateTimeOffset a, DateTimeOffset b) => a > b ? a : b;
    private sealed record SpanRow(DateTimeOffset Start, DateTimeOffset End, long? AppId, string? Name,
        long? CategoryId, string? CategoryName);

    public void Dispose()
    {
        lock (gate)
        {
            if (disposed)
                return;
            connection.Dispose();
            sourceLease.Dispose();
            disposed = true;
        }
    }
}
