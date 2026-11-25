using System.Security.Cryptography;
using System.Text.Json;

namespace TimePilot.WinForms.KYS24;

internal enum RestoreSettingsChoice { Unspecified, KeepCurrent, UseBackup }
internal enum RestoreMappingAction { Match, Add, Conflict }
internal sealed record RestoreIdentityMapping(long CurrentId, string? TargetKey, RestoreMappingAction Action, string Name);
internal sealed record RestoreRecordSelection(string Table, int BackupBefore, int CurrentAtOrAfter,
    int CurrentBeforeReplaced, int BackupAtOrAfterOmitted);

// Preparation evidence only: no method in this type writes or swaps a database.
internal sealed class KeepLaterRestorePreparation
{
    private readonly KeepLaterRestorePreview preview;
    private readonly bool backupHasSettings;
    public const string PolicyVersion = "keep-later-preparation-v1";
    public string BackupSha256 { get; }
    public string CurrentSha256 { get; }
    public string PlanSha256 { get; }
    public RestoreSettingsChoice SettingsChoice { get; }
    public DateTimeOffset? ReferenceAt => preview.ReferenceAt;
    public DateTimeOffset ObservedAt => preview.ObservedAt;
    public IReadOnlyList<RestoreRecordSelection> Records { get; }
    public IReadOnlyList<RestoreIdentityMapping> Categories { get; }
    public IReadOnlyList<RestoreIdentityMapping> Apps { get; }
    public IReadOnlyList<string> Blockers { get; }
    public IReadOnlyList<string> Warnings { get; }
    public bool HasUnresolvedIssues => Blockers.Count > 0;

    private KeepLaterRestorePreparation(string backupHash, string currentHash, KeepLaterRestorePreview preview,
        RestoreSettingsChoice settings, List<RestoreRecordSelection> records, List<RestoreIdentityMapping> categories,
        List<RestoreIdentityMapping> apps, HashSet<string> blockers, HashSet<string> warnings, bool backupHasSettings)
    {
        this.preview = preview with { Tables = Array.AsReadOnly(preview.Tables.ToArray()) };
        this.backupHasSettings = backupHasSettings;
        BackupSha256 = backupHash;
        CurrentSha256 = currentHash;
        SettingsChoice = settings;
        Records = Array.AsReadOnly(records.ToArray());
        Categories = Array.AsReadOnly(categories.ToArray());
        Apps = Array.AsReadOnly(apps.ToArray());
        Blockers = Array.AsReadOnly(blockers.OrderBy(b => b, StringComparer.Ordinal).ToArray());
        Warnings = Array.AsReadOnly(warnings.OrderBy(b => b, StringComparer.Ordinal).ToArray());
        PlanSha256 = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new
        {
            PolicyVersion, BackupSha256, CurrentSha256, preview.ReferenceAt, preview.ReferenceSource,
            preview.ObservedAt, SettingsChoice, Records, Categories, Apps, Blockers, Warnings
        })));
    }

    public bool MatchesSnapshotHashes(string backupHash, string currentHash) =>
        string.Equals(BackupSha256, backupHash, StringComparison.Ordinal)
        && string.Equals(CurrentSha256, currentHash, StringComparison.Ordinal);

    public KeepLaterRestorePreparation WithSettingsChoice(RestoreSettingsChoice settings)
    {
        if (!Enum.IsDefined(settings)) throw new ArgumentOutOfRangeException(nameof(settings));
        var blockers = Blockers.Where(b => !b.StartsWith("settings-", StringComparison.Ordinal)
            && !b.StartsWith("backup-settings-", StringComparison.Ordinal)).ToHashSet(StringComparer.Ordinal);
        AddSettingsBlockers(blockers, settings, backupHasSettings);
        return new(BackupSha256, CurrentSha256, preview, settings, Records.ToList(), Categories.ToList(), Apps.ToList(),
            blockers, Warnings.ToHashSet(StringComparer.Ordinal), backupHasSettings);
    }

    private static void AddSettingsBlockers(HashSet<string> blockers, RestoreSettingsChoice settings, bool backupHasSettings)
    {
        if (settings == RestoreSettingsChoice.Unspecified) blockers.Add("settings-choice-required");
        if (settings == RestoreSettingsChoice.UseBackup)
            blockers.Add(backupHasSettings ? "backup-settings-unvalidated" : "backup-settings-missing");
    }

    internal static KeepLaterRestorePreparation Build(KeepLaterRestorePreview preview,
        CategoryImportState? backup, CategoryImportState? current, string backupHash, string currentHash,
        RestoreSettingsChoice settings = RestoreSettingsChoice.Unspecified, bool backupHasSettings = false)
    {
        var blockers = new HashSet<string>(StringComparer.Ordinal);
        var warnings = new HashSet<string>(StringComparer.Ordinal);
        if (preview.ReferenceSource != RestoreReferenceSource.Metadata || preview.ReferenceAt is null) blockers.Add("reference-unconfirmed");
        if (preview.BackupBeyondReference) blockers.Add("backup-after-reference");
        if (preview.Crossing > 0) blockers.Add("crossing-sessions");
        if (preview.Open > 0) blockers.Add("open-sessions");
        if (preview.Invalid > 0) blockers.Add("invalid-records");
        if (preview.MissingTables) blockers.Add("missing-record-schema");
        var required = new[] { "foreground_sessions", "idle_sessions", "app_runtime_sessions", "process_runtime_sessions", "system_events" };
        if (preview.Tables.Count != required.Length || required.Any(name => preview.Tables.Count(t => t.Name == name) != 1))
            blockers.Add("missing-record-schema");
        if (!Enum.IsDefined(settings)) throw new ArgumentOutOfRangeException(nameof(settings));
        AddSettingsBlockers(blockers, settings, backupHasSettings);
        if (string.IsNullOrWhiteSpace(backupHash) || string.IsNullOrWhiteSpace(currentHash)) blockers.Add("snapshot-hash-missing");
        // All zero/after omissions are explicit candidates, never a silently applied deletion plan.
        var records = preview.Tables.Select(t => new RestoreRecordSelection(t.Name,
            t.Backup.Before, t.Current.AtOrAfter, t.Current.Before, t.Backup.AtOrAfter)).ToList();
        var categories = new List<RestoreIdentityMapping>();
        var apps = new List<RestoreIdentityMapping>();
        if (backup is null || current is null) blockers.Add("identity-schema-unavailable");
        else
        {
            CategoryImportService.ValidateState(backup);
            CategoryImportService.ValidateState(current);
            foreach (var category in current.Categories)
            {
                var matches = backup.Categories.Where(c => c.Builtin == category.Builtin && (c.Builtin
                    ? Canonical(c) is { } name && Same(name, Canonical(category)) : Same(c.Name, category.Name))).ToArray();
                if (matches.Length == 1)
                {
                    categories.Add(new(category.Id, $"backup:{matches[0].Id}", RestoreMappingAction.Match, category.Name));
                    if (!SameColor(category.Color, matches[0].Color)) warnings.Add("category-color-backup-priority");
                }
                else if (matches.Length > 1 || category.Builtin || backup.Categories.Any(c => NameCollision(c, category)))
                {
                    categories.Add(new(category.Id, null, RestoreMappingAction.Conflict, category.Name));
                    blockers.Add("category-identity-conflict");
                }
                else categories.Add(new(category.Id, $"current:{category.Id}", RestoreMappingAction.Add, category.Name));
            }
            foreach (var app in current.Apps)
            {
                var match = backup.Apps.SingleOrDefault(a => Same(a.Process, app.Process));
                var currentCategory = app.CategoryId is { } id ? categories.Single(c => c.CurrentId == id).TargetKey : null;
                if (match is null)
                {
                    var action = app.CategoryId is not null && currentCategory is null ? RestoreMappingAction.Conflict : RestoreMappingAction.Add;
                    apps.Add(new(app.Id, action == RestoreMappingAction.Add ? $"current:{app.Id}" : null, action, app.Process));
                    if (action == RestoreMappingAction.Conflict) blockers.Add("app-category-unmapped");
                    continue;
                }
                var pathConflict = !string.IsNullOrWhiteSpace(app.Path) && !string.IsNullOrWhiteSpace(match.Path)
                    && !Same(app.Path.Replace('/', '\\'), match.Path.Replace('/', '\\'));
                var backupCategory = match.CategoryId is { } backupId ? $"backup:{backupId}" : null;
                var userConflict = Same(app.Source, AppCategorySource.User) && currentCategory != backupCategory;
                if (pathConflict || userConflict)
                {
                    apps.Add(new(app.Id, null, RestoreMappingAction.Conflict, app.Process));
                    if (pathConflict) blockers.Add("app-path-conflict");
                    if (userConflict) blockers.Add("user-category-conflict");
                }
                else
                {
                    apps.Add(new(app.Id, $"backup:{match.Id}", RestoreMappingAction.Match, app.Process));
                    if (currentCategory != backupCategory) warnings.Add("app-category-backup-priority");
                    if (string.IsNullOrWhiteSpace(app.Path) || string.IsNullOrWhiteSpace(match.Path)) warnings.Add("app-path-unknown");
                }
            }
        }
        return new(backupHash, currentHash, preview, settings, records, categories, apps, blockers, warnings, backupHasSettings);
    }

    private static bool Same(string? a, string? b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
    private static string? Canonical(ImportCategory c) => AppCategoryDisplay.BuiltinCategories.FirstOrDefault(d => Same(d.CanonicalName, c.Name) || Same(d.KoreanName, c.Name))?.CanonicalName;
    private static bool NameCollision(ImportCategory a, ImportCategory b) => Same(a.Name, b.Name)
        || (a.Builtin && Canonical(a) is { } ac && AppCategoryDisplay.BuiltinCategories.Any(d => Same(d.CanonicalName, ac) && Same(d.KoreanName, b.Name)))
        || (b.Builtin && Canonical(b) is { } bc && AppCategoryDisplay.BuiltinCategories.Any(d => Same(d.CanonicalName, bc) && Same(d.KoreanName, a.Name)));
    private static bool SameColor(string? a, string? b) => string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b)
        ? string.IsNullOrWhiteSpace(a) && string.IsNullOrWhiteSpace(b)
        : ColorTranslator.FromHtml(a).ToArgb() == ColorTranslator.FromHtml(b).ToArgb();
}
