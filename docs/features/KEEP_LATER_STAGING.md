# Keep-Later Offline Staging (#346, Phase 3)

## Scope

`KeepLaterStagingBuilder` builds a new, disposable result DB from two closed,
standalone snapshots and a reviewed phase-2 preparation. It is internal, has no UI
entry point, and never opens the running app's data path implicitly. It does not
replace a database, stop recording, create a safety backup, restore settings, or
invoke Windows startup configuration. Existing restore UI behavior is unchanged.

This follows [phase 2](KEEP_LATER_RESTORE_PREPARATION.md), not a finished restore
implementation. An empty preparation blocker list still does not guarantee that
staging will pass its stricter schema/retained-parent checks.

## Preconditions And Publication

- Inputs must be absolute paths to closed standalone DB snapshots. WAL, SHM,
  journal sidecars, symbolic links and junctions are rejected. Do not pass live
  user databases. Read leases deny writes while the build runs.
- Require `KeepCurrent` settings and no preparation blockers. Recompute the
  preparation using the same reference/inspection instants and frozen input
  hashes; reject stale evidence or changed plans. Settings files are not inputs
  and are not copied or validated by this phase.
- Validate integrity, declared foreign keys, integer primary IDs and the known
  eight-table payload column sets. Unknown tables/columns, views, triggers,
  generated columns and different foreign-key behavior are rejected rather than
  migrated or dropped. This is a narrow current-schema gate, not general support
  for legacy/future databases or hostile SQL database files.
- The caller supplies a new absolute `.db` output path, distinct from inputs.
  Reject existing output sidecars as well so stale journals cannot accompany it.
  Build in an owned GUID-named sibling file via the SQLite backup API and one
  transaction. Never overwrite an existing destination.
- Check selected record counts, integrity and foreign keys, then recheck input
  hashes/sidecars. Publish by moving the completed staging file to the new path.
  Return output SHA-256, preparation SHA-256 and per-table retained counts.
- On failure, roll back and remove only this invocation's temporary file and its
  sidecars. Never delete input files or unrelated destination files. A crash can
  leave an unreferenced staging file; startup recovery/cleanup is not implemented.

## Selection And Identity Policy

Backup is the metadata baseline. Retain backup closed records strictly before
the cutoff (end at cutoff is allowed), then current closed records starting at
or after cutoff. Point events and zero-length sessions exactly at cutoff belong
to current. All five record tables participate. Open/crossing/invalid records,
inferred reference times and backup evidence after reference remain blockers;
no implicit truncation, end inference or silent row omission is introduced.

Keep backup numeric IDs for retained backup records. Allocate fresh numeric IDs
for current records, apps and custom categories; map both primary/recommended
category references, foreground/process/idle app references and runtime parents
of system events. A selected event whose runtime parent was not selected from
the same source aborts the whole build; it is not rewritten to NULL.

Matched apps/categories keep backup metadata, including alias, exclusion,
recommendations and observation history. Current observations are copied only
for newly added apps. New apps preserve their complete metadata; newly added
custom categories are marked `import`. All other payload columns are copied
without recomputing durations. Current user-assignment conflicts continue to
block in preparation. Non-user backup-priority warnings are not resolved by this
builder; a future UI must obtain approval of those policies before execution.

## Synthetic Verification

No Community app launch, actual user data or Pro contract change is required:

```powershell
dotnet test TimePilot.Tests/TimePilot.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~KeepLaterStagingBuilderTests"
dotnet test TimePilot.Tests/TimePilot.Tests.csproj -c Release --no-restore
dotnet test TimePilot.Analytics.ConsumerTests/TimePilot.Analytics.ConsumerTests.csproj -c Release --no-restore
```

Tests use isolated temporary databases created with the current storage schema:
all five record tables, colliding source IDs, custom categories, matched/new apps,
retained runtime references, cutoff equality, payload preservation, unchanged
source/settings bytes, output hash/counts, no-overwrite, stale input, unsafe
sessions/settings/sidecars, unsupported schema and rollback/owned-file cleanup.

## Remaining Work

The subsequent [safety workspace phase](KEEP_LATER_SAFETY_WORKSPACE.md) retains
offline inputs, a verified safety ZIP and operation state. It still performs no
live replacement, automatic rollback or crash recovery.

Before any live restore: explicit open/crossing-session and snapshot timing
policy; retained frozen inputs and UI approval lifetime; progress/cancellation;
quiesced recording and safety backup; atomic replacement/reopen and rollback;
crash recovery and user-facing end-to-end tests. Offline staging itself is not a
backup and cannot protect a future live swap. #346 remains open. Auto-backup
scheduling (#357), notifications (#355), live DB/WAL consumption and Pro contracts
are separate. No Store version bump or deployment is part of this work.
