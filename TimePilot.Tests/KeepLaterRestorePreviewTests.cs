using System.IO.Compression;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using TimePilot.WinForms.KYS24;
using Xunit;

namespace TimePilot.Tests;

public sealed class KeepLaterRestorePreviewTests
{
    [Fact]
    public void HalfOpenBoundaryCountsNeverSplitCrossingOrCloseOpenRows()
    {
        var t = DateTimeOffset.Parse("2026-11-22T12:00:00Z");
        RestorePreviewRecord[] rows =
        [
            new(t.AddMinutes(-10), t.AddMinutes(-5)), new(t.AddMinutes(-5), t),
            new(t, t.AddMinutes(1)), new(t.AddSeconds(-1), t.AddSeconds(1)),
            new(t, null, IsPoint: true), new(t, t),
            new(t.AddMinutes(-1), null, t.AddMinutes(1)),
            new(t, t.AddSeconds(-1)), new(null, t), new(t.AddDays(2), null),
            new(t, t.AddDays(2)), new(t, t, Invalid: true)
        ];
        var counts = KeepLaterRestorePreviewReader.Count(rows, t, t.AddDays(1));
        Assert.Equal(2, counts.Before);
        Assert.Equal(3, counts.AtOrAfter);
        Assert.Equal(1, counts.Crossing);
        Assert.Equal(1, counts.Open);
        Assert.Equal(5, counts.Invalid);
        Assert.Equal(rows.Length, counts.Total);
        var shifted = KeepLaterRestorePreviewReader.Count([new(t.ToOffset(TimeSpan.FromHours(9)), t.AddSeconds(1))], t, t.AddDays(1));
        Assert.Equal(1, shifted.AtOrAfter);
    }

    [Theory]
    [InlineData("metadata", (int)RestoreReferenceSource.Metadata)]
    [InlineData("heartbeat", (int)RestoreReferenceSource.RuntimeHeartbeat)]
    [InlineData("observation", (int)RestoreReferenceSource.LastObservation)]
    [InlineData("missing", (int)RestoreReferenceSource.Missing)]
    [InlineData("future", (int)RestoreReferenceSource.LastObservation)]
    public void ReferenceSourceIsExplicitAndNeverUsesCurrentDataAsBackupCutoff(string scenario, int expected)
    {
        using var f = new CategoryImportFixture();
        if (scenario != "missing") f.Source.RecordSystemEvent("test", f.Now, f.Now.AddHours(-1), "Synthetic");
        if (scenario == "heartbeat")
        {
            f.Source.BeginRuntimeSession(f.Now.AddHours(-1), f.Now.AddHours(-2), "test");
            f.Source.UpdateRuntimeHeartbeat(f.Now.AddMinutes(-1));
        }
        using var backup = Open(f.SourceDb);
        using var current = Open(f.TargetDb);
        var metadata = scenario == "metadata" ? f.Now.AddMinutes(1) : scenario == "future" ? f.Now.AddDays(2) : (DateTimeOffset?)null;
        var preview = KeepLaterRestorePreviewReader.Read(backup, current, metadata, f.Now.AddDays(1));
        Assert.Equal((RestoreReferenceSource)expected, preview.ReferenceSource);
        if (scenario == "missing")
        {
            Assert.Null(preview.ReferenceAt);
            Assert.Equal(1, preview.Tables.Single(t => t.Name == "system_events").Current.Unclassified);
        }
        if (scenario == "heartbeat")
        {
            Assert.Equal(f.Now.AddMinutes(-1), preview.ReferenceAt);
            Assert.True(preview.BackupBeyondReference);
            Assert.Equal(1, preview.Open);
        }
        if (scenario == "future") Assert.True(preview.MetadataAt > preview.ObservedAt);
    }

    [Fact]
    public void MissingTablesInvalidTimesAndOpenIdleAreNotReportedAsSafeRows()
    {
        using var f = new CategoryImportFixture();
        f.Execute(f.SourceDb, "DROP TABLE process_runtime_sessions;");
        f.Execute(f.SourceDb, "INSERT INTO idle_sessions(started_at,ended_at,threshold_ms) VALUES('2026-11-22T00:00:00Z',NULL,60000);");
        f.Execute(f.SourceDb, "INSERT INTO system_events(event_type,occurred_at) VALUES('test','not-a-date');");
        using var backup = Open(f.SourceDb);
        using var current = Open(f.TargetDb);
        var preview = KeepLaterRestorePreviewReader.Read(backup, current, f.Now, f.Now.AddDays(1));
        Assert.True(preview.MissingTables);
        Assert.Equal(1, preview.Open);
        Assert.Equal(1, preview.Invalid);
        Assert.False(preview.Tables.Single(t => t.Name == "process_runtime_sessions").BackupAvailable);
    }

    [Fact]
    public void DetailedPreviewUsesSnapshotsAndPreservesCurrentSourceZipAndSettings()
    {
        using var f = new CategoryImportFixture();
        f.PrepareApp();
        var extend = "UPDATE foreground_sessions SET ended_at='2026-11-22T00:00:10+00:00', last_observed_at='2026-11-22T00:00:10+00:00', duration_ms=10000;";
        f.Execute(f.SourceDb, extend);
        f.Execute(f.TargetDb, extend);
        var session = f.Target.StartForegroundSession(new("after", "After", null), f.Now.AddSeconds(30));
        f.Target.UpdateForegroundSessionObservation(session, new("after", "After", null), f.Now.AddSeconds(40));
        f.Target.EndForegroundSession(session, f.Now.AddSeconds(40));
        f.BuildZip();
        WriteMetadata(f, new { SchemaVersion = 1, CreatedAtUtc = f.Now.AddSeconds(5) });
        var sourceHash = f.Hash(f.SourceDb);
        var currentHash = f.Hash(f.TargetDb);
        var settingsHash = f.Hash(f.Settings);
        var zipHash = f.Hash(f.Zip);
        var progress = new CollectingProgress();
        var comparison = new DataBackupService(Path.GetDirectoryName(f.TargetDb)!).InspectBackupDetailedComparison(f.Zip, f.Now.AddDays(1), progress);
        Assert.NotNull(comparison);
        var preview = Assert.IsType<KeepLaterRestorePreview>(comparison.KeepLaterPreview);
        Assert.Equal(RestoreReferenceSource.Metadata, preview.ReferenceSource);
        Assert.Equal(f.Now.AddSeconds(5), preview.ReferenceAt);
        var foreground = preview.Tables.Single(t => t.Name == "foreground_sessions");
        Assert.Equal(1, foreground.Backup.Crossing);
        Assert.Equal(1, foreground.Current.Crossing);
        Assert.Equal(1, foreground.Current.AtOrAfter);
        Assert.Equal(sourceHash, f.Hash(f.SourceDb));
        Assert.Equal(currentHash, f.Hash(f.TargetDb));
        Assert.Equal(settingsHash, f.Hash(f.Settings));
        Assert.Equal(zipHash, f.Hash(f.Zip));
        Assert.Equal(Enum.GetValues<RestoreAnalysisStage>(), progress.Values.Select(v => v.Stage).Distinct());
        foreach (var group in progress.Values.Where(v => v.Total is not null).GroupBy(v => v.Stage))
        {
            Assert.Equal(0, group.First().Completed);
            Assert.Equal(group.Last().Total, group.Last().Completed);
            Assert.Equal(group.Select(v => v.Completed).OrderBy(v => v), group.Select(v => v.Completed));
        }
        Assert.Equal(15, progress.Values.Last().Completed);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("local-time")]
    [InlineData("unsupported")]
    public void UntrustedMetadataFallsBackWithoutInventingCreationTime(string scenario)
    {
        using var f = new CategoryImportFixture();
        f.PrepareApp();
        if (scenario == "local-time") WriteMetadata(f, new { SchemaVersion = 1, CreatedAtUtc = "2026-11-22T00:00:00" });
        if (scenario == "unsupported") WriteMetadata(f, new { SchemaVersion = 99, CreatedAtUtc = f.Now });
        var comparison = new DataBackupService(Path.GetDirectoryName(f.TargetDb)!).InspectBackupDetailedComparison(f.Zip, f.Now.AddDays(1));
        Assert.NotNull(comparison);
        var preview = Assert.IsType<KeepLaterRestorePreview>(comparison.KeepLaterPreview);
        Assert.Equal(RestoreReferenceSource.LastObservation, preview.ReferenceSource);
        Assert.Null(preview.MetadataAt);
    }

    private static void WriteMetadata(CategoryImportFixture f, object metadata)
    {
        using var zip = ZipFile.Open(f.Zip, ZipArchiveMode.Update);
        zip.GetEntry("metadata.json")?.Delete();
        using var stream = zip.CreateEntry("metadata.json").Open();
        JsonSerializer.Serialize(stream, metadata);
    }

    private sealed class CollectingProgress : IProgress<RestoreAnalysisProgress>
    {
        internal List<RestoreAnalysisProgress> Values { get; } = new();
        public void Report(RestoreAnalysisProgress value) => Values.Add(value);
    }

    private static SqliteConnection Open(string path)
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, Mode = SqliteOpenMode.ReadOnly, Pooling = false }.ToString());
        connection.Open();
        return connection;
    }
}
