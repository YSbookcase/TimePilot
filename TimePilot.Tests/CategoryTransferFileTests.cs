using System.Text.Json;
using System.Text.Json.Nodes;
using System.IO.Compression;
using TimePilot.WinForms.KYS24;
using Xunit;

namespace TimePilot.Tests;

public sealed class CategoryTransferFileTests
{
    [Fact]
    public void ExportHasOnlyWhitelistedFieldsAndRoundTripsWithoutRecordsSettingsOrPrivatePaths()
    {
        using var f = new CategoryImportFixture();
        f.PrepareApp();
        f.Execute(f.SourceDb, """
            PRAGMA foreign_keys=OFF;
            UPDATE app_categories SET id=5000 WHERE name='Imported';
            UPDATE apps SET primary_category_id=5000, display_name='Private display', user_alias='Private alias',
                executable_path='C:\Private\Person\test.exe' WHERE process_name='test';
            """);
        f.Execute(f.TargetDb, @"UPDATE apps SET executable_path='D:\Other\test.exe' WHERE process_name='test';");
        var source = f.Hash(f.SourceDb);
        var records = f.RecordSnapshot();
        var settings = f.Hash(f.Settings);
        var path = Path.Combine(f.Root, "categories.json");
        CategoryTransferFile.Export(f.SourceDb, path, f.Now);
        Assert.Equal(source, f.Hash(f.SourceDb));
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var root = document.RootElement;
        Assert.Equal(new[] { "Assignments", "Categories", "ExportedAtUtc", "Format", "Version" },
            root.EnumerateObject().Select(p => p.Name).OrderBy(n => n));
        Assert.Equal(CategoryTransferFile.Format, root.GetProperty("Format").GetString());
        Assert.Equal(1, root.GetProperty("Version").GetInt32());
        foreach (var category in root.GetProperty("Categories").EnumerateArray())
        {
            Assert.Equal(new[] { "Color", "Id", "IsBuiltin", "Name", "SortOrder" },
                category.EnumerateObject().Select(p => p.Name).OrderBy(n => n));
            Assert.NotEqual(5000, category.GetProperty("Id").GetInt64());
        }
        var assignment = Assert.Single(root.GetProperty("Assignments").EnumerateArray());
        Assert.Equal(new[] { "CategoryId", "ProcessName" }, assignment.EnumerateObject().Select(p => p.Name).OrderBy(n => n));
        Assert.Equal("test", assignment.GetProperty("ProcessName").GetString());
        var json = File.ReadAllText(path);
        Assert.DoesNotContain("Private", json);
        Assert.DoesNotContain("UiLanguage", json);
        Assert.DoesNotContain("started_at", json);
        Assert.DoesNotContain("duration", json);
        var service = new CategoryImportService(f.TargetDb);
        var plan = service.Preview(path);
        Assert.True(plan.ProcessNameOnly);
        Assert.Equal(1, plan.AddedCategories);
        Assert.Equal(1, plan.AssignedApps);
        service.ApplyWithSafetyBackup(plan, new DataBackupService(Path.GetDirectoryName(f.TargetDb)!), Path.Combine(f.Root, "safety.zip"), f.Now);
        Assert.Equal(records, f.RecordSnapshot());
        Assert.Equal(settings, f.Hash(f.Settings));
        Assert.Equal("import", f.Scalar("SELECT primary_category_source FROM apps WHERE process_name='test';"));
        Assert.Equal(0, service.Preview(path).AssignedApps);
    }

    [Fact]
    public void ExportNeverOverwritesExistingFilesAndValidationFailurePublishesNothing()
    {
        using var f = new CategoryImportFixture();
        var path = Path.Combine(f.Root, "existing.json");
        File.WriteAllText(path, "Keep this file");
        Assert.Throws<IOException>(() => CategoryTransferFile.Export(f.SourceDb, path, f.Now));
        Assert.Equal("Keep this file", File.ReadAllText(path));
        var failed = Path.Combine(f.Root, "invalid.json");
        f.Execute(f.SourceDb, "UPDATE app_categories SET name='' WHERE name='Game';");
        Assert.Throws<InvalidDataException>(() => CategoryTransferFile.Export(f.SourceDb, failed, f.Now));
        Assert.False(File.Exists(failed));
        Assert.Empty(Directory.GetFiles(f.Root, "*.tmp"));
    }

    [Theory]
    [InlineData("version")]
    [InlineData("format")]
    [InlineData("null")]
    [InlineData("reference")]
    [InlineData("duplicate")]
    [InlineData("extra")]
    [InlineData("missing")]
    public void InvalidOrUnknownJsonIsRejectedWithoutChangingCurrentData(string change)
    {
        using var f = new CategoryImportFixture();
        f.PrepareApp();
        var path = Path.Combine(f.Root, "categories.json");
        CategoryTransferFile.Export(f.SourceDb, path, f.Now);
        var root = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
        switch (change)
        {
            case "version": root["Version"] = 2; break;
            case "format": root["Format"] = "other-data"; break;
            case "null": root["Categories"]!.AsArray().Add((JsonNode?)null); break;
            case "reference": root["Assignments"]![0]!["CategoryId"] = 90000; break;
            case "duplicate": root["Assignments"]!.AsArray().Add(root["Assignments"]![0]!.DeepClone()); break;
            case "extra": root["ExecutablePath"] = "Do not accept unknown fields"; break;
            case "missing": root.Remove("Categories"); break;
        }
        File.WriteAllText(path, root.ToJsonString());
        var before = f.Hash(f.TargetDb);
        Assert.ThrowsAny<Exception>(() => new CategoryImportService(f.TargetDb).Preview(path));
        Assert.Equal(before, f.Hash(f.TargetDb));
    }

    [Fact]
    public void RawExportRemainsDiagnosticCsvRatherThanBackupOrCategoryTransfer()
    {
        using var f = new CategoryImportFixture();
        f.PrepareApp();
        var path = Path.Combine(f.Root, "raw-data.zip");
        new RawDataZipExporter(f.Target).Export(path);
        using (var archive = ZipFile.OpenRead(path))
        {
            Assert.NotNull(archive.GetEntry("README.txt"));
            Assert.NotNull(archive.GetEntry("apps.csv"));
            Assert.NotNull(archive.GetEntry("foreground_sessions.csv"));
            Assert.Null(archive.GetEntry("timepilot.db"));
            Assert.Null(archive.GetEntry("settings.json"));
            using var reader = new StreamReader(archive.GetEntry("app_categories.csv")!.Open());
            Assert.Contains("source", reader.ReadLine());
        }
        Assert.Throws<InvalidDataException>(() => new DataBackupService(Path.GetDirectoryName(f.TargetDb)!).InspectBackup(path));
        Assert.Throws<InvalidDataException>(() => new CategoryImportService(f.TargetDb).Preview(path));
    }

    [Fact]
    public void OversizedJsonIsRejectedBeforeParsing()
    {
        using var f = new CategoryImportFixture();
        var path = Path.Combine(f.Root, "large.json");
        using (var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write))
            output.SetLength(CategoryTransferFile.MaximumBytes + 1);
        Assert.Throws<InvalidDataException>(() => CategoryTransferFile.Read(path));
    }
}
