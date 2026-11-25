using TimePilot.WinForms.KYS24;
using Xunit;

namespace TimePilot.Tests;

public sealed class KeepLaterRestorePreparationTests
{
    internal static KeepLaterRestorePreview Preview()
    {
        var at = DateTimeOffset.Parse("2026-11-22T12:00:00Z");
        var backup = new RestorePreviewCounts(4, 0, 0, 0, 0, 0, at.AddHours(-1), at);
        var current = new RestorePreviewCounts(3, 2, 0, 0, 0, 0, at.AddHours(-1), at.AddHours(1));
        var zero = new RestorePreviewCounts(0, 0, 0, 0, 0, 0, null, null);
        return new(at, RestoreReferenceSource.Metadata, at.AddDays(1), at, at,
            Array.AsReadOnly(new[] { "foreground_sessions", "idle_sessions", "app_runtime_sessions", "process_runtime_sessions", "system_events" }
                .Select(name => new RestorePreviewTable(name, name == "foreground_sessions" ? backup : zero,
                    name == "foreground_sessions" ? current : zero, true, true)).ToArray()));
    }
    private static ImportCategory Category(long id = 1, string name = "Development", string? color = "#123456", bool builtin = true) => new(id, name, color, 10, builtin);
    private static ImportApp App(long id = 1, string process = "editor", string? path = @"C:\editor.exe", long? category = 1, string source = "user") => new(id, process, process, path, category, source, null);
    private static CategoryImportState State(ImportCategory[]? categories = null, ImportApp[]? apps = null) => new(categories ?? [Category()], apps ?? [App()]);
    private static KeepLaterRestorePreparation Build(CategoryImportState? backup = null, CategoryImportState? current = null, RestoreSettingsChoice settings = RestoreSettingsChoice.Unspecified) =>
        KeepLaterRestorePreparation.Build(Preview(), backup ?? State(), current ?? State(), "BACKUP-HASH", "CURRENT-HASH", settings, true);

    [Fact]
    public void SettingsChoiceIsExplicitAndPlanIsImmutableAndContentBound()
    {
        var plan = Build();
        Assert.Contains("settings-choice-required", plan.Blockers);
        var selected = plan.WithSettingsChoice(RestoreSettingsChoice.KeepCurrent);
        Assert.DoesNotContain("settings-choice-required", selected.Blockers);
        Assert.Contains("settings-choice-required", plan.Blockers);
        Assert.NotEqual(plan.PlanSha256, selected.PlanSha256);
        Assert.Equal(selected.PlanSha256, Build().WithSettingsChoice(RestoreSettingsChoice.KeepCurrent).PlanSha256);
        Assert.True(plan.MatchesSnapshotHashes("BACKUP-HASH", "CURRENT-HASH"));
        Assert.False(plan.MatchesSnapshotHashes("BACKUP-HASH", "CHANGED"));
        Assert.False(plan.MatchesSnapshotHashes("CHANGED", "CURRENT-HASH"));
        Assert.Throws<NotSupportedException>(() => ((IList<string>)plan.Blockers).Add("modified"));
        Assert.Throws<ArgumentOutOfRangeException>(() => plan.WithSettingsChoice((RestoreSettingsChoice)99));
        Assert.Contains("backup-settings-unvalidated", plan.WithSettingsChoice(RestoreSettingsChoice.UseBackup).Blockers);
    }

    [Fact]
    public void MissingSettingsCannotBeSelectedAsValidBackupSettings()
    {
        var plan = KeepLaterRestorePreparation.Build(Preview(), State(), State(), "A", "B", backupHasSettings: false);
        Assert.Contains("backup-settings-missing", plan.WithSettingsChoice(RestoreSettingsChoice.UseBackup).Blockers);
        Assert.DoesNotContain("backup-settings-missing", plan.WithSettingsChoice(RestoreSettingsChoice.KeepCurrent).Blockers);
    }

    [Fact]
    public void SourceScopedKeysAvoidCategoryAndAppIdCollisions()
    {
        var backup = State([Category()], [App()]);
        var current = State([Category(5, "개발"), Category(1, "Custom", null, false)],
            [App(5, category: 5), App(1, "new-app", null, 1)]);
        var plan = Build(backup, current, RestoreSettingsChoice.KeepCurrent);
        Assert.Equal("backup:1", plan.Categories.Single(c => c.CurrentId == 5).TargetKey);
        Assert.Equal("current:1", plan.Categories.Single(c => c.CurrentId == 1).TargetKey);
        Assert.Equal("backup:1", plan.Apps.Single(a => a.CurrentId == 5).TargetKey);
        Assert.Equal("current:1", plan.Apps.Single(a => a.CurrentId == 1).TargetKey);
        var records = Assert.Single(plan.Records, r => r.Table == "foreground_sessions");
        Assert.Equal(4, records.BackupBefore);
        Assert.Equal(2, records.CurrentAtOrAfter);
        Assert.Equal(3, records.CurrentBeforeReplaced);
        Assert.False(plan.HasUnresolvedIssues);
        current.Categories[0] = Category(9, "Changed");
        Assert.Equal("개발", plan.Categories.Single(c => c.CurrentId == 5).Name);
    }

    [Theory]
    [InlineData("path")]
    [InlineData("user")]
    [InlineData("builtin")]
    [InlineData("name-collision")]
    public void ConflictingIdentitiesAreBlockedInsteadOfSilentlyDroppingRecords(string kind)
    {
        var current = kind switch
        {
            "path" => State(apps: [App(path: @"D:\different.exe")]),
            "user" => State([Category(), Category(2, "My category", null, false)], [App(category: 2)]),
            "builtin" => State([Category(name: "Unknown builtin")], [App()]),
            _ => State([Category(name: "Development", builtin: false)], [App()])
        };
        var plan = Build(current: current);
        Assert.True(plan.HasUnresolvedIssues);
        Assert.Contains(plan.Blockers, b => b is "app-path-conflict" or "user-category-conflict" or "category-identity-conflict");
    }

    [Fact]
    public void NonUserAssignmentAndColorDifferencesAreExplicitBackupPriorityWarnings()
    {
        var current = State([Category(color: "#FFFFFF"), Category(2, "Custom", null, false)], [App(category: 2, source: "recommendation")]);
        var plan = Build(current: current, settings: RestoreSettingsChoice.KeepCurrent);
        Assert.Contains("category-color-backup-priority", plan.Warnings);
        Assert.Contains("app-category-backup-priority", plan.Warnings);
        Assert.Equal(RestoreMappingAction.Match, Assert.Single(plan.Apps).Action);
        Assert.DoesNotContain("user-category-conflict", plan.Blockers);
    }

    [Fact]
    public void FreshSnapshotAfterCurrentChangesCannotMatchEarlierPreparation()
    {
        using var f = new CategoryImportFixture();
        f.PrepareApp();
        var service = new DataBackupService(Path.GetDirectoryName(f.TargetDb)!);
        var first = service.InspectBackupDetailedComparison(f.Zip, f.Now.AddDays(1))!.Preparation!;
        // Change actual identity metadata independently of session recording.
        f.Execute(f.TargetDb, "UPDATE apps SET user_alias='Updated alias' WHERE process_name='test';");
        var second = service.InspectBackupDetailedComparison(f.Zip, f.Now.AddDays(1))!.Preparation!;
        Assert.Equal(first.BackupSha256, second.BackupSha256);
        Assert.NotEqual(first.CurrentSha256, second.CurrentSha256);
        Assert.False(first.MatchesSnapshotHashes(second.BackupSha256, second.CurrentSha256));
        Assert.NotEqual(first.PlanSha256, second.PlanSha256);
    }

    [Fact]
    public void UnsafeRecordBucketsAndInferredReferenceRemainBlockedRegardlessOfSettings()
    {
        var preview = Preview();
        var table = preview.Tables[0];
        var unsafeCounts = table.Backup with { Crossing = 1, Open = 1, Invalid = 1 };
        preview = preview with
        {
            ReferenceSource = RestoreReferenceSource.LastObservation,
            BackupLastObservation = preview.ReferenceAt!.Value.AddSeconds(1),
            Tables = [table with { Backup = unsafeCounts, CurrentAvailable = false }]
        };
        var plan = KeepLaterRestorePreparation.Build(preview, null, null, "A", "B").WithSettingsChoice(RestoreSettingsChoice.KeepCurrent);
        foreach (var code in new[] { "reference-unconfirmed", "backup-after-reference", "crossing-sessions", "open-sessions", "invalid-records", "missing-record-schema", "identity-schema-unavailable" })
            Assert.Contains(code, plan.Blockers);
    }
}
