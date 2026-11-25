using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

namespace TimePilot.WinForms.KYS24;

internal enum RestoreWorkspaceState { Preparing, SafetyVerified, Ready, Failed, Cancelled }
internal sealed record RestoreWorkspaceManifest(int Version, RestoreWorkspaceState State, DateTimeOffset CreatedAt,
    string PlanSha256, string BackupSha256, string CurrentSha256, string SettingsSha256,
    string? SafetySha256 = null, string? ResultSha256 = null);
internal sealed record RestoreWorkspaceInspection(RestoreWorkspaceState State, bool SafetyVerified, bool ResultVerified);

// Offline preparation only: persisted evidence is not permission to replace or reopen a live DB.
internal static class KeepLaterRestoreWorkspace
{
    private const string ManifestName = "operation.json";
    private const string SafetyName = "safety.zip";

    public static string Prepare(string backupSnapshot, string currentSnapshot, string settingsSnapshot,
        string workspaceParent, KeepLaterRestorePreparation approved, DateTimeOffset createdAt,
        CancellationToken cancellationToken = default, IProgress<RestoreWorkspaceState>? progress = null)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var backup = KeepLaterStagingBuilder.CheckSnapshot(backupSnapshot);
        var current = KeepLaterStagingBuilder.CheckSnapshot(currentSnapshot);
        if (!Path.IsPathFullyQualified(settingsSnapshot) || !Path.IsPathFullyQualified(workspaceParent))
            throw new ArgumentException("Settings snapshot and workspace parent must be absolute paths.");
        KeepLaterStagingBuilder.CheckReparsePoints(settingsSnapshot);
        KeepLaterStagingBuilder.CheckReparsePoints(workspaceParent);
        if (approved.HasUnresolvedIssues || approved.SettingsChoice != RestoreSettingsChoice.KeepCurrent)
            throw new InvalidOperationException("Review preparation and choose current settings first.");

        using var backupLease = Lease(backup);
        using var currentLease = Lease(current);
        using var settingsLease = Lease(settingsSnapshot);
        if (!approved.MatchesSnapshotHashes(KeepLaterStagingBuilder.Hash(backup), KeepLaterStagingBuilder.Hash(current)))
            throw new InvalidOperationException("Frozen input databases no longer match preparation.");
        using (var settings = JsonDocument.Parse(settingsLease))
            if (settings.RootElement.ValueKind != JsonValueKind.Object) throw new InvalidDataException("Settings snapshot must be a JSON object.");
        var manifest = new RestoreWorkspaceManifest(1, RestoreWorkspaceState.Preparing, createdAt.ToUniversalTime(),
            approved.PlanSha256, approved.BackupSha256, approved.CurrentSha256, KeepLaterStagingBuilder.Hash(settingsSnapshot));
        var root = Path.Combine(Path.GetFullPath(workspaceParent), "ActiveLogbook-restore-" + Guid.NewGuid().ToString("N"));
        if (Path.Exists(root)) throw new IOException("Workspace already exists.");
        Directory.CreateDirectory(root);
        try
        {
            Save(root, manifest);
            progress?.Report(manifest.State);
            cancellationToken.ThrowIfCancellationRequested();
            File.Copy(backup, Path.Combine(root, "backup.db"), overwrite: false);
            var currentDirectory = Path.Combine(root, "current");
            Directory.CreateDirectory(currentDirectory);
            File.Copy(current, Path.Combine(currentDirectory, "timepilot.db"), overwrite: false);
            File.Copy(settingsSnapshot, Path.Combine(currentDirectory, "settings.json"), overwrite: false);
            KeepLaterStagingBuilder.CheckSnapshot(backup);
            KeepLaterStagingBuilder.CheckSnapshot(current);
            VerifyInputs(root, manifest);
            KeepLaterStagingBuilder.ValidateSnapshot(Path.Combine(currentDirectory, "timepilot.db"));
            cancellationToken.ThrowIfCancellationRequested();

            var safetyPath = Path.Combine(root, SafetyName);
            using (var archive = ZipFile.Open(safetyPath, ZipArchiveMode.Create))
            {
                archive.CreateEntryFromFile(Path.Combine(currentDirectory, "timepilot.db"), "timepilot.db");
                archive.CreateEntryFromFile(Path.Combine(currentDirectory, "settings.json"), "settings.json");
                using var writer = new StreamWriter(archive.CreateEntry("metadata.json").Open());
                writer.Write(JsonSerializer.Serialize(new DataBackupMetadata(1, manifest.CreatedAt, manifest.CreatedAt, null)));
            }
            VerifySafety(root, manifest);
            manifest = manifest with { State = RestoreWorkspaceState.SafetyVerified, SafetySha256 = KeepLaterStagingBuilder.Hash(safetyPath) };
            Save(root, manifest);
            progress?.Report(manifest.State);
            cancellationToken.ThrowIfCancellationRequested();

            var result = KeepLaterStagingBuilder.Build(Path.Combine(root, "backup.db"),
                Path.Combine(currentDirectory, "timepilot.db"), Path.Combine(root, "result.db"), approved);
            cancellationToken.ThrowIfCancellationRequested();
            VerifyInputs(root, manifest);
            VerifySafety(root, manifest);
            KeepLaterStagingBuilder.CheckSnapshot(backup);
            KeepLaterStagingBuilder.CheckSnapshot(current);
            manifest = manifest with { State = RestoreWorkspaceState.Ready, ResultSha256 = result.Sha256 };
            Save(root, manifest);
            progress?.Report(manifest.State);
            return root;
        }
        catch (Exception ex)
        {
            // Retain evidence and any verified safety backup. Never undo or delete user data here.
            var state = ex is OperationCanceledException ? RestoreWorkspaceState.Cancelled : RestoreWorkspaceState.Failed;
            try { Save(root, manifest with { State = state }); }
            catch (Exception saveError) when (saveError is IOException or UnauthorizedAccessException) { }
            ex.Data["RestoreWorkspacePath"] = root;
            throw;
        }
    }

    // Inspection never resumes a job or trusts an incomplete manifest as a completed restore.
    public static RestoreWorkspaceInspection Inspect(string root)
    {
        if (!Path.IsPathFullyQualified(root)) throw new ArgumentException("Workspace path must be absolute.");
        KeepLaterStagingBuilder.CheckReparsePoints(root);
        var manifestPath = Path.Combine(root, ManifestName);
        KeepLaterStagingBuilder.CheckReparsePoints(manifestPath);
        using var manifestLease = new FileStream(manifestPath, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
        var manifest = JsonSerializer.Deserialize<RestoreWorkspaceManifest>(manifestLease)
            ?? throw new InvalidDataException("Missing workspace manifest.");
        if (manifest.Version != 1 || !Enum.IsDefined(manifest.State)) throw new InvalidDataException("Unsupported workspace manifest.");
        foreach (var hash in new[] { manifest.PlanSha256, manifest.BackupSha256, manifest.CurrentSha256, manifest.SettingsSha256 })
            if (!ValidHash(hash)) throw new InvalidDataException("Incomplete workspace evidence.");
        if (manifest.SafetySha256 is not null && !ValidHash(manifest.SafetySha256)
            || manifest.ResultSha256 is not null && !ValidHash(manifest.ResultSha256)
            || manifest.State is RestoreWorkspaceState.SafetyVerified or RestoreWorkspaceState.Ready && manifest.SafetySha256 is null)
            throw new InvalidDataException("Incomplete workspace state.");
        var safety = false;
        if (manifest.SafetySha256 is not null)
        {
            VerifySafety(root, manifest);
            safety = true;
        }
        var result = false;
        if (manifest.State == RestoreWorkspaceState.Ready)
        {
            if (!safety || manifest.ResultSha256 is null) throw new InvalidDataException("Incomplete ready workspace.");
            VerifyInputs(root, manifest);
            var path = KeepLaterStagingBuilder.CheckSnapshot(Path.Combine(root, "result.db"));
            RequireHash(path, manifest.ResultSha256);
            result = true;
        }
        return new(manifest.State, safety, result);
    }

    private static void VerifyInputs(string root, RestoreWorkspaceManifest manifest)
    {
        RequireHash(KeepLaterStagingBuilder.CheckSnapshot(Path.Combine(root, "backup.db")), manifest.BackupSha256);
        RequireHash(KeepLaterStagingBuilder.CheckSnapshot(Path.Combine(root, "current", "timepilot.db")), manifest.CurrentSha256);
        var settings = Path.Combine(root, "current", "settings.json");
        KeepLaterStagingBuilder.CheckReparsePoints(settings);
        RequireHash(settings, manifest.SettingsSha256);
    }

    private static void VerifySafety(string root, RestoreWorkspaceManifest manifest)
    {
        var path = Path.Combine(root, SafetyName);
        KeepLaterStagingBuilder.CheckReparsePoints(path);
        using var lease = Lease(path);
        if (manifest.SafetySha256 is not null) RequireHash(path, manifest.SafetySha256);
        using var zip = new ZipArchive(lease, ZipArchiveMode.Read);
        if (zip.Entries.Count != 3 || zip.Entries.Select(e => e.FullName).Distinct(StringComparer.Ordinal).Count() != 3)
            throw new InvalidDataException("Unexpected safety backup entries.");
        foreach (var (name, hash) in new[] { ("timepilot.db", manifest.CurrentSha256), ("settings.json", manifest.SettingsSha256) })
        {
            using var entry = (zip.GetEntry(name) ?? throw new InvalidDataException("Missing safety backup entry.")).Open();
            if (Convert.ToHexString(SHA256.HashData(entry)) != hash) throw new InvalidDataException("Safety backup payload changed.");
        }
        using var metadata = (zip.GetEntry("metadata.json") ?? throw new InvalidDataException("Missing metadata.")).Open();
        var info = JsonSerializer.Deserialize<DataBackupMetadata>(metadata);
        if (info?.SchemaVersion != 1 || info.CreatedAtUtc != manifest.CreatedAt) throw new InvalidDataException("Safety metadata mismatch.");
    }

    private static void RequireHash(string path, string expected)
    {
        if (KeepLaterStagingBuilder.Hash(path) != expected) throw new InvalidDataException("Workspace file does not match frozen evidence.");
    }

    private static FileStream Lease(string path) => new(path, FileMode.Open, FileAccess.Read, FileShare.Read);

    private static bool ValidHash(string? value) => value?.Length == 64 && value.All(c => c is >= '0' and <= '9' or >= 'A' and <= 'F');

    private static void Save(string root, RestoreWorkspaceManifest manifest)
    {
        var path = Path.Combine(root, ManifestName);
        KeepLaterStagingBuilder.CheckReparsePoints(path);
        var pending = Path.Combine(root, "operation-" + Guid.NewGuid().ToString("N") + ".tmp");
        using (var stream = new FileStream(pending, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            JsonSerializer.Serialize(stream, manifest);
            stream.Flush(flushToDisk: true);
        }
        File.Move(pending, path, overwrite: true);
    }
}
