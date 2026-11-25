using Microsoft.Data.Sqlite;
using TimePilot.WinForms.KYS24;
using Xunit;

namespace TimePilot.Tests;

public sealed class KeepLaterStagingBuilderTests
{
    private static string Output(CategoryImportFixture f) => Path.Combine(f.Root, "result.db");

    internal static KeepLaterRestorePreparation Prepare(CategoryImportFixture f)
    {
        SqliteConnection.ClearAllPools();
        using var backup = Connect(f.SourceDb);
        using var current = Connect(f.TargetDb);
        var preview = KeepLaterRestorePreviewReader.Read(backup, current, f.Now.AddHours(1), f.Now.AddDays(1));
        return KeepLaterRestorePreparation.Build(preview, CategoryImportService.ReadState(backup),
            CategoryImportService.ReadState(current), KeepLaterStagingBuilder.Hash(f.SourceDb),
            KeepLaterStagingBuilder.Hash(f.TargetDb), RestoreSettingsChoice.KeepCurrent);
    }

    private static SqliteConnection Connect(string path)
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
            { DataSource = path, Pooling = false, Mode = SqliteOpenMode.ReadOnly }.ToString());
        connection.Open();
        return connection;
    }

    private static object? Scalar(string path, string sql)
    {
        using var c = Connect(path);
        using var command = c.CreateCommand();
        command.CommandText = sql;
        return command.ExecuteScalar();
    }

    // Both sources deliberately reuse numeric IDs for unrelated apps and runtime sessions.
    internal static void Populate(CategoryImportFixture f)
    {
        f.Source.CreateCustomAppCategory("Backup custom", "Red");
        var custom = f.Target.CreateCustomAppCategory("Current custom", "Blue");
        f.AddApp(f.Source, "backup-app", @"C:\backup.exe");
        var currentApp = f.AddApp(f.Target, "current-app", @"C:\current.exe");
        f.Target.SetAppPrimaryCategory(currentApp, custom);
        f.Target.SetAppUserAlias(currentApp, "My alias");
        foreach (var (path, hour) in new[] { (f.SourceDb, "00"), (f.TargetDb, "02") })
        {
            var start = $"2026-11-22T{hour}:00:00+00:00";
            var end = $"2026-11-22T{hour}:00:10+00:00";
            f.Execute(path, $"""
                UPDATE foreground_sessions SET started_at='{start}',ended_at='{end}',last_observed_at='{end}',duration_ms=10000;
                INSERT INTO app_runtime_sessions(id,started_at,ended_at,duration_ms,last_heartbeat_at,shutdown_reason,app_version)
                  VALUES(1,'{start}','{end}',10000,'{end}','normal','synthetic');
                INSERT INTO idle_sessions(started_at,ended_at,duration_ms,threshold_ms,foreground_app_id)
                  VALUES('{start}','{end}',10000,60000,1);
                INSERT INTO process_runtime_sessions(app_id,process_id,started_at,ended_at,duration_ms,first_observed_at,last_observed_at,tracking_scope,has_main_window,is_current_session_process)
                  VALUES(1,123,'{start}','{end}',10000,'{start}','{end}',1,1,1);
                DELETE FROM system_events;
                INSERT INTO system_events(event_type,occurred_at,app_runtime_session_id,details)
                  VALUES('test','{end}',1,'synthetic');
                """);
        }
    }

    [Fact]
    public void BuildsMergedOfflineDbWithRemappedIdsAndUnchangedSources()
    {
        using var f = new CategoryImportFixture();
        Populate(f);
        var plan = Prepare(f);
        Assert.Empty(plan.Blockers);
        var settings = f.Hash(f.Settings);
        var result = KeepLaterStagingBuilder.Build(f.SourceDb, f.TargetDb, Output(f), plan);
        Assert.Equal(plan.BackupSha256, KeepLaterStagingBuilder.Hash(f.SourceDb));
        Assert.Equal(plan.CurrentSha256, KeepLaterStagingBuilder.Hash(f.TargetDb));
        Assert.Equal(settings, f.Hash(f.Settings));
        Assert.Equal(result.Sha256, KeepLaterStagingBuilder.Hash(result.DatabasePath));
        Assert.Equal(plan.PlanSha256, result.PreparationSha256);
        Assert.All(result.RecordCounts.Values, count => Assert.Equal(2, count));
        Assert.Equal("ok", Scalar(Output(f), "PRAGMA integrity_check;"));
        Assert.Null(Scalar(Output(f), "PRAGMA foreign_key_check;"));
        Assert.Equal(2L, Scalar(Output(f), "SELECT COUNT(*) FROM foreground_sessions;"));
        Assert.Equal(20000L, Scalar(Output(f), "SELECT SUM(duration_ms) FROM process_runtime_sessions;"));
        Assert.Equal("current-app", Scalar(Output(f), "SELECT a.process_name FROM foreground_sessions s JOIN apps a ON a.id=s.app_id WHERE s.started_at LIKE '%T02:%';"));
        Assert.Equal("Current custom", Scalar(Output(f), "SELECT c.name FROM apps a JOIN app_categories c ON c.id=a.primary_category_id WHERE a.process_name='current-app';"));
        Assert.Equal("My alias", Scalar(Output(f), "SELECT user_alias FROM apps WHERE process_name='current-app';"));
        Assert.Equal("import", Scalar(Output(f), "SELECT source FROM app_categories WHERE name='Current custom';"));
        Assert.Equal(2L, Scalar(Output(f), "SELECT COUNT(*) FROM system_events e JOIN app_runtime_sessions r ON r.id=e.app_runtime_session_id WHERE substr(e.occurred_at,1,13)=substr(r.started_at,1,13);"));
        Assert.Equal(0L, Scalar(Output(f), "SELECT COUNT(*) FROM app_observations o JOIN apps a ON a.id=o.app_id WHERE o.process_name<>a.process_name;"));
        Assert.Empty(Directory.GetFiles(f.Root, "ActiveLogbook-staging-*"));
        Assert.Throws<IOException>(() => KeepLaterStagingBuilder.Build(f.SourceDb, f.TargetDb, Output(f), plan));
        Assert.Equal(result.Sha256, KeepLaterStagingBuilder.Hash(Output(f)));
    }

    [Theory]
    [InlineData("open")]
    [InlineData("crossing")]
    [InlineData("settings")]
    [InlineData("changed")]
    [InlineData("sidecar")]
    public void UnsafeOrStalePreparationNeverPublishes(string kind)
    {
        using var f = new CategoryImportFixture();
        Populate(f);
        if (kind == "open") f.Execute(f.TargetDb, "UPDATE foreground_sessions SET ended_at=NULL;");
        if (kind == "crossing") f.Execute(f.TargetDb, "UPDATE foreground_sessions SET started_at='2026-11-22T00:59:00Z';");
        var plan = Prepare(f);
        if (kind == "settings") plan = plan.WithSettingsChoice(RestoreSettingsChoice.UseBackup);
        if (kind == "changed") f.Execute(f.TargetDb, "UPDATE apps SET user_alias='changed';");
        if (kind == "sidecar") File.WriteAllText(f.TargetDb + "-wal", "synthetic");
        Assert.Throws<InvalidOperationException>(() => KeepLaterStagingBuilder.Build(f.SourceDb, f.TargetDb, Output(f), plan));
        Assert.False(File.Exists(Output(f)));
        Assert.Empty(Directory.GetFiles(f.Root, "ActiveLogbook-staging-*"));
    }

    [Theory]
    [InlineData("table")]
    [InlineData("column")]
    [InlineData("trigger")]
    [InlineData("foreign-key")]
    public void UnsupportedSchemasAreRejectedWithoutChangingInputs(string kind)
    {
        using var f = new CategoryImportFixture();
        Populate(f);
        f.Execute(f.TargetDb, kind switch
        {
            "table" => "CREATE TABLE future_records(id INTEGER);",
            "column" => "ALTER TABLE apps ADD COLUMN future_value TEXT;",
            "trigger" => "CREATE TRIGGER future_trigger AFTER INSERT ON apps BEGIN SELECT 1; END;",
            _ => "PRAGMA foreign_keys=OFF; ALTER TABLE system_events RENAME TO old_events; CREATE TABLE system_events AS SELECT * FROM old_events; DROP TABLE old_events;"
        });
        var plan = Prepare(f);
        Assert.Throws<InvalidDataException>(() => KeepLaterStagingBuilder.Build(f.SourceDb, f.TargetDb, Output(f), plan));
        Assert.Equal(plan.CurrentSha256, KeepLaterStagingBuilder.Hash(f.TargetDb));
        Assert.False(File.Exists(Output(f)));
    }

    [Fact]
    public void MissingRetainedRuntimeParentRollsBackAndCleansOnlyOwnedStaging()
    {
        using var f = new CategoryImportFixture();
        Populate(f);
        // Valid source FK, but its parent is before cutoff and not selected from current.
        f.Execute(f.TargetDb, "UPDATE app_runtime_sessions SET started_at='2026-11-22T00:00:00Z',ended_at='2026-11-22T00:00:10Z',last_heartbeat_at='2026-11-22T00:00:10Z';");
        var plan = Prepare(f);
        Assert.Empty(plan.Blockers);
        var unrelated = Path.Combine(f.Root, "other.db");
        File.WriteAllText(unrelated, "keep me");
        Assert.Throws<InvalidDataException>(() => KeepLaterStagingBuilder.Build(f.SourceDb, f.TargetDb, Output(f), plan));
        Assert.False(File.Exists(Output(f)));
        Assert.Equal("keep me", File.ReadAllText(unrelated));
        Assert.Empty(Directory.GetFiles(f.Root, "ActiveLogbook-staging-*"));
        Assert.Equal(plan.BackupSha256, KeepLaterStagingBuilder.Hash(f.SourceDb));
        Assert.Equal(plan.CurrentSha256, KeepLaterStagingBuilder.Hash(f.TargetDb));
    }

    [Fact]
    public void CutoffIsInclusiveForCurrentPointsAndZeroLengthSessions()
    {
        using var f = new CategoryImportFixture();
        Populate(f);
        f.Execute(f.TargetDb, """
            UPDATE foreground_sessions SET started_at='2026-11-22T01:00:00Z',ended_at='2026-11-22T01:00:00Z',last_observed_at='2026-11-22T01:00:00Z',duration_ms=0;
            UPDATE system_events SET occurred_at='2026-11-22T01:00:00Z',app_runtime_session_id=NULL;
            """);
        var plan = Prepare(f);
        Assert.Empty(plan.Blockers);
        KeepLaterStagingBuilder.Build(f.SourceDb, f.TargetDb, Output(f), plan);
        Assert.Equal(1L, Scalar(Output(f), "SELECT COUNT(*) FROM foreground_sessions WHERE duration_ms=0;"));
        Assert.Equal(1L, Scalar(Output(f), "SELECT COUNT(*) FROM system_events WHERE occurred_at='2026-11-22T01:00:00Z';"));
    }

    [Fact]
    public void MatchedAppsKeepBackupMetadataAndCurrentLaterRecordsUseBackupIdentity()
    {
        using var f = new CategoryImportFixture();
        Populate(f);
        f.Execute(f.TargetDb, @"UPDATE apps SET process_name='backup-app',executable_path='C:\backup.exe',primary_category_id=NULL,primary_category_source='none',user_alias='later alias';");
        var plan = Prepare(f);
        Assert.Empty(plan.Blockers);
        KeepLaterStagingBuilder.Build(f.SourceDb, f.TargetDb, Output(f), plan);
        Assert.Equal(1L, Scalar(Output(f), "SELECT COUNT(*) FROM apps;"));
        Assert.Equal(DBNull.Value, Scalar(Output(f), "SELECT user_alias FROM apps;"));
        Assert.Equal(2L, Scalar(Output(f), "SELECT COUNT(*) FROM foreground_sessions WHERE app_id=1;"));
        Assert.Equal(1L, Scalar(Output(f), "SELECT COUNT(*) FROM app_observations;"));
    }

    [Fact]
    public void ExistingOutputSidecarsAreNeverReusedOrDeleted()
    {
        using var f = new CategoryImportFixture();
        var plan = Prepare(f);
        var sidecar = Output(f) + "-wal";
        File.WriteAllText(sidecar, "unrelated journal");
        Assert.Throws<InvalidOperationException>(() => KeepLaterStagingBuilder.Build(f.SourceDb, f.TargetDb, Output(f), plan));
        Assert.False(File.Exists(Output(f)));
        Assert.Equal("unrelated journal", File.ReadAllText(sidecar));
    }

    [Fact]
    public void InputPathsCannotBeUsedAsOutput()
    {
        using var f = new CategoryImportFixture();
        var plan = Prepare(f);
        Assert.Throws<IOException>(() => KeepLaterStagingBuilder.Build(f.SourceDb, f.TargetDb, f.TargetDb, plan));
        Assert.Equal(plan.CurrentSha256, KeepLaterStagingBuilder.Hash(f.TargetDb));
    }
}
