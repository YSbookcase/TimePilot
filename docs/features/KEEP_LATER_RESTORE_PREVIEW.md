# Keep-Later Restore Preview (#346, Phase 1)

## Scope And Entry

File > Restore backup > Detailed analysis > Keep-later preview.
The new window is read-only. Closing it returns to the restore-method chooser;
cancel that chooser to finish analysis without changing data. **Continue full
restore** still starts the existing full-replacement workflow, not keep-later
restore. Both merge/import radio choices remain disabled.

This phase does not implement a writer, ID remapping, settings restore, recovery
or session splicing. It must not be interpreted as completion of #346.

## Reference Time

Read the backup and current data from separate temporary snapshots. The original
ZIP, live database and settings are not restored or modified by analysis.

Choose only from backup evidence, in this order:

1. Recognized metadata schema 1 CreatedAtUtc with an explicit offset, not in the
   future relative to inspection time.
2. Last valid app_runtime_sessions.last_heartbeat_at.
3. Last valid observation/start/end/system-event time across the five record types.
4. No reference if none can be determined. Current data is never used to invent
   a backup cutoff. Closed current rows then remain unclassified.

The preview exposes source, reference, inspection time, last backup observation
and observed record ranges. Inferred reference times are not presented as actual
backup creation times. Future metadata is ignored with a warning. Backup evidence
beyond the reference is shown as a snapshot-timing warning, not silently trimmed.
The creation timestamp currently precedes the DB snapshot in normal ZIP creation,
so even legitimate backups may contain observations just after that timestamp.
The new reader streams rows (one backup pass for reference evidence, then per-table
classification) rather than retaining an additional full record array in memory.
The pre-existing overlap analyzer is unchanged in its collection-based design.

## Boundary And Counts

Counts are **database rows, not usage durations**. Foreground, idle, app runtime,
process runtime and system events are separate; do not sum overlapping durations.
Each source/table row belongs to exactly one bucket:

- Closed interval ending at/before the reference: before-reference candidate.
- Closed interval starting at/after it: at/after-reference candidate.
- Closed interval strictly spanning it: crossing, needs an explicit later policy.
- Open interval: open, needs review even when last observation/heartbeat exists.
  Observation is not proof of a normal close, and no end is synthesized.
- Missing/malformed start, reversed interval, future or offset-free timestamps:
  invalid, not silently counted as preserved or discarded.
- Valid closed row without a reference: unclassified.

Point system events and zero-duration intervals at the exact reference belong to
the at/after bucket. Timezone offsets compare as instants in UTC. Missing tables
or required time columns are marked unavailable; their zero counters are not proof
of absence. Displayed before/after totals are candidates, not an executable plan.

Planned baseline remains backup before reference and current data at/after it.
Crossing/open handling, temporal app/category identity and settings precedence
still need decisions and validation before a writer can be enabled.

## Existing Comparison

The old overlap comparison is retained, but labeled as an overlap-only diagnostic,
not a cutoff restore plan. Its nonoverlap counts cannot answer how many records
would survive #346. Malformed timestamps are skipped in that old diagnostic while
the new preview exposes their invalid count. Unsupported schemas may still make
detailed analysis unavailable; no restore choice is automatically enabled.

Analysis runs off the UI thread. Duplicate analysis, chooser continuation and
user closing are blocked while it runs; failures leave the future restore modes
disabled. New preview warnings use the selected KO/EN language independently of
global language settings.
The chooser shows an indeterminate animated progress bar and a persistent analysis
status. It does not invent a percentage or ETA. Completion, unavailable results and
failure are distinct; the bar disappears when work ends and retry remains available.

## Verification And Follow-Up

```powershell
dotnet test TimePilot.Tests/TimePilot.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~KeepLaterRestorePreview"
dotnet test TimePilot.Tests/TimePilot.Tests.csproj -c Release --no-restore
dotnet test TimePilot.Analytics.ConsumerTests/TimePilot.Analytics.ConsumerTests.csproj -c Release --no-restore
```

Synthetic tests cover boundary/offset/point/zero-duration behavior, open/invalid and
missing data, reference provenance/fallback, original DB/ZIP/settings hash
preservation, async chooser guards and KO/EN read-only layouts. No live user data
is used. Before release, inspect the new entry path in Windows/MSIX and cancel out
of the chooser; do not press Continue full restore during preview-only testing.

Next phase: decide unresolved boundary and mapping policies; create a frozen,
verifiable application plan; build in staging with safety backup and rollback;
only then enable actual keep-later restore in a separate PR. Pro contracts and
notification issue #355 are not part of this phase.
