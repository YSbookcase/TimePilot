using System.Globalization;
using System.Security.Cryptography;
using Microsoft.Data.Sqlite;

namespace TimePilot.Analytics.ConsumerTests;

// Fixture v1: all data is synthetic; this project has no reference to the recording app.
internal sealed class DailyFixture : IDisposable
{
    public string Root { get; } = Path.Combine(Path.GetTempPath(), $"TimePilotConsumer-{Guid.NewGuid():N}");
    public string DatabasePath => Path.Combine(Root, "synthetic.db");

    public DailyFixture()
    {
        Directory.CreateDirectory(Root);
        Execute("""
            CREATE TABLE apps(id INTEGER PRIMARY KEY, display_name TEXT, process_name TEXT, user_alias TEXT, primary_category_id INTEGER);
            CREATE TABLE app_categories(id INTEGER PRIMARY KEY, name TEXT);
            CREATE TABLE foreground_sessions(id INTEGER PRIMARY KEY, app_id INTEGER, started_at TEXT, ended_at TEXT, last_observed_at TEXT);
            CREATE TABLE idle_sessions(id INTEGER PRIMARY KEY, started_at TEXT, ended_at TEXT);
            CREATE TABLE app_runtime_sessions(id INTEGER PRIMARY KEY, started_at TEXT, ended_at TEXT, last_heartbeat_at TEXT);
            INSERT INTO app_categories VALUES (10, 'Development'), (20, 'Reading');
            INSERT INTO apps VALUES (1, 'Editor', 'editor', 'My editor', 10), (2, 'Reader', 'reader', NULL, 20), (3, 'Other', 'other', NULL, NULL);
            """);
        File.WriteAllText(Path.Combine(Root, "settings.json"), "{\"fixture\":\"not read or saved\"}");
    }

    public void Execute(string sql)
    {
        using var db = Connect();
        using var command = db.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    public SqliteConnection Connect()
    {
        var db = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = DatabasePath, Pooling = false }.ToString());
        db.Open();
        return db;
    }

    public void Foreground(int app, DateTimeOffset start, DateTimeOffset? end, DateTimeOffset? observed = null) =>
        Insert("foreground_sessions", "app_id,started_at,ended_at,last_observed_at", app, Stamp(start), Stamp(end), Stamp(observed));

    public void Idle(DateTimeOffset start, DateTimeOffset? end) =>
        Insert("idle_sessions", "started_at,ended_at", Stamp(start), Stamp(end));

    public void Runtime(DateTimeOffset start, DateTimeOffset? end, DateTimeOffset? heartbeat = null) =>
        Insert("app_runtime_sessions", "started_at,ended_at,last_heartbeat_at", Stamp(start), Stamp(end), Stamp(heartbeat));

    private void Insert(string table, string columns, params object?[] values)
    {
        using var db = Connect();
        using var command = db.CreateCommand();
        command.CommandText = $"INSERT INTO {table} ({columns}) VALUES ({string.Join(',', values.Select((_, i) => "$p" + i))});";
        for (var i = 0; i < values.Length; i++)
            command.Parameters.AddWithValue("$p" + i, values[i] ?? DBNull.Value);
        command.ExecuteNonQuery();
    }

    private static string? Stamp(DateTimeOffset? value) => value?.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);

    public Dictionary<string, string> Files() => Directory.GetFiles(Root)
        .ToDictionary(path => Path.GetFileName(path)!, path =>
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            return Convert.ToHexString(SHA256.HashData(stream));
        });

    public string LogicalContents()
    {
        using var db = Connect();
        using var command = db.CreateCommand();
        command.CommandText = "SELECT group_concat(sql, '|') FROM sqlite_schema;";
        var schema = command.ExecuteScalar()?.ToString();
        command.CommandText = "SELECT COUNT(*) FROM foreground_sessions;";
        return schema + "|" + command.ExecuteScalar();
    }

    public void Dispose() => Directory.Delete(Root, recursive: true);
}
