using System.Globalization;
using Microsoft.Data.Sqlite;

namespace TimePilot.WinForms.KYS24;

internal enum RestoreReferenceSource { Missing, Metadata, RuntimeHeartbeat, LastObservation }
internal sealed record RestorePreviewRecord(DateTimeOffset? Start, DateTimeOffset? End,
    DateTimeOffset? Observation = null, bool IsPoint = false, bool Invalid = false);
internal sealed record RestorePreviewCounts(int Before, int AtOrAfter, int Crossing, int Open, int Invalid, int Unclassified,
    DateTimeOffset? Earliest, DateTimeOffset? Latest)
{
    public int Total => Before + AtOrAfter + Crossing + Open + Invalid + Unclassified;
}
internal sealed record RestorePreviewTable(string Name, RestorePreviewCounts Backup, RestorePreviewCounts Current,
    bool BackupAvailable, bool CurrentAvailable);
internal sealed record KeepLaterRestorePreview(DateTimeOffset? ReferenceAt, RestoreReferenceSource ReferenceSource,
    DateTimeOffset ObservedAt, DateTimeOffset? MetadataAt, DateTimeOffset? BackupLastObservation,
    IReadOnlyList<RestorePreviewTable> Tables)
{
    public int CurrentBefore => Tables.Sum(t => t.Current.Before);
    public int CurrentAfter => Tables.Sum(t => t.Current.AtOrAfter);
    public int Crossing => Tables.Sum(t => t.Backup.Crossing + t.Current.Crossing);
    public int Open => Tables.Sum(t => t.Backup.Open + t.Current.Open);
    public int Invalid => Tables.Sum(t => t.Backup.Invalid + t.Current.Invalid);
    public bool MissingTables => Tables.Any(t => !t.BackupAvailable || !t.CurrentAvailable);
    public bool BackupBeyondReference => ReferenceAt is { } cutoff && BackupLastObservation > cutoff;
}

internal static class KeepLaterRestorePreviewReader
{
    private static readonly string[] Tables = ["foreground_sessions", "idle_sessions", "app_runtime_sessions", "process_runtime_sessions", "system_events"];

    public static KeepLaterRestorePreview Read(SqliteConnection backup, SqliteConnection current,
        DateTimeOffset? metadataAt, DateTimeOffset observedAt, IProgress<RestoreAnalysisProgress>? progress = null)
    {
        var backupTables = Tables.ToDictionary(name => name, name => DescribeTable(backup, name));
        var currentTables = Tables.ToDictionary(name => name, name => DescribeTable(current, name));
        DateTimeOffset? last = null, heartbeat = null;
        var completed = 0;
        const int passes = 15;
        progress?.Report(new(RestoreAnalysisStage.Classify, completed, passes));
        foreach (var table in backupTables.Values)
        {
            foreach (var row in ReadRecords(backup, table))
            {
                if (!Valid(row, observedAt)) continue;
                last = Latest(last, Latest(row.Start, Latest(row.End, row.Observation)));
                if (table.Name == "app_runtime_sessions") heartbeat = Latest(heartbeat, row.Observation);
            }
            progress?.Report(new(RestoreAnalysisStage.Classify, ++completed, passes));
        }
        DateTimeOffset? cutoff;
        RestoreReferenceSource source;
        if (metadataAt is { } metadata && metadata <= observedAt)
            (cutoff, source) = (metadata.ToUniversalTime(), RestoreReferenceSource.Metadata);
        else if (heartbeat is { } runtime)
            (cutoff, source) = (runtime, RestoreReferenceSource.RuntimeHeartbeat);
        else if (last is { } observation)
            (cutoff, source) = (observation, RestoreReferenceSource.LastObservation);
        else
            (cutoff, source) = (null, RestoreReferenceSource.Missing);
        var tables = new List<RestorePreviewTable>();
        foreach (var name in Tables)
        {
            var backupCounts = Count(ReadRecords(backup, backupTables[name]), cutoff, observedAt);
            progress?.Report(new(RestoreAnalysisStage.Classify, ++completed, passes));
            var currentCounts = Count(ReadRecords(current, currentTables[name]), cutoff, observedAt);
            progress?.Report(new(RestoreAnalysisStage.Classify, ++completed, passes));
            tables.Add(new(name, backupCounts, currentCounts, backupTables[name].Available, currentTables[name].Available));
        }
        return new(cutoff, source, observedAt, metadataAt, last, tables);
    }

    internal static RestorePreviewCounts Count(IEnumerable<RestorePreviewRecord> rows,
        DateTimeOffset? cutoff, DateTimeOffset observedAt)
    {
        int before = 0, after = 0, crossing = 0, open = 0, invalid = 0, unclassified = 0;
        DateTimeOffset? earliest = null, latest = null;
        foreach (var row in rows)
        {
            if (!Valid(row, observedAt)) { invalid++; continue; }
            var start = row.Start!.Value;
            earliest = earliest is null || start < earliest ? start : earliest;
            latest = Latest(latest, Latest(row.Start, Latest(row.End, row.Observation)));
            if (!row.IsPoint && row.End is null) { open++; continue; }
            if (cutoff is null) { unclassified++; continue; }
            if (row.Start >= cutoff) after++;
            else if (row.IsPoint || row.End <= cutoff) before++;
            else crossing++;
        }
        return new(before, after, crossing, open, invalid, unclassified, earliest, latest);
    }

    private static DateTimeOffset? Latest(DateTimeOffset? a, DateTimeOffset? b) => a is null ? b : b is null || a > b ? a : b;

    private static bool Valid(RestorePreviewRecord row, DateTimeOffset observedAt) =>
        !row.Invalid && row.Start is { } start && start <= observedAt
        && (row.End is null || row.End >= start && row.End <= observedAt)
        && (row.Observation is null || row.Observation >= start && row.Observation <= observedAt);

    private sealed record TableDefinition(string Name, string StartColumn, string ObservationSql, bool Point, bool Available);

    private static TableDefinition DescribeTable(SqliteConnection connection, string table)
    {
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using (var info = connection.CreateCommand())
        {
            info.CommandText = $"PRAGMA table_info({table});";
            using var reader = info.ExecuteReader();
            while (reader.Read()) columns.Add(reader.GetString(1));
        }
        var point = table == "system_events";
        var startColumn = point ? "occurred_at" : "started_at";
        var observationColumn = table == "app_runtime_sessions" ? "last_heartbeat_at" : "last_observed_at";
        var observationSql = columns.Contains(observationColumn) ? observationColumn : "NULL";
        return new(table, startColumn, observationSql, point, columns.Contains(startColumn) && (point || columns.Contains("ended_at")));
    }

    private static IEnumerable<RestorePreviewRecord> ReadRecords(SqliteConnection connection, TableDefinition table)
    {
        if (!table.Available) yield break;
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT {table.StartColumn}, {(table.Point ? "NULL" : "ended_at")}, {table.ObservationSql} FROM {table.Name};";
        using var rows = command.ExecuteReader();
        while (rows.Read())
        {
            bool invalid = false;
            var start = Parse(rows, 0, ref invalid);
            var end = Parse(rows, 1, ref invalid);
            var observation = Parse(rows, 2, ref invalid);
            yield return new(start, end, observation, table.Point, invalid);
        }
    }

    private static DateTimeOffset? Parse(SqliteDataReader row, int column, ref bool invalid)
    {
        if (row.IsDBNull(column)) return null;
        var value = row.GetValue(column) as string;
        // Offset-free local timestamps cannot define a portable restore boundary.
        if (value is not null && (value.EndsWith('Z') || value.LastIndexOf('+') > 9 || value.LastIndexOf('-') > 9)
            && DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            return parsed.ToUniversalTime();
        invalid = true;
        return null;
    }
}
