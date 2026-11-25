# Keep-Later Restore Preparation (#346, Phase 2)

## Delivered Scope

Phase 1 counted records around a reference time. Phase 2 adds an immutable
preparation report with record-selection counts, current-to-backup identity
candidates, settings-policy choice, explicit blockers and snapshot/plan SHA-256.

Entry: Restore backup > Detailed analysis > Keep-later preview > Inspect
preparation. This window has only a settings-policy selector and Close. Selecting
a policy updates this report in memory; it does not save settings or perform a
backup/restore. Full replacement restore elsewhere in the chooser is unchanged.

**There is no writer, output database or atomic replacement in this phase.**
The subsequent [offline staging phase](KEEP_LATER_STAGING.md) adds a separate
internal result builder, without connecting it to this UI or live replacement.
An empty blocker list is not certification that a restore can safely be run. Record
IDs, runtime foreign keys, full payload schemas and output integrity are not yet
planned/validated. The type is deliberately called Preparation rather than an
executable application plan.

## Boundary Policy And Blockers

The draft record direction is backup closed rows before the reference and current
closed rows at/after it, including point events at the reference. The report shows
backup-before/current-after candidates and current-before/backup-after omissions
as separate counts, not usage durations. It cannot execute deletions.

Automatic application remains blocked for any crossing/open/invalid record,
unavailable record table/time columns, an unconfirmed/inferred reference, or backup
evidence after the reference. No session is cut, discarded, closed normally or
assigned an inferred end to make the blockers disappear. This fail-closed draft is
intentional; live current data normally has open sessions and ordinary backups can
contain observations slightly after metadata creation time. These cases need an
explicit policy and a final quiesced snapshot, not merely a checkbox override.

## Identity Candidates

The backup is the proposed base. Backup-only app/category identities remain in
that base; the report maps all current identities, not only ones referenced by
selected later records. This conservative approach can report a conflict on an
unused current identity. A future writer must resolve/prune it explicitly.

- Builtins match canonical names/known Korean aliases. Custom names match
  case-insensitively. Existing backup IDs are never assumed equal to current IDs.
- Target keys are scoped: `backup:1` and `current:1` are different identity
  candidates, not final numeric IDs. New numeric IDs will be allocated in staging.
- Missing custom categories and apps are addition candidates. Custom/builtin name
  collisions, ambiguous builtin matches and unknown current builtin names block.
- Same process name and compatible known paths form an app match candidate;
  slash direction/case are ignored. Different known paths block. Missing paths
  produce a warning, not proof of identity. Display name/alias is not an ID.
- Backup metadata is the proposed base for matched apps. If the current primary
  source is `user` (including explicit unclassified) and differs after category
  remapping, the report blocks instead of overriding. Non-user assignment and
  shared color differences are explicit backup-priority warnings.
- No historic aliases, exclusion/recommendation values, category names/order or
  provenance are written by preparation. The earlier category-only import options
  do not implicitly authorize metadata overwrites in record restore.

Identity reads reuse the existing validated category metadata reader, against the
same temporary DB connections used for record analysis. Unsupported identity
schemas produce an explicit blocker while record diagnostics remain available.

## Settings Policy

No default replacement choice is made. The report starts with settings-choice-
required. Keep current settings removes only that decision blocker; it does not
clear unrelated record/identity blockers. Use backup settings remains blocked
until a future step validates JSON and PC-bound values; a missing settings entry
is a separate blocker. Nothing in this window invokes StartupTask or changes OS
startup, language, folder paths, backup schedules or notification preferences.

This selector is for reviewing a proposed plan only. It resets when reopening the
report and does not change current full-replacement restore behavior.

## Frozen Evidence And Limits

SHA-256 covers each extracted/snapshotted DB file. Plan SHA-256 covers policy
version `keep-later-preparation-v1`, input hashes, reference/inspection instants,
settings choice, record counts, identity candidates and ordered blocker/warning
codes. Public collections are read-only copies. A settings choice produces a new
report and hash without mutating the original.

MatchesSnapshotHashes compares supplied hashes only. It is not an automatic
live-DB freshness check, never reads live files and does not account for settings,
logs or WAL by itself. The source snapshot files are temporary and disposed after
analysis. A future writer must retain its owned frozen inputs, validate all source
components, quiesce recording, revalidate or regenerate the plan and obtain fresh
approval before swapping. Hashing a live main DB alone is not sufficient.

The seventh progress stage now identifies metadata mapping/hash work rather than
leaving the previous classification stage appearing finished. It is indeterminate
because no byte-level copy/hash progress is measured yet.

## Verification And Next Step

```powershell
dotnet test TimePilot.Tests/TimePilot.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~KeepLaterRestore"
dotnet test TimePilot.Tests/TimePilot.Tests.csproj -c Release --no-restore
dotnet test TimePilot.Analytics.ConsumerTests/TimePilot.Analytics.ConsumerTests.csproj -c Release --no-restore
```

Synthetic tests cover scoped IDs, builtin aliases, category/app conflicts, current
user intent, non-user/color warnings, settings decisions, immutable plans/hash
changes, unsafe records, unsupported metadata and read-only KO/EN screens. Existing
snapshot analysis tests verify source DB/ZIP/settings byte preservation.

Next: finalize open/crossing/reference timing handling, per-record ID selection and
runtime/session foreign-key remapping; build an owned staging result with safety
backup; validate schemas/integrity/foreign keys and retained records; implement
failure recovery; only then enable live replacement in a separate PR. #346 remains
open. Pro contracts, automatic-backup scheduling (#357) and notifications (#355)
are separate tasks. No version bump or Store deployment belongs to this phase.
