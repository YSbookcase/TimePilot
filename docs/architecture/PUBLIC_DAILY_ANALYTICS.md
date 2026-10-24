# Public Daily Analytics Contract

Issue #349 is the first delivery under #333. This is a data boundary, not a Pro UI
or a live recording integration. No production WinForms file references the new
adapter, so Community startup, tracking, migration and backup paths are unchanged.

## Projects And Version

- `TimePilot.Analytics.Contracts`: .NET 8, public DTOs and `IDailyAnalyticsReader`,
  no WinForms or SQLite dependency. Contract version `1.0.0`.
- `TimePilot.Analytics.Sqlite`: .NET 8, Windows-only adapter, Microsoft.Data.Sqlite
  `10.0.7`. Explicit factory `SqliteDailyAnalyticsReader.Open(absolutePath)`.
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

Create the isolated snapshot using an explicit backup/export workflow outside
this API, never a raw copy of a live WAL DB. The reader does not generate backups,
initialize schemas, start tracking, read/save settings, register startup, mutate
global paths, search default folders or substitute another source. Only the
selected DB is read. Apps/categories are read from that DB, not settings.json.
No executable paths, window titles, domains, URLs or process IDs are exposed.

Supporting concurrent live WAL readers without source-side effects remains an
explicit follow-up decision, not a capability claimed by version 1.

## Calling Example

```csharp
using TimePilot.Analytics.Contracts;
using TimePilot.Analytics.Sqlite;

using IDailyAnalyticsReader reader = SqliteDailyAnalyticsReader.Open(snapshotPath);
var result = reader.Read(new DailyQuery(
    new DateOnly(2024, 1, 1),
    new DateOnly(2025, 1, 1), // exclusive calendar boundary
    TimeZoneInfo.FindSystemTimeZoneById("Korea Standard Time"),
    observedAt), cancellationToken);
```

No installed app or SharedStoreData session is required. This API is synchronous;
call it off the UI thread. It serializes Read/Dispose on each instance, releases
its transaction before returning immutable-list results, and permits repeated
reads until disposed. Cancellation is checked before and during reads and daily
aggregation; native commands receive Cancel. Cancellation is cooperative, not a
hard latency promise; lock waiting and individual grouping operations may finish
first. SQLite busy timeout is two seconds. There is no arbitrary SQL endpoint.

## Time And Aggregation Semantics

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
- `Coverage.RecordedMilliseconds` is the observed **recorder runtime** within the
  calendar day. `ObservedMilliseconds` is elapsed calendar time up to ObservedAt;
  Unobserved is their difference. This is NOT Community's Windows-recordable-time
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
dotnet test TimePilot.Tests/TimePilot.Tests.csproj -c Release -p:OutputPath=bin/CodexPublicAnalyticsVerify/
```

Tests compare DB/settings hashes, file lists and logical schema before/after repeat
reads and disposal. Writer rejection and WAL/sidecar rejection use independent
fixtures so writer activity is not misattributed to the reader. Cases include
leap day, year boundary, explicit offset, DST, observation clipping, empty data,
invalid input/schema, cancellation and size limits. A synthetic year of 10,000
foreground sessions must return expected totals within 15 seconds on the test
host; this is a regression budget, not a production SLA. Existing Community
tests additionally initialize a fresh genuine schema and verify adapter acceptance.

## Handoff

After merge, record the PR, full merge SHA and actual test results on #349. Pro
must pin that SHA and independently repeat public consumer/unchanged-source checks
on fixture v1 before wiring real screens. Data-boundary validation is separate
from UI entry/date navigation (#351). Offline-only and calendar coverage limits
must be accepted or extended explicitly; this delivery does not prove complete
live Pro integration or close all of #333.
