using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using TimePilot.Analytics.Contracts;
using TimePilot.Analytics.Sqlite;

if (!OperatingSystem.IsWindows())
    throw new PlatformNotSupportedException("This offline adapter validation requires Windows.");
if (args.Length != 1 || !Path.IsPathFullyQualified(args[0]))
    throw new ArgumentException("Supply one absolute, new sandbox directory. No user DB argument is accepted.");
var sandbox = Path.GetFullPath(args[0]);
if (Path.Exists(sandbox))
    throw new IOException("The synthetic sandbox must not already exist; nothing will be overwritten.");
if (DateTimeOffset.UtcNow < new DateTimeOffset(2024, 3, 1, 0, 0, 0, TimeSpan.Zero))
    throw new InvalidOperationException("Fixture v1 requires a host clock after February 2024.");

var sourceDirectory = Path.Combine(sandbox, "synthetic-source");
Directory.CreateDirectory(sourceDirectory);
var source = Path.Combine(sourceDirectory, "fixture-v1.db");
// This is fixture generation only, not an initialization API for real Community data.
using (var db = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = source, Pooling = false }.ToString()))
{
    db.Open();
    using var command = db.CreateCommand();
    command.CommandText = """
        CREATE TABLE apps(id INTEGER PRIMARY KEY, display_name TEXT, process_name TEXT, user_alias TEXT, primary_category_id INTEGER);
        CREATE TABLE app_categories(id INTEGER PRIMARY KEY, name TEXT);
        CREATE TABLE foreground_sessions(id INTEGER PRIMARY KEY, app_id INTEGER, started_at TEXT, ended_at TEXT, last_observed_at TEXT);
        CREATE TABLE idle_sessions(id INTEGER PRIMARY KEY, started_at TEXT, ended_at TEXT);
        CREATE TABLE app_runtime_sessions(id INTEGER PRIMARY KEY, started_at TEXT, ended_at TEXT, last_heartbeat_at TEXT);
        INSERT INTO app_categories VALUES(10, 'Synthetic development');
        INSERT INTO apps VALUES(1, 'Synthetic editor', 'synthetic-editor', NULL, 10);
        INSERT INTO foreground_sessions VALUES(1, 1, '2024-02-28T09:00:00.0000000+00:00', '2024-02-28T11:00:00.0000000+00:00', NULL);
        INSERT INTO idle_sessions VALUES(1, '2024-02-28T11:00:00.0000000+00:00', '2024-02-28T12:00:00.0000000+00:00');
        INSERT INTO app_runtime_sessions VALUES(1, '2024-02-28T09:00:00.0000000+00:00', '2024-02-28T12:00:00.0000000+00:00', NULL);
        """;
    command.ExecuteNonQuery();
}
File.WriteAllText(Path.Combine(sourceDirectory, "settings.json"), "{\"syntheticFixtureVersion\":1}");
var before = SourceFiles(sourceDirectory);
var prepared = OfflineAnalyticsSnapshot.Create(source, Path.Combine(sandbox, "prepared-snapshot"));
DailySnapshot result;
using (IDailyAnalyticsReader reader = OfflineAnalyticsSnapshot.Open(prepared.DirectoryPath))
{
    result = reader.Read(new DailyQuery(new(2024, 2, 28), new(2024, 2, 29), TimeZoneInfo.Utc, reader.SourceCapturedAt));
    var day = result.Days.Single();
    Require(day.ActiveMilliseconds == 7_200_000, "Expected two foreground hours.");
    Require(day.IdleMilliseconds == 3_600_000, "Expected one idle hour.");
    Require(day.Categories.Single().CategoryId == 10, "Category identity must survive the public boundary.");
    Require(day.RecorderCalendarCoverage.ElapsedCalendarMilliseconds == 86_400_000, "Expected an elapsed calendar day, not Windows-recordable time.");
    Require(day.RecorderCalendarCoverage.RecorderRunningMilliseconds == 10_800_000, "Expected three recorder hours.");
    Require(result.SourceCapturedAt == prepared.SourceCapturedAt, "Capture provenance must be preserved.");
}
Require(before.SequenceEqual(SourceFiles(sourceDirectory)), "Source DB, settings, or file list changed.");
using (var reopened = OfflineAnalyticsSnapshot.Open(prepared.DirectoryPath))
    Require(reopened.SourceCapturedAt == prepared.SourceCapturedAt, "Reopening must not replace the capture time with now.");

Console.WriteLine(JsonSerializer.Serialize(new
{
    FixtureVersion = 1,
    ContractVersion = result.ContractVersion,
    result.SchemaProfile,
    SnapshotFormatVersion = OfflineAnalyticsSnapshot.FormatVersion,
    result.SourceCapturedAt,
    SourceUnchanged = true,
    ActiveMilliseconds = result.Days[0].ActiveMilliseconds,
    IdleMilliseconds = result.Days[0].IdleMilliseconds,
    RecorderToCalendarRatio = result.Days[0].RecorderCalendarCoverage.RecorderToCalendarRatio
}, new JsonSerializerOptions { WriteIndented = true }));

static KeyValuePair<string, string>[] SourceFiles(string directory) => Directory.GetFiles(directory)
    .OrderBy(path => Path.GetFileName(path), StringComparer.Ordinal)
    .Select(path => new KeyValuePair<string, string>(Path.GetFileName(path), Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)))))
    .ToArray();

static void Require(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}
