using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using TimePilot.WinForms.KYS24;
using Xunit;

namespace TimePilot.Tests;

public sealed class CategoryImportServiceTests
{
    [Fact]
    public void PreviewAndApplyPreserveRecordsSettingsUserChoicesAndSourceBackup()
    {
        using var f = new CategoryImportFixture();
        var custom = f.Source.CreateCustomAppCategory("Deep work", "Red");
        var shared = f.Source.CreateCustomAppCategory("Shared", "#111111");
        f.Target.CreateCustomAppCategory("Shared", "#ABCDEF");
        f.Execute(f.SourceDb, "UPDATE app_categories SET name = '개발' WHERE name = 'Development';");
        foreach (var process in new[] { "empty", "user", "current", "shared", "builtin", "path" })
        {
            var sourceId = f.AddApp(f.Source, process, @"C:\Apps\" + process + ".exe");
            var targetId = f.AddApp(f.Target, process, process == "path" ? @"D:\Other.exe" : @"C:\Apps\" + process + ".exe");
            var sourceCategory = process == "shared" ? shared : process == "builtin" ? f.Category(f.SourceDb, "개발") : custom;
            f.Source.SetAppPrimaryCategory(sourceId, sourceCategory);
            if (process == "user") f.Target.SetAppPrimaryCategory(targetId, f.Category(f.TargetDb, "Game"));
            if (process == "current") f.Target.SetAppPrimaryCategory(targetId, f.Category(f.TargetDb, "Game"), AppCategorySource.Recommendation);
            f.Target.SetAppUserAlias(targetId, "My " + process);
        }
        f.Source.SetAppPrimaryCategory(f.AddApp(f.Source, "missing", @"C:\missing.exe"), custom);
        f.BuildZip();
        var original = f.Hash(f.Zip);
        var settings = f.Hash(f.Settings);
        var records = f.RecordSnapshot();
        var appIdentity = f.AppIdentitySnapshot();
        var targetBefore = f.Hash(f.TargetDb);
        var service = new CategoryImportService(f.TargetDb);
        var plan = service.Preview(f.Zip);
        Assert.Equal(targetBefore, f.Hash(f.TargetDb));
        Assert.Equal(1, plan.AddedCategories);
        Assert.Equal(3, plan.AssignedApps);
        Assert.Contains(plan.Rows, r => r.Action == CategoryImportAction.KeepUser);
        Assert.Contains(plan.Rows, r => r.Action == CategoryImportAction.KeepCurrent);
        Assert.Contains(plan.Rows, r => r.Action == CategoryImportAction.PathConflict);
        Assert.Contains(plan.Rows, r => r.Action == CategoryImportAction.MissingApp);
        service.Apply(plan, f.Now);
        Assert.Equal(records, f.RecordSnapshot());
        Assert.Equal(appIdentity, f.AppIdentitySnapshot());
        Assert.Equal(settings, f.Hash(f.Settings));
        Assert.Equal(original, f.Hash(f.Zip));
        Assert.Equal("import", f.Scalar("SELECT source FROM app_categories WHERE name = 'Deep work';"));
        Assert.Equal("#ABCDEF", f.Scalar("SELECT color FROM app_categories WHERE name = 'Shared';"));
        Assert.Equal("import", f.Scalar("SELECT primary_category_source FROM apps WHERE process_name = 'empty';"));
        Assert.Equal("user", f.Scalar("SELECT primary_category_source FROM apps WHERE process_name = 'user';"));
        Assert.Equal("recommendation", f.Scalar("SELECT primary_category_source FROM apps WHERE process_name = 'current';"));
        Assert.Equal("Development", f.Scalar("SELECT c.name FROM apps a JOIN app_categories c ON a.primary_category_id=c.id WHERE a.process_name='builtin';"));
        Assert.Equal("none", f.Scalar("SELECT primary_category_source FROM apps WHERE process_name = 'path';"));
        var second = service.Preview(f.Zip);
        Assert.Equal(0, second.AddedCategories);
        Assert.Equal(0, second.AssignedApps);
    }

    [Fact]
    public void StalePreviewRollsBackAndNeverReplacesNewUserAssignment()
    {
        using var f = new CategoryImportFixture();
        var id = f.PrepareApp();
        var service = new CategoryImportService(f.TargetDb);
        var plan = service.Preview(f.Zip);
        f.Target.SetAppPrimaryCategory(id, f.Category(f.TargetDb, "Game"));
        var before = f.Hash(f.TargetDb);
        Assert.Throws<InvalidOperationException>(() => service.Apply(plan, f.Now));
        Assert.Equal(before, f.Hash(f.TargetDb));
        Assert.Equal(0L, f.Scalar("SELECT COUNT(*) FROM app_categories WHERE name='Imported';"));
    }

    [Fact]
    public void SqlFailureRollsBackNewCategoriesAndAssignments()
    {
        using var f = new CategoryImportFixture();
        f.PrepareApp();
        f.Execute(f.TargetDb, "CREATE TRIGGER deny_import BEFORE UPDATE OF primary_category_id ON apps BEGIN SELECT RAISE(ABORT, 'Synthetic failure'); END;");
        var service = new CategoryImportService(f.TargetDb);
        var plan = service.Preview(f.Zip);
        Assert.Throws<SqliteException>(() => service.Apply(plan, f.Now));
        Assert.Equal(0L, f.Scalar("SELECT COUNT(*) FROM app_categories WHERE name='Imported';"));
        Assert.Equal("none", f.Scalar("SELECT primary_category_source FROM apps WHERE process_name='test';"));
    }

    [Fact]
    public void PreviewUsesFrozenBackupAndIgnoresOnlyRecordingTimestampChanges()
    {
        using var f = new CategoryImportFixture();
        f.PrepareApp();
        var service = new CategoryImportService(f.TargetDb);
        var plan = service.Preview(f.Zip);
        File.Delete(f.Zip);
        f.Execute(f.TargetDb, "UPDATE apps SET last_seen_at='2026-11-22T12:00:00Z';");
        service.Apply(plan, f.Now);
        Assert.Equal("import", f.Scalar("SELECT primary_category_source FROM apps WHERE process_name='test';"));
    }

    [Theory]
    [InlineData("Game")]
    [InlineData("게임")]
    public void CategoryNameCollisionWithBuiltinIsSkippedAndUnknownBuiltinIsNotAdded(string categoryName)
    {
        using var f = new CategoryImportFixture();
        f.Execute(f.SourceDb, "DELETE FROM app_categories WHERE name='Game';");
        var custom = f.Source.CreateCustomAppCategory(categoryName, null);
        f.Source.SetAppPrimaryCategory(f.AddApp(f.Source, "test", null), custom);
        f.AddApp(f.Target, "test", null);
        f.Execute(f.SourceDb, "UPDATE app_categories SET name='Future builtin' WHERE name='Development';");
        f.BuildZip();
        var plan = new CategoryImportService(f.TargetDb).Preview(f.Zip);
        Assert.Equal(0, plan.AddedCategories);
        Assert.Equal(0, plan.AssignedApps);
        Assert.Contains(plan.Rows, r => r.Backup == "Future builtin" && r.Action == CategoryImportAction.UnsupportedCategory);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("duplicate")]
    [InlineData("corrupt")]
    [InlineData("schema")]
    [InlineData("reference")]
    public void InvalidBackupCannotChangeCurrentData(string kind)
    {
        using var f = new CategoryImportFixture();
        f.PrepareApp();
        if (kind == "schema") f.Execute(f.SourceDb, "PRAGMA foreign_keys=OFF; DROP TABLE app_categories;");
        if (kind == "reference") f.Execute(f.SourceDb, "PRAGMA foreign_keys=OFF; UPDATE apps SET primary_category_id=999999;");
        if (kind is "schema" or "reference") f.BuildZip();
        if (kind is "missing" or "duplicate" or "corrupt")
        {
            File.Delete(f.Zip);
            using var archive = ZipFile.Open(f.Zip, ZipArchiveMode.Create);
            if (kind == "corrupt")
            {
                using var writer = new StreamWriter(archive.CreateEntry("timepilot.db").Open());
                writer.Write("Not a database");
            }
            if (kind == "duplicate")
            {
                archive.CreateEntryFromFile(f.SourceDb, "timepilot.db");
                archive.CreateEntryFromFile(f.SourceDb, "timepilot.db");
            }
        }
        var before = f.Hash(f.TargetDb);
        Assert.ThrowsAny<Exception>(() => new CategoryImportService(f.TargetDb).Preview(f.Zip));
        Assert.Equal(before, f.Hash(f.TargetDb));
    }

    [Fact]
    public void LegacyBackupWithoutCategoryProvenanceIsAcceptedAndExplicitUserNoneIsKept()
    {
        using var f = new CategoryImportFixture();
        f.PrepareApp();
        f.Execute(f.SourceDb, "ALTER TABLE app_categories DROP COLUMN source;");
        f.Execute(f.TargetDb, "UPDATE apps SET primary_category_source='user' WHERE process_name='test';");
        f.BuildZip();
        var service = new CategoryImportService(f.TargetDb);
        var plan = service.Preview(f.Zip);
        Assert.Equal(0, plan.AssignedApps);
        Assert.Contains(plan.Rows, r => r.Action == CategoryImportAction.KeepUser);
        service.Apply(plan, f.Now);
        Assert.Equal("user", f.Scalar("SELECT primary_category_source FROM apps WHERE process_name='test';"));
        f.Target.Initialize(f.Now, f.Now.AddHours(-1));
        Assert.Equal("import", f.Scalar("SELECT source FROM app_categories WHERE name='Imported';"));
    }

    [Fact]
    public void SafetyBackupFailureDoesNotApplyAndSuccessfulBackupContainsOriginalCategories()
    {
        using var f = new CategoryImportFixture();
        f.PrepareApp();
        var service = new CategoryImportService(f.TargetDb);
        var plan = service.Preview(f.Zip);
        var data = Path.GetDirectoryName(f.TargetDb)!;
        var safety = Path.Combine(f.Root, "safety.zip");
        File.Delete(f.Settings);
        Assert.Throws<InvalidDataException>(() => service.ApplyWithSafetyBackup(plan, new DataBackupService(data), safety, f.Now));
        Assert.Equal(0L, f.Scalar("SELECT COUNT(*) FROM app_categories WHERE name='Imported';"));
        File.WriteAllText(f.Settings, "{}");
        service.ApplyWithSafetyBackup(plan, new DataBackupService(data), safety, f.Now);
        Assert.True(new DataBackupService(data).InspectBackup(safety).HasSettings);
        var safetyPlan = service.Preview(safety);
        Assert.DoesNotContain(safetyPlan.Backup.Categories, c => c.Name == "Imported");
        Assert.Equal("import", f.Scalar("SELECT primary_category_source FROM apps WHERE process_name='test';"));
    }

    [Fact]
    public void MigrationAddsProvenanceWithoutChangingExistingCategoriesOrRecords()
    {
        using var f = new CategoryImportFixture();
        var custom = f.Target.CreateCustomAppCategory("Old custom", null);
        var records = f.RecordSnapshot();
        f.Execute(f.TargetDb, "ALTER TABLE app_categories DROP COLUMN source;");
        f.Target.Initialize(f.Now, f.Now.AddHours(-1));
        Assert.Equal(records, f.RecordSnapshot());
        Assert.Equal("user", f.Scalar("SELECT source FROM app_categories WHERE id=" + custom));
        Assert.Equal("system", f.Scalar("SELECT source FROM app_categories WHERE name='Development';"));
    }
}

internal sealed class CategoryImportFixture : IDisposable
{
    public string Root { get; } = Path.Combine(Path.GetTempPath(), "TimePilot-category-tests-" + Guid.NewGuid().ToString("N"));
    public DateTimeOffset Now { get; } = DateTimeOffset.Parse("2026-11-22T00:00:00Z");
    public string SourceDb => Path.Combine(Root, "source", "timepilot.db");
    public string TargetDb => Path.Combine(Root, "target", "timepilot.db");
    public string Settings => Path.Combine(Root, "target", "settings.json");
    public string Zip => Path.Combine(Root, "backup.zip");
    public TimePilotStorage Source { get; }
    public TimePilotStorage Target { get; }

    public CategoryImportFixture()
    {
        Source = new(SourceDb);
        Target = new(TargetDb);
        Source.Initialize(Now, Now.AddHours(-1));
        Target.Initialize(Now, Now.AddHours(-1));
        File.WriteAllText(Settings, "{\"UiLanguage\":1}");
        Target.RecordSystemEvent("test", Now, Now.AddHours(-1), "Synthetic");
    }

    public long PrepareApp()
    {
        var category = Source.CreateCustomAppCategory("Imported", "#123456");
        Source.SetAppPrimaryCategory(AddApp(Source, "test", null), category);
        var id = AddApp(Target, "test", null);
        BuildZip();
        return id;
    }

    public long AddApp(TimePilotStorage storage, string process, string? path)
    {
        var session = storage.StartForegroundSession(new(process, process, path), Now);
        storage.EndForegroundSession(session, Now.AddSeconds(10));
        var database = ReferenceEquals(storage, Source) ? SourceDb : TargetDb;
        return (long)ScalarFrom(database, "SELECT id FROM apps WHERE process_name=$process", process)!;
    }

    public void BuildZip()
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(Zip)) File.Delete(Zip);
        using var archive = ZipFile.Open(Zip, ZipArchiveMode.Create);
        archive.CreateEntryFromFile(SourceDb, "timepilot.db");
        using var writer = new StreamWriter(archive.CreateEntry("settings.json").Open());
        writer.Write("{\"UiLanguage\":0}");
    }
    public long Category(string database, string name) => (long)ScalarFrom(database, "SELECT id FROM app_categories WHERE name=$process", name)!;
    public object? Scalar(string sql) => ScalarFrom(TargetDb, sql);
    private static object? ScalarFrom(string database, string sql, string? parameter = null)
    {
        using var connection = Connect(database);
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        if (parameter is not null) command.Parameters.AddWithValue("$process", parameter);
        return command.ExecuteScalar();
    }
    public void Execute(string database, string sql)
    {
        using var connection = Connect(database);
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
    private static SqliteConnection Connect(string database)
    {
        var c = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = database, Pooling = false }.ToString());
        c.Open();
        return c;
    }
    public string Hash(string path)
    {
        using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        return Convert.ToHexString(SHA256.HashData(file));
    }
    public string RecordSnapshot() => string.Join("\n", new[] { "foreground_sessions", "idle_sessions", "process_runtime_sessions", "app_runtime_sessions", "system_events", "app_observations" }
        .Select(table => Snapshot("SELECT * FROM " + table + " ORDER BY id")));
    public string AppIdentitySnapshot() => Snapshot("SELECT id, process_name, display_name, executable_path, first_seen_at, last_seen_at, user_alias, is_excluded, recommended_category_id, recommended_category_reason, recommended_category_confidence, recommended_category_updated_at FROM apps ORDER BY id;");
    private string Snapshot(string sql)
    {
        using var c = Connect(TargetDb);
        using var command = c.CreateCommand();
        command.CommandText = sql;
        using var reader = command.ExecuteReader();
        var rows = new List<object[]>();
        while (reader.Read())
        {
            var values = new object[reader.FieldCount];
            reader.GetValues(values);
            rows.Add(values.Select(v => v is DBNull ? null! : v).ToArray());
        }
        return JsonSerializer.Serialize(rows);
    }
    public void Dispose()
    {
        Source.Dispose();
        Target.Dispose();
        SqliteConnection.ClearAllPools();
        if (!Path.GetFullPath(Root).StartsWith(Path.GetFullPath(Path.GetTempPath()), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Invalid test root");
        Directory.Delete(Root, true);
    }
}
