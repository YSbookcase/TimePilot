# Public Daily Analytics Contract

Issue #349 is the first delivery under #333. This is a data boundary, not a Pro UI
or a live recording integration. No production WinForms file references the new
adapter, so Community startup, tracking, migration and backup paths are unchanged.

For SDK/platform conditions, fixed-commit external solution builds and one-command
synthetic reproduction, see [the consumer handoff](ANALYTICS_CONSUMER_HANDOFF.md).

## Projects And Version

- `TimePilot.Analytics.Contracts`: .NET 8, public DTOs and `IDailyAnalyticsReader`,
  no WinForms or SQLite dependency. Contract version `2.0.0`.
- `TimePilot.Analytics.Sqlite`: .NET 8, Windows-only adapter, Microsoft.Data.Sqlite
  `10.0.7`. Prefer `OfflineAnalyticsSnapshot.Create` then `OfflineAnalyticsSnapshot.Open`.
- `TimePilot.Analytics.ConsumerTests`: independent consumer, no WinForms reference
  or friend-assembly access. Fixture v1 is generated inside temporary directories
  by `DailyFixture`; it contains synthetic apps, categories and intervals only.

Use project references for now; no NuGet package has been published. Pin the full
merged Community commit, not a moving branch. Breaking DTO/semantic changes require
a major contract version. Additional optional functionality requires consumer
compatibility tests. `community-daily-v1` accepts `user_version=0` with the required
Community columns in real tables (not views). Extra columns are allowed. This is
a structural profile because Community does not yet stamp its schema version.
Future version values and missing required columns fail; they are never migrated.

The unmerged `57b2093` prototype used contract 1.0.0. Version 2.0.0 intentionally
breaks that draft: explicit source capture provenance is required, result DTOs
carry SourceCapturedAt, and generic Coverage names are removed. Both project
versions are 2.0.0; this is not an ActiveLogbook app/Store version bump. The DB
schema profile remains community-daily-v1 and artifact metadata format is 1.

## Source Safety And WAL Policy

This first adapter deliberately accepts an **offline rollback-journal snapshot**,
not the Store's live recording database. Opening it holds a Windows read-only
file lease denying existing/new write handles until Dispose. Source locks are
released on every opening failure. SQLite uses `Mode=ReadOnly`, private cache,
no pooling, plus connection-local `query_only=ON` and `trusted_schema=OFF`.

The header must specify rollback-journal format and no `-wal`, `-shm` or `-journal`
sidecar may exist. WAL is rejected even if checkpointed and its sidecars are gone.
This check precedes opening SQLite; the reader never creates sidecars, recovers
journals, checkpoints or converts journal mode. Do not mark a live DB immutable
to bypass this policy. An active SQLite writer fails with SourceUnavailable.
Direct SQL writes are not part of the API, and independent OS write attempts are
denied by the source lease. Holding a reader can prevent a recorder from opening
that same file: keep it short-lived and use a separate snapshot.

Prepare the isolated snapshot with the **explicit** workflow below, never a raw
copy of a live WAL DB. The reader alone does not generate backups,
initialize schemas, start tracking, read/save settings, register startup, mutate
global paths, search default folders or substitute another source. Only the
selected DB is read. Apps/categories are read from that DB, not settings.json.
No executable paths, window titles, domains, URLs or process IDs are exposed.

Supporting concurrent live WAL readers without source-side effects remains an
explicit follow-up decision, not a capability claimed by this offline stage.

## Non-Mutating Snapshot Preparation

1. Choose an existing absolute source DB path explicitly. Stop its recording app
   normally first. Create/Open do not stop processes, checkpoint, repair or convert
   the DB. Normal shutdown may itself write data; that is the recorder's action,
   not a snapshot-preparation side effect. WAL sources remain unsupported.
2. Choose a **new directory outside the source data folder**, with an already
   existing parent. Existing destinations, symbolic links/junctions in the source
   or destination-parent path, and output inside the source directory are rejected.
3. Call `OfflineAnalyticsSnapshot.Create(sourcePath, destinationDirectory, token)`.
   It obtains the source write-exclusion lease and validates the schema, then records
   SourceCapturedAt in UTC. The source is fixed for the whole copy, so that instant
   identifies its frozen contents, not copy completion or future query wall time.
4. While holding the lease, copy the rollback-journal main DB into a unique sibling
   staging directory. This file copy is safe **only because no writer or WAL is
   admitted**. Create analytics.db and snapshot.json containing format/contract/schema
   versions, SourceCapturedAt and the SHA-256 of the copy. No original path or
   settings data is persisted. Verify the copied artifact through the public Open
   API, then publish the directory in one move without replacing an existing path.
5. The returned PreparedAnalyticsSnapshot contains DirectoryPath, SourceCapturedAt
   and DatabaseSha256. Copy/transfer the **whole two-file directory**, not just the
   DB, to another process or PC. Open verifies the versions, capture timestamp and
   DB hash under its read lease; the timestamp is exposed as reader.SourceCapturedAt.

Preparation is synchronous, cancelable at copy-buffer/publication boundaries and
must run off the UI thread. Source contents and source folder file list do not
change; writes occur only in the explicitly selected destination parent. The source
lease is disposed on success or failure, so recording may resume after Create.
Failure/cancellation does not publish an incomplete directory. Only known files in
the owned staging directory are cleaned up; permission/IO cleanup failure can leave
an unpublished `.analytics-preparing-*` directory for manual cleanup. Output IO
failures may throw IOException/UnauthorizedAccessException. There is no automatic
retention cleanup, backup upload or fallback. Snapshot DBs contain private records.

Hashes detect a mismatched DB/metadata pair, not malicious forgery. Metadata must
come from a trusted preparation workflow; it is not cryptographically signed.
For an imported offline backup with separately trusted provenance, the lower-level
factory is `SqliteDailyAnalyticsReader.Open(path, sourceCapturedAt)`. The timestamp
is mandatory (default/MaxValue are rejected); do not substitute DateTimeOffset.Now,
file LastWriteTime, or backup-extraction time. Without known capture provenance,
prepare a new supported snapshot rather than guessing. A caller that forges the
lower-level capture argument can defeat the observation bound; it is not a security
boundary against the caller.

## Calling Example

```csharp
using TimePilot.Analytics.Contracts;
using TimePilot.Analytics.Sqlite;

// Explicit preparation, separate from querying. Both paths are absolute;
// source is offline; destination is a new directory outside the source folder.
var prepared = OfflineAnalyticsSnapshot.Create(sourcePath, newSnapshotDirectory, cancellationToken);

// Later, including in another process, transfer/read the full prepared directory.
using IDailyAnalyticsReader reader = OfflineAnalyticsSnapshot.Open(prepared.DirectoryPath);
var result = reader.Read(new DailyQuery(
    new DateOnly(2024, 1, 1),
    new DateOnly(2025, 1, 1), // exclusive calendar boundary
    TimeZoneInfo.FindSystemTimeZoneById("Korea Standard Time"),
    reader.SourceCapturedAt), cancellationToken);
var provenance = result.SourceCapturedAt;
var calendarOnly = result.Days[0].RecorderCalendarCoverage.RecorderToCalendarRatio;
```

No installed app or SharedStoreData session is required. This API is synchronous;
call it off the UI thread. It serializes Read/Dispose on each instance, releases
its transaction before returning immutable-list results, and permits repeated
reads until disposed. Cancellation is checked before and during reads and daily
aggregation; native commands receive Cancel. Cancellation is cooperative, not a
hard latency promise; lock waiting and individual grouping operations may finish
first. SQLite busy timeout is two seconds. There is no arbitrary SQL endpoint.

## Time And Aggregation Semantics

ObservedAt is an **evaluation cutoff**, not a promise that the DB was reconstructed
as of a historical instant. It must be at or before reader.SourceCapturedAt; a later
value throws ArgumentOutOfRangeException rather than silently clamping. The normal
choice is the stored capture time. Earlier cutoffs are allowed for duration clipping,
but app aliases/categories are still those of the captured DB. The source metadata
is never rewritten to match a query. Reopening the same artifact later must reuse
the same capture time, so open-ended idle intervals cannot grow with the host clock.
Dates before the query start give zero elapsed/recorded time; all durations also
respect the requested day. CapturedAt is metadata, not a recorder heartbeat: open
foreground/runtime rows still stop at their own last observation/heartbeat.

- Request 1 through 366 days as `[StartDate, EndDateExclusive)` in the supplied
  timezone. No host-local timezone or clock is used. All results are ascending by
  date, including empty and future days. Durations are integer milliseconds.
- Each midnight uses that date's offset, so DST days can contain 23 or 25 hours.
  Ambiguous midnight uses its earliest UTC occurrence; invalid midnight advances
  to the first valid minute (at most one skipped day). No fixed 24-hour splitting.
- Every interval is clipped to the requested day and ObservedAt, including closed
  intervals. Reversed intervals and malformed round-trip timestamp values fail.
- Foreground active time uses `ended_at`, else `last_observed_at`, else start. It
  does not invent activity after the last foreground observation. Like the current
  Community query, overlapping foreground rows are summed, not unioned or reduced
  by idle time. Totals can exceed wall time for inconsistent/overlapping records.
- Apps retain database-local IDs. Alias/display/process-name fallback is used only
  as a display label; two IDs with identical labels are not merged. TopApp is the
  largest app total, ties resolved by ascending ID; null for no activity.
- Categories use each app's current primary category at query time, not historical
  category attribution. Null ID is the uncategorized bucket. Category totals use
  all apps and are never inferred from TopApp. IDs are local to this DB snapshot.
- Idle uses its end or the observation limit and is unioned to avoid double count.
  Runtime uses its end, heartbeat, or start; runtime overlaps are unioned too.
- `DailyActivity.RecorderCalendarCoverage` is a `RecorderCalendarCoverage` DTO.
  `RecorderRunningMilliseconds` is the observed **recorder runtime** within the
  calendar day. `ElapsedCalendarMilliseconds` is elapsed calendar time up to ObservedAt;
  `CalendarGapMilliseconds` is their difference, and `RecorderToCalendarRatio` divides
  recorder runtime by elapsed calendar time (zero denominator yields zero).
  This is NOT Community's Windows-recordable-time
  coverage: no OS uptime, sleep/lock exclusion or boot-time inference is supplied.
  A gap does not mean user inactivity, Windows shutdown or an application crash.
  Foreground/idle and coverage are independent evidence; inconsistent records are
  not silently repaired or forced to agree.

The adapter fetches each of three interval tables once per request and bounds each
query at 250,000 overlapping rows. Exceeding that returns LimitExceeded, never a
partial result. SQLite date conversion in predicates favors correct offset handling
over index-only scans; very large histories need a future indexed normalized read
model. No paging/weekly activity intervals are promised here (#350).

## Errors And Verification

Invalid arguments/ranges use ArgumentException; non-Windows uses
PlatformNotSupportedException; post-dispose use throws ObjectDisposedException.
Cancellation uses OperationCanceledException. Source, journal, schema, data and
size errors use AnalyticsReadException with AnalyticsError. File-access failures
do not create parent directories or databases. Consumers must not retry against
another inferred/default path.

```powershell
dotnet test TimePilot.Analytics.ConsumerTests/TimePilot.Analytics.ConsumerTests.csproj -c Release
dotnet test TimePilot.Tests/TimePilot.Tests.csproj -c Release -p:OutputPath=bin/CodexSnapshotReviewVerify/
```

Tests compare DB/settings hashes, file lists and logical schema before/after repeat
reads and disposal. Writer rejection and WAL/sidecar rejection use independent
fixtures so writer activity is not misattributed to the reader. Cases include
leap day, year boundary, explicit offset, DST, observation clipping, empty data,
invalid input/schema, cancellation and size limits. A synthetic year of 10,000
foreground sessions must return expected totals within 15 seconds on the test
host; this is a regression budget, not a production SLA. Existing Community
tests additionally initialize a fresh genuine schema and verify adapter acceptance.
They also compare the two coverage definitions on the same synthetic DB: a full
elapsed day includes hours outside estimated Windows runtime, so the calendar ratio
differs from the Community UI's Windows-recordable ratio. Consumer tests cover
preparation-to-public-read, unchanged source files, frozen capture time on reopen,
future ObservedAt rejection, existing destinations and tampered/missing metadata.

Pro-review follow-up validation: 35 independent consumer tests and 303 Community
tests passed on Windows. No real user DB, settings or backup files were used.

## Handoff

After merge, record the PR, full merge SHA and actual test results on #349. Pro
must pin that SHA and independently repeat public consumer/unchanged-source checks
on fixture v1 and the prepared format-1 artifact before wiring real screens. Data-boundary validation is separate
from UI entry/date navigation (#351). Offline-only and calendar coverage limits
must be accepted or extended explicitly; this delivery does not prove complete
live Pro integration or close all of #333.
