using System.Text.Json;
using TimePilot.WinForms.KYS24;
using Xunit;

namespace TimePilot.Tests;

public sealed class KeepLaterRestoreWorkspaceTests
{
    private static string Run(CategoryImportFixture f, KeepLaterRestorePreparation plan,
        CancellationToken token = default, IProgress<RestoreWorkspaceState>? progress = null) =>
        KeepLaterRestoreWorkspace.Prepare(f.SourceDb, f.TargetDb, f.Settings, f.Root, plan, f.Now.AddDays(2), token, progress);

    private static string Root(Exception error) => Assert.IsType<string>(error.Data["RestoreWorkspacePath"]);

    [Fact]
    public void ReadyWorkspaceContainsRecoverableBackupAndFrozenInputsWithoutChangingSources()
    {
        using var f = new CategoryImportFixture();
        KeepLaterStagingBuilderTests.Populate(f);
        var plan = KeepLaterStagingBuilderTests.Prepare(f);
        var settingsHash = f.Hash(f.Settings);
        var stages = new List<RestoreWorkspaceState>();
        var root = Run(f, plan, progress: new InlineProgress(stages.Add));
        Assert.Equal([RestoreWorkspaceState.Preparing, RestoreWorkspaceState.SafetyVerified, RestoreWorkspaceState.Ready], stages);
        Assert.Equal(new(RestoreWorkspaceState.Ready, true, true), KeepLaterRestoreWorkspace.Inspect(root));
        Assert.Equal(plan.BackupSha256, f.Hash(f.SourceDb));
        Assert.Equal(plan.CurrentSha256, f.Hash(f.TargetDb));
        Assert.Equal(settingsHash, f.Hash(f.Settings));

        var service = new DataBackupService(root);
        var backup = service.InspectBackup(Path.Combine(root, "safety.zip"));
        Assert.True(backup.HasSettings);
        Assert.Equal(f.Now.AddDays(2), backup.CreatedAt);
        // Restore only into a fresh synthetic directory, never onto either input.
        var restored = Path.Combine(f.Root, "restored");
        new DataBackupService(restored).RestoreBackup(Path.Combine(root, "safety.zip"));
        Assert.Equal(plan.CurrentSha256, f.Hash(Path.Combine(restored, "timepilot.db")));
        Assert.Equal(settingsHash, f.Hash(Path.Combine(restored, "settings.json")));
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "operation.json")));
        Assert.Equal(plan.PlanSha256, manifest.RootElement.GetProperty("PlanSha256").GetString());
        Assert.DoesNotContain(f.Root.Replace("\\", "\\\\"), manifest.RootElement.GetRawText());
    }

    [Fact]
    public void FailureAfterSafetyPreservesBackupAndDoesNotClaimResultReady()
    {
        using var f = new CategoryImportFixture();
        KeepLaterStagingBuilderTests.Populate(f);
        f.Execute(f.TargetDb, "UPDATE app_runtime_sessions SET started_at='2026-11-22T00:00:00Z',ended_at='2026-11-22T00:00:10Z',last_heartbeat_at='2026-11-22T00:00:10Z';");
        var plan = KeepLaterStagingBuilderTests.Prepare(f);
        var error = Assert.Throws<InvalidDataException>(() => Run(f, plan));
        var root = Root(error);
        Assert.Equal(new(RestoreWorkspaceState.Failed, true, false), KeepLaterRestoreWorkspace.Inspect(root));
        Assert.False(File.Exists(Path.Combine(root, "result.db")));
        Assert.Equal(plan.CurrentSha256, f.Hash(f.TargetDb));
        Assert.True(new DataBackupService(root).InspectBackup(Path.Combine(root, "safety.zip")).HasSettings);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    public void CancellationPersistsHonestStateAndRetainsVerifiedSafety(int cancelAt, bool safety)
    {
        using var f = new CategoryImportFixture();
        var plan = KeepLaterStagingBuilderTests.Prepare(f);
        using var cancellation = new CancellationTokenSource();
        var error = Assert.Throws<OperationCanceledException>(() => Run(f, plan, cancellation.Token,
            new InlineProgress(state => { if ((int)state == cancelAt) cancellation.Cancel(); })));
        var root = Root(error);
        Assert.Equal(new(RestoreWorkspaceState.Cancelled, safety, false), KeepLaterRestoreWorkspace.Inspect(root));
        Assert.False(File.Exists(Path.Combine(root, "result.db")));
    }

    [Fact]
    public void PreCancelledOrStaleInputsDoNotCreateWorkspace()
    {
        using var f = new CategoryImportFixture();
        var plan = KeepLaterStagingBuilderTests.Prepare(f);
        Assert.Throws<OperationCanceledException>(() => Run(f, plan, new CancellationToken(true)));
        f.Execute(f.TargetDb, "DELETE FROM system_events;");
        Assert.Throws<InvalidOperationException>(() => Run(f, plan));
        Assert.Empty(Directory.GetDirectories(f.Root, "ActiveLogbook-restore-*"));
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("[]")]
    public void InvalidSettingsCannotBecomeSafetyBackup(string json)
    {
        using var f = new CategoryImportFixture();
        var plan = KeepLaterStagingBuilderTests.Prepare(f);
        File.WriteAllText(f.Settings, json);
        Assert.ThrowsAny<Exception>(() => Run(f, plan));
        Assert.Empty(Directory.GetDirectories(f.Root, "ActiveLogbook-restore-*"));
    }

    [Theory]
    [InlineData("result.db")]
    [InlineData("safety.zip")]
    [InlineData("current/settings.json")]
    [InlineData("backup.db")]
    public void InspectionRejectsTamperedEvidenceWithoutDeletingAnything(string relative)
    {
        using var f = new CategoryImportFixture();
        var root = Run(f, KeepLaterStagingBuilderTests.Prepare(f));
        var path = Path.Combine(root, relative);
        File.AppendAllText(path, "changed");
        Assert.ThrowsAny<Exception>(() => KeepLaterRestoreWorkspace.Inspect(root));
        Assert.True(File.Exists(path));
        Assert.True(File.Exists(Path.Combine(root, "safety.zip")));
    }

    [Fact]
    public void InvalidCurrentSchemaFailsBeforeSafetyIsDeclaredVerified()
    {
        using var f = new CategoryImportFixture();
        f.Execute(f.TargetDb, "ALTER TABLE apps ADD COLUMN unsupported TEXT;");
        var plan = KeepLaterStagingBuilderTests.Prepare(f);
        var error = Assert.Throws<InvalidDataException>(() => Run(f, plan));
        var root = Root(error);
        Assert.Equal(new(RestoreWorkspaceState.Failed, false, false), KeepLaterRestoreWorkspace.Inspect(root));
        Assert.False(File.Exists(Path.Combine(root, "safety.zip")));
    }

    [Theory]
    [InlineData("version")]
    [InlineData("hash")]
    [InlineData("state")]
    public void IncompleteManifestCannotAuthorizeReadyResult(string kind)
    {
        using var f = new CategoryImportFixture();
        var root = Run(f, KeepLaterStagingBuilderTests.Prepare(f));
        var path = Path.Combine(root, "operation.json");
        var manifest = JsonSerializer.Deserialize<RestoreWorkspaceManifest>(File.ReadAllText(path))!;
        var invalid = kind switch
        {
            "version" => manifest with { Version = 999 },
            "hash" => manifest with { PlanSha256 = "" },
            _ => manifest with { SafetySha256 = null }
        };
        File.WriteAllText(path, JsonSerializer.Serialize(invalid));
        Assert.Throws<InvalidDataException>(() => KeepLaterRestoreWorkspace.Inspect(root));
        Assert.True(File.Exists(Path.Combine(root, "safety.zip")));
    }

    [Fact]
    public void InterruptedWorkspaceIsNotAutomaticallyResumed()
    {
        using var f = new CategoryImportFixture();
        var plan = KeepLaterStagingBuilderTests.Prepare(f);
        var root = Run(f, plan);
        var path = Path.Combine(root, "operation.json");
        var manifest = JsonSerializer.Deserialize<RestoreWorkspaceManifest>(File.ReadAllText(path))!;
        File.WriteAllText(path, JsonSerializer.Serialize(manifest with { State = RestoreWorkspaceState.SafetyVerified, ResultSha256 = null }));
        var before = f.Hash(path);
        Assert.Equal(new(RestoreWorkspaceState.SafetyVerified, true, false), KeepLaterRestoreWorkspace.Inspect(root));
        Assert.Equal(before, f.Hash(path));
        Assert.True(File.Exists(Path.Combine(root, "result.db")));
    }

    private sealed class InlineProgress(Action<RestoreWorkspaceState> report) : IProgress<RestoreWorkspaceState>
    {
        public void Report(RestoreWorkspaceState value) => report(value);
    }
}
