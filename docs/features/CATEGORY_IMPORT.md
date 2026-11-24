# Category Export And Import (#347)

## Scope

File > Import app categories is separate from full restore. It reads the selected
ZIP's single root `timepilot.db` into an owned temporary file, opens it read-only,
checks SQLite integrity and loads category/app metadata. Nothing else in the ZIP
is extracted or restored. The selected archive is not modified. Settings are not
required in the selected archive; the pre-apply safety backup must contain them.

It also accepts the category-only JSON described below. JSON does not contain a DB
or settings and is never accepted as a full restore backup.

The current database is previewed in a read transaction. Preview and cancel do not
change it. The backup's metadata is frozen in memory after preview; changing or
deleting the selected ZIP afterward cannot substitute a different import payload.
The same frozen-payload rule applies to JSON input.

## Category-Only Export

File > Export app categories writes a new `.json` file using format
`activelogbook-categories`, version 1. Export is read-only and uses a consistent
read transaction. It never overwrites an existing file; publication uses a unique
owned staging file in the chosen directory. A failure can leave a private-data
staging file only if cleanup is blocked by permissions or locks.

The file uses an explicit whitelist:

- Header: Format, Version, ExportedAtUtc (export time, not usage time).
- Categories: file-local Id, Name, Color, SortOrder, IsBuiltin.
- Assignments: ProcessName and file-local CategoryId, for classified apps only.

No session rows, usage durations/timestamps, settings, executable paths, display
names, user aliases, exclusions, recommendations or original database IDs are
serialized. It includes unused categories too. Category names and classified app
process names remain potentially sensitive; this is not anonymization or encryption.
Review user-authored names before sharing and use a full backup for recovery.

JSON input is limited to 8 MiB and depth 32, rejects unknown format versions,
missing required/unknown fields, null entries, duplicates and invalid references.
Because the file deliberately excludes executable paths, app matching is by process
name only. The preview explicitly warns about this limitation. ZIP input retains
path-conflict checks when both paths are known, even in file-priority mode.

## Conservative Mapping

- Builtins match recognized canonical names, including their known Korean aliases.
  Unknown builtins are skipped, not silently converted into custom categories.
- Custom categories match existing custom names, case-insensitively. Existing
  order is kept; colors are kept unless separately selected for replacement.
  Missing custom categories, even unused ones, are added.
  Names colliding with either language of a builtin are skipped.
- Apps match process names case-insensitively. If both executable paths are known
  and differ (ignoring case and slash direction), the app is skipped. Missing paths
  do not prove identity; this is the documented conservative first-stage policy,
  not executable identity verification.
- Only apps already in the current database can receive assignments. Backup-only
  apps are shown as skipped and are not created.
- By default every current assignment is retained, including `user`, `recommendation`, and
  earlier `import` assignments. Explicit `user` source is retained even with no
  category. Only unclassified, non-user apps receive an `import` assignment.
- The preview shows added/matched categories and each classified backup app's
  current category, file category, and keep/assign/skip/replace decision.

## Conflict Choices And Explicit Approval

- **Keep current assignments (default)** retains the conservative behavior above.
- **Use file assignments** replaces conflicting assignments on matching existing
  apps, including `user` assignments, only after explicit approval of the preview.
  Replaced assignments receive `import` provenance. Identical assignments are not
  rewritten and keep their existing provenance.
- **Use file colors for existing categories** is a separate, default-off checkbox,
  independent of assignment priority. It can update matched builtin and custom
  colors, including clearing a color when the file explicitly contains no color.
  Existing category names/order/provenance are not replaced. Equivalent HTML color
  representations are treated as the same color.
- Current/file color values and swatches appear in the preview. A shared category
  color affects every app using it, even apps absent from the file; the approval
  prompt explicitly warns about this and counts color and assignment replacements,
  including the subset of user assignments.
- The UI asks for overwrite approval on every apply attempt that would replace
  existing values. Cancel leaves all data and controls unchanged. The service also
  rejects such a plan without the approval flag, including before safety-backup
  creation. Backup skipping requires its own separate confirmation.
- Neither priority deletes categories/apps nor clears current assignments for
  missing or unclassified input entries. Version 1 JSON omits unclassified apps,
  and ZIP category import ignores unclassified assignments as well. This is not a
  complete historical category restore; that needs a separately versioned policy.

Choice changes recalculate the preview from the same frozen current/file metadata;
they do not reopen the input or silently refresh the approval basis. Apply still
checks the current database fingerprint in its write transaction. A changed current
state requires reopening the file. Priority/color options reset when a new dialog opens.

## Apply And Safety

The dialog defaults to creating a verified pre-import safety ZIP before applying.
Users can explicitly uncheck the recommendation and confirm applying without one;
the choice is not persisted. Completion shows either the safety ZIP path or that
the user skipped backup. When selected, backup failure still blocks application
and never silently falls back to skipping. This backup is in the app's normal backup directory; it is not
an external removal backup and may disappear on MSIX uninstall/reset. ZIPs are not
encrypted and contain sensitive records. A safety ZIP can remain even when the
subsequent import fails; normal backup management applies.

Category/app identities, assignments, colors, and category ordering are compared
again under a write transaction. Changes after preview abort the import and require
a new preview. Recording timestamps and new session rows do not invalidate a plan.
New app identities do invalidate it. SQLite rolls back all category/assignment
changes on any apply error, including errors after category creation.

Only `app_categories` insertions, approved color/updated_at changes, and the three
`apps.primary_category_*` assignment fields are written. Aliases, exclusion flags, recommendations, usage sessions,
durations, settings and source archives are not restored or replaced. Recording
continues during preview. Foreground sampling pauses during safety backup/apply and
resumes afterward; existing sessions are not ended or recreated.

Initialization adds `app_categories.source` with `user` as the default for existing
custom categories and `system` for builtins. Newly imported categories use `import`.
App assignment provenance continues using `apps.primary_category_source`. Backups
without the new category source column remain readable. Older backups lacking the
category/assignment schema are rejected without running migrations on the source.
Public analytics contract 2.0.0 and schema profile `community-daily-v1` are unchanged;
the additive metadata column does not alter their required fields.

The UI prevents duplicate apply/close while applying, shows progress and explicit
completion/failure, and requires reopening the backup after failure. Automatic
backup is suppressed while this dialog is open. No Windows removal or full restore
is invoked by this workflow.
With zero planned changes, apply and backup selection are disabled with an explicit
no-changes status; priority/color choices remain available to review alternatives.
Closing does not create a backup or run an import.

Input is limited to one database entry of at most 1 GiB; archive entry names cannot
choose extraction paths. Temporary extraction cleanup is best effort. A file lock
or permission failure can leave a private-data temporary DB in the OS temp folder.

## Verification

Run from the repository root on Windows with the normal .NET 8 development setup:

```powershell
dotnet test TimePilot.Tests/TimePilot.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~CategoryImport"
dotnet test TimePilot.Tests/TimePilot.Tests.csproj -c Release --no-restore
dotnet test TimePilot.Analytics.ConsumerTests/TimePilot.Analytics.ConsumerTests.csproj -c Release --no-restore
```

Tests use synthetic databases and cover preview/cancel nonmutation, canonical and
custom matching, user/current assignment protection, path conflicts, missing apps,
stale previews, legacy provenance, safety-backup failure, transaction rollback,
session/settings preservation, invalid ZIP/DB/schema/references, idempotency and
KO/EN dialog state/layout. No real user data or Store package is changed.
They also verify the JSON field whitelist, export/import roundtrip, source
nonmutation, file-local IDs, no overwrite, invalid JSON/version/reference rejection,
size limits and the process-name-only warning in both UI languages.
Policy tests cover explicit approval/refusal, default preservation, approved user
assignment replacement, shared/cleared/equivalent colors, absent/unclassified entries,
path conflicts, stale approved plans, and rollback of color/addition/assignment changes
together. UI tests verify that the exact displayed plan reaches the apply callback.

Before release, manually verify the File menu, file chooser cancellation, preview
details, progress/completion and refreshed category display in both languages.
Use disposable test data before pressing Apply. No version bump or Store update is
part of this PR. Time-based merge restore (#346), real-time DB/WAL consumption and
Pro-only analytics remain separate work.

For repeatable manual testing, see `../testing/CATEGORY_IMPORT_SMOKE.ko.md` and its
synthetic JSON fixture. It contains one unused custom category and no assignments.
Applying it adds that test category to the current DB, not usage records or app
assignments. Remove the unused test category in category management before repeating.
