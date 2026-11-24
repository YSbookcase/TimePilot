using Microsoft.Data.Sqlite;
using TimePilot.WinForms.KYS24;
using Xunit;

namespace TimePilot.Tests;

public sealed class CategoryImportPolicyTests
{
    [Fact]
    public void FilePriorityRequiresApprovalAndPreservesEverythingOutsideSelectedCategoryChanges()
    {
        using var f = new CategoryImportFixture();
        var app = f.PrepareApp();
        f.Target.SetAppPrimaryCategory(app, f.Category(f.TargetDb, "Game"));
        f.Target.SetAppUserAlias(app, "My alias");
        var other = f.AddApp(f.Target, "not-in-file", null);
        f.Target.SetAppPrimaryCategory(other, f.Category(f.TargetDb, "Game"));
        var records = f.RecordSnapshot();
        var identity = f.AppIdentitySnapshot();
        var settings = f.Hash(f.Settings);
        var source = f.Hash(f.Zip);
        var service = new CategoryImportService(f.TargetDb);
        var keep = service.Preview(f.Zip);
        Assert.Equal(0, keep.ReplacedApps);
        Assert.Contains(keep.Rows, r => r.Action == CategoryImportAction.KeepUser);
        var useFile = CategoryImportService.Replan(keep, new(true));
        Assert.Equal(1, useFile.ReplacedUserApps);
        Assert.Equal(1, useFile.ReplacedApps);
        var before = f.Hash(f.TargetDb);
        Assert.Throws<InvalidOperationException>(() => service.Apply(useFile, f.Now));
        var safety = Path.Combine(f.Root, "not-created.zip");
        Assert.Throws<InvalidOperationException>(() => service.ApplyWithSafetyBackup(useFile,
            new DataBackupService(Path.GetDirectoryName(f.TargetDb)!), safety, f.Now));
        Assert.False(File.Exists(safety));
        Assert.Equal(before, f.Hash(f.TargetDb));
        service.Apply(useFile, f.Now, overwriteApproved: true);
        Assert.Equal("Imported", f.Scalar("SELECT c.name FROM apps a JOIN app_categories c ON c.id=a.primary_category_id WHERE a.process_name='test';"));
        Assert.Equal("import", f.Scalar("SELECT primary_category_source FROM apps WHERE process_name='test';"));
        Assert.Equal("Game", f.Scalar("SELECT c.name FROM apps a JOIN app_categories c ON c.id=a.primary_category_id WHERE a.process_name='not-in-file';"));
        Assert.Equal(records, f.RecordSnapshot());
        Assert.Equal(identity, f.AppIdentitySnapshot());
        Assert.Equal(settings, f.Hash(f.Settings));
        Assert.Equal(source, f.Hash(f.Zip));
        Assert.False(CategoryImportService.Replan(service.Preview(f.Zip), new(true)).HasChanges);
    }

    [Theory]
    [InlineData(AppCategorySource.Recommendation)]
    [InlineData(AppCategorySource.Import)]
    [InlineData(AppCategorySource.System)]
    public void FilePriorityCanExplicitlyReplaceNonUserAssignmentsButDefaultKeepsThem(string source)
    {
        using var f = new CategoryImportFixture();
        var app = f.PrepareApp();
        f.Target.SetAppPrimaryCategory(app, f.Category(f.TargetDb, "Game"), source);
        var service = new CategoryImportService(f.TargetDb);
        var keep = service.Preview(f.Zip);
        Assert.Contains(keep.Rows, r => r.Action == CategoryImportAction.KeepCurrent);
        var useFile = CategoryImportService.Replan(keep, new(true));
        Assert.Equal(1, useFile.ReplacedApps);
        Assert.Equal(0, useFile.ReplacedUserApps);
        Assert.Contains(useFile.Rows, r => r.Action == CategoryImportAction.Replace);
        Assert.Throws<InvalidOperationException>(() => service.Apply(useFile, f.Now));
        service.Apply(useFile, f.Now, overwriteApproved: true);
        Assert.Equal("import", f.Scalar("SELECT primary_category_source FROM apps WHERE process_name='test';"));
    }

    [Theory]
    [InlineData("#010203")]
    [InlineData(null)]
    public void ColorsAreIndependentOptInIncludingClearingColorAndStillNeedApproval(string? color)
    {
        using var f = new CategoryImportFixture();
        var app = f.PrepareApp();
        f.Target.CreateCustomAppCategory("Imported", "#123456");
        f.Target.SetAppPrimaryCategory(app, f.Category(f.TargetDb, "Game"));
        f.Source.UpdateAppCategoryColor(f.Category(f.SourceDb, "Game"), color);
        f.BuildZip();
        var service = new CategoryImportService(f.TargetDb);
        var keep = service.Preview(f.Zip);
        Assert.False(keep.HasChanges);
        var colors = CategoryImportService.Replan(keep, new(false, true));
        Assert.Equal(1, colors.ChangedColors);
        Assert.Equal(0, colors.ReplacedApps);
        Assert.True(colors.RequiresOverwriteApproval);
        var row = Assert.Single(colors.Rows, r => r.Action == CategoryImportAction.UpdateColor);
        Assert.NotNull(row.CurrentColor);
        Assert.Equal(color, row.FileColor);
        Assert.Throws<InvalidOperationException>(() => service.Apply(colors, f.Now));
        service.Apply(colors, f.Now, overwriteApproved: true);
        var stored = f.Scalar("SELECT color FROM app_categories WHERE name='Game';");
        if (color is null) Assert.IsType<DBNull>(stored);
        else Assert.Equal(color, stored);
        Assert.Equal("user", f.Scalar("SELECT primary_category_source FROM apps WHERE process_name='test';"));
        Assert.Equal("system", f.Scalar("SELECT source FROM app_categories WHERE name='Game';"));
    }

    [Fact]
    public void EquivalentColorRepresentationsDoNotCauseOverwrites()
    {
        using var f = new CategoryImportFixture();
        f.Source.CreateCustomAppCategory("Shared", "Red");
        f.Target.CreateCustomAppCategory("Shared", "#FF0000");
        f.Execute(f.SourceDb, "UPDATE app_categories SET color='Red' WHERE name='Shared';");
        f.BuildZip();
        var plan = CategoryImportService.Replan(new CategoryImportService(f.TargetDb).Preview(f.Zip), new(true, true));
        Assert.Equal(0, plan.ChangedColors);
        Assert.False(plan.HasChanges);
    }

    [Fact]
    public void ApprovedFilePolicyStillRejectsStaleCurrentState()
    {
        using var f = new CategoryImportFixture();
        var app = f.PrepareApp();
        f.Target.SetAppPrimaryCategory(app, f.Category(f.TargetDb, "Game"));
        var service = new CategoryImportService(f.TargetDb);
        var plan = CategoryImportService.Replan(service.Preview(f.Zip), new(true, true));
        f.Target.UpdateAppCategoryColor(f.Category(f.TargetDb, "Game"), "#FFFFFF");
        var before = f.Hash(f.TargetDb);
        Assert.Throws<InvalidOperationException>(() => service.Apply(plan, f.Now, overwriteApproved: true));
        Assert.Equal(before, f.Hash(f.TargetDb));
    }

    [Fact]
    public void ReplacementFailureRollsBackColorsAndNewCategoriesTogether()
    {
        using var f = new CategoryImportFixture();
        var app = f.PrepareApp();
        f.Target.SetAppPrimaryCategory(app, f.Category(f.TargetDb, "Game"));
        f.Source.UpdateAppCategoryColor(f.Category(f.SourceDb, "Game"), "#010203");
        f.BuildZip();
        var originalColor = f.Scalar("SELECT color FROM app_categories WHERE name='Game';");
        f.Execute(f.TargetDb, "CREATE TRIGGER deny_replace BEFORE UPDATE OF primary_category_id ON apps BEGIN SELECT RAISE(ABORT, 'Synthetic failure'); END;");
        var service = new CategoryImportService(f.TargetDb);
        var plan = CategoryImportService.Replan(service.Preview(f.Zip), new(true, true));
        Assert.Equal(1, plan.ChangedColors);
        Assert.Throws<SqliteException>(() => service.Apply(plan, f.Now, overwriteApproved: true));
        Assert.Equal(originalColor, f.Scalar("SELECT color FROM app_categories WHERE name='Game';"));
        Assert.Equal(0L, f.Scalar("SELECT COUNT(*) FROM app_categories WHERE name='Imported';"));
        Assert.Equal("user", f.Scalar("SELECT primary_category_source FROM apps WHERE process_name='test';"));
    }

    [Fact]
    public void FilePriorityNeverOverridesPathConflictOrClearsUnclassifiedBackupEntry()
    {
        using var f = new CategoryImportFixture();
        var app = f.PrepareApp();
        f.Target.SetAppPrimaryCategory(app, f.Category(f.TargetDb, "Game"));
        f.AddApp(f.Source, "empty-in-file", null);
        var other = f.AddApp(f.Target, "empty-in-file", null);
        f.Target.SetAppPrimaryCategory(other, f.Category(f.TargetDb, "Game"));
        f.Execute(f.SourceDb, @"UPDATE apps SET executable_path='C:\source.exe' WHERE process_name='test';");
        f.Execute(f.TargetDb, @"UPDATE apps SET executable_path='D:\different.exe' WHERE process_name='test';");
        f.BuildZip();
        var service = new CategoryImportService(f.TargetDb);
        var plan = CategoryImportService.Replan(service.Preview(f.Zip), new(true));
        Assert.Equal(0, plan.ReplacedApps);
        Assert.Contains(plan.Rows, r => r.Action == CategoryImportAction.PathConflict);
        service.Apply(plan, f.Now);
        Assert.Equal(2L, f.Scalar("SELECT COUNT(*) FROM apps a JOIN app_categories c ON c.id=a.primary_category_id WHERE c.name='Game';"));
    }
}
