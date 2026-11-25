# Keep-Later Safety Workspace (#346, Phase 4)

## Delivered Boundary

`KeepLaterRestoreWorkspace.Prepare` packages the offline staging step with frozen
inputs, current settings, a verified safety ZIP and a persisted state manifest.
It has no UI entry point and does not pause recording, replace/reopen the live
database, apply settings, schedule startup or automatically recover anything.
The current restore chooser is unchanged.

Inputs are caller-owned **closed standalone snapshots**, never live user DBs or
their WAL files. Pass the current settings snapshot explicitly. The caller must
select KeepCurrent, review a blocker-free preparation, and pass its matching DB
snapshots plus an absolute workspace-parent path and actual preparation time.
The time is independent of the historical restore cutoff/analysis observation.

## Operation

1. Validate paths, reviewed plan hashes and JSON-object settings. Acquire read
   leases denying writes. Reject pre-cancelled requests before creating a folder.
2. Create a GUID-named operation folder and a version-1 `operation.json` manifest
   in Preparing state. It stores hashes/timestamps, not original absolute paths,
   aliases, settings values or exception messages.
3. Retain exact `backup.db`, `current/timepilot.db`, `current/settings.json` bytes;
   verify copies and the current DB's supported schema/integrity/foreign keys.
4. Create `safety.zip` with exact current DB/settings and standard version-1
   backup metadata. Hash-check both ZIP payloads and metadata before persisting
   SafetyVerified. No logs are included. This ZIP can be inspected/restored by
   DataBackupService; it is a snapshot backup, not a backup of subsequent writes.
5. Build `result.db` using the phase-3 builder. Recheck frozen inputs and safety
   evidence, then persist Ready with the output SHA-256.

The manifest is serialized to a unique temporary file, flushed, then moved over
the previous manifest. This reduces partial JSON exposure but is **not** a
power-loss durability guarantee for the directory or all artifact files.

## Failure, Cancellation And Inspection

Failure/observed cancellation persists Failed/Cancelled where possible, retains
all evidence and any verified safety ZIP, and rethrows. The exception's
`Data["RestoreWorkspacePath"]` locates an operation that was created. Failure
before folder creation has no operation path. No recursive cleanup is performed;
especially, safety artifacts are never deleted as part of failure handling.

`Inspect` is read-only. It rejects unsupported/inconsistent manifests and
changed/missing artifacts; ZIP payload hashes and metadata are checked, not just
the presence of a file. Ready additionally requires matching frozen inputs and
result hash. SafetyVerified/Preparing after interruption are never promoted to
Ready, even if a result file happens to exist. Failed/Cancelled can still report
a verified safety backup without claiming a completed result.

These hashes detect accidental changes against stored evidence; the manifest is
not signed, authenticated or authority to perform a restore. Inspection does not
resume, discard, replace, reopen or roll back anything. Startup discovery and
user-approved recovery remain follow-up work. If disk/permission failure also
prevents a failure-state write, the last persisted state may be incomplete;
inspection must not infer success from it.

Cancellation and progress are **stage boundaries**, not a percentage or an
immediate interruption of SQLite backup, hashing, compression or staging loops.
A future UI should run the operation outside its UI thread and describe this
limitation honestly.

## Privacy And Retention

Workspace files contain complete local activity DB/settings snapshots, not just
hashes. They are unencrypted, use normal filesystem permissions, and consume
space for both inputs, a ZIP and a result. Choose a private local directory with
adequate free space. Do not commit, upload or attach these artifacts to GitHub.
The tests use only synthetic temporary data. Automatic retention/deletion,
encrypted backups and user-visible recovery management are not delivered here.

## Verification And Next Work

```powershell
dotnet test TimePilot.Tests/TimePilot.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~KeepLaterRestoreWorkspaceTests"
dotnet test TimePilot.Tests/TimePilot.Tests.csproj -c Release --no-restore
dotnet test TimePilot.Analytics.ConsumerTests/TimePilot.Analytics.ConsumerTests.csproj -c Release --no-restore
```

Synthetic tests cover normal preparation, source/settings preservation, restoring
the safety ZIP into a separate empty directory, staged failure with safety
retention, cancellation before/after safety, stale inputs, unsupported current
schema, invalid settings, tampered evidence/manifests and read-only inspection of
an interrupted operation.

Next: resolve cutoff/open-session policy, final quiesced snapshots and approval
lifetime; add recording lifecycle orchestration, explicit UI approval/progress,
atomic live replacement/reopen with verified rollback, startup crash recovery and
end-to-end tests. **There is still no live apply action.** #346 remains open.
Pro contracts, scheduling (#357), notifications (#355) and Store version/release
changes remain separate.
