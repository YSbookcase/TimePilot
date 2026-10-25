# External Solution Handoff For Contract 2.0.0

This is the build/verification handoff for #349 / PR #352. It is not a claim that
TimePilot.Pro.sln has already been built or that Pro integration is complete.
The exact public API and time semantics are in PUBLIC_DAILY_ANALYTICS.md.

## References And Requirements

Reference these two projects from the **same fixed full Community commit**:

- TimePilot.Analytics.Contracts/TimePilot.Analytics.Contracts.csproj, net8.0,
  assembly TimePilot.Analytics.Contracts, project/contract version 2.0.0.
- TimePilot.Analytics.Sqlite/TimePilot.Analytics.Sqlite.csproj, net8.0,
  assembly TimePilot.Analytics.Sqlite, project version 2.0.0. This references the
  contract project itself; explicit references to both are shown for clarity.

Do not reference ActiveLogbook.exe, TimePilot.WinForms, TimePilot.Tests, the
packaging project, or the main TimePilot.sln for a Pro consumer-only build. No
Community app execution, Store installation, shared-data profile, Desktop workload,
Windows SDK packaging tools or user settings/data directory is needed by these two
libraries. Pro's own UI may have additional requirements.

Build with a .NET SDK capable of net8.0/C# 12 (SDK 8.0 or newer). Framework-dependent
console/test execution needs the .NET 8 Microsoft.NETCore.App runtime as well; a
newer SDK alone does not guarantee that runtime is installed. Adapter preparation
and queries require Windows because they enforce Windows file sharing semantics.
The validated host uses Windows x64, SDK 10.0.402 and .NET runtime 8.0.32. Other
architectures and SDK 8 specifically are not yet independently validated here.

NuGet restore must access the configured feeds or a populated local cache:

- Microsoft.Data.Sqlite 10.0.7 (adapter's only direct package).
- Resolved transitive dependencies on the validation host: Microsoft.Data.Sqlite.Core
  10.0.7, SQLitePCLRaw.bundle_e_sqlite3/core/lib.e_sqlite3/provider.e_sqlite3 2.1.11,
  System.Memory 4.5.3. The bundle supplies the native SQLite library; no separate
  SQLite executable/server is required. Do not exclude native runtime assets.
- Consumer tests additionally use Microsoft.NET.Test.Sdk 17.14.1, xunit 2.9.3,
  xunit.runner.visualstudio 3.1.4. They do not use InternalsVisibleTo or WinForms.

Git and Windows PowerShell 5.1 or PowerShell 7 are required only for the provided
handoff script, not the library API. Only the versions actually run are recorded
in #349; installation instructions are not a claim that every combination is tested.

## Pinning Community Outside Its Active Working Folder

Use an isolated checkout/archive, not the Community development directory whose
branch can change. After separate merge approval, the handoff records the **full
merge SHA** on #349. Until then, a PR head SHA is review-only, not a merged baseline.
The SHA must include the samples/script added by this handoff revision.

Example setup in a separate directory (substitute the recorded 40-character SHA):

```powershell
$communitySha = '<FULL_COMMUNITY_SHA_FROM_ISSUE_349>'
git clone --no-checkout https://github.com/YSbookcase/TimePilot.git CommunityPinned
git -C CommunityPinned fetch origin codex/public-daily-analytics
git -C CommunityPinned checkout --detach $communitySha
git -C CommunityPinned rev-parse HEAD
```

Keep that checkout stable. Do not switch its branch from Cursor or another chat.
After the PR branch is removed, a fresh default-branch clone contains the merged
commit and the extra PR-branch fetch is unnecessary. No Community source code is
copied into Pro production code: Pro uses normal project references to the fixed
public libraries, whose transitive packages are restored by its build.

## Pro Project And Solution References

For a .NET 8 or compatible Windows .NET 8 Pro project, define CommunityRoot as a
local MSBuild property (not a committed developer-specific absolute path):

```xml
<ItemGroup>
  <ProjectReference Include="$(CommunityRoot)\TimePilot.Analytics.Contracts\TimePilot.Analytics.Contracts.csproj" />
  <ProjectReference Include="$(CommunityRoot)\TimePilot.Analytics.Sqlite\TimePilot.Analytics.Sqlite.csproj" />
</ItemGroup>
```

Then build the owning external solution, for example:

```powershell
dotnet build TimePilot.Pro.sln -c Release "-p:CommunityRoot=C:\absolute\CommunityPinned" -p:ShouldUnsetParentConfigurationAndPlatform=false
```

Referenced projects need not be listed separately in the solution to build; they
are resolved through ProjectReference. The command-line global property preserves the chosen
configuration for references outside the solution; otherwise MSBuild may build
them with their default Debug configuration even when the solution uses Release.
Alternatively explicitly include/map the public projects in the Pro solution.
Setting this only as a local property in the consumer csproj is insufficient for
transitive references; the command-line property propagates through the whole graph.
The build must not redirect them to a moving
branch or mix contract/adapter revisions. This command is a Pro setup example;
Community validation does not run or modify the private Pro repository.

## One-Command Reproduction At An Exact Commit

From the pinned checkout run (choose an absolute new output directory with an
existing parent, outside user data; output is intentionally retained):

```powershell
.\scripts\verify-analytics-handoff.ps1 `
  -CommunityCommit $communitySha `
  -OutputDirectory 'C:\absolute\scratch\analytics-handoff-01'
```

The script checks a full local commit SHA, uses git archive to export **only** the
two public libraries, independent tests and sample at that SHA, and builds a
separate ExternalConsumer/Analytics.HandoffConsumer.sln. It supplies CommunityRoot
explicitly to the fixed export. No working-tree edits, branch switches, user paths,
private Pro files, or main Community/packaging solution are included. A missing
commit must be fetched first; no implicit branch fallback is allowed.

Its console sample accepts only a new sandbox directory (never a source/user DB
argument). It generates synthetic fixture v1, hashes the entire source folder,
calls Create then Open/Read, checks expected app/category/idle/calendar-coverage
results and compares source hashes/file names after disposal. It also reopens the
metadata to verify capture time persistence. Failure exits nonzero. The sample
does not erase outputs or overwrite existing folders. Then the script runs all
independent consumer tests (35 in this revision).

Retained outputs include input.json (exact SHA/SDK/platform), community-input.zip,
the fixed Community export, the external solution/build output, synthetic source,
prepared analytics.db + snapshot.json, and TestResults/*.trx. All DBs here are
generated synthetic data, not private production records.

Manual equivalent in a fixed checkout:

```powershell
dotnet build samples/Analytics.HandoffConsumer/Analytics.HandoffConsumer.sln -c Release -p:ShouldUnsetParentConfigurationAndPlatform=false
dotnet run --project samples/Analytics.HandoffConsumer/Analytics.HandoffConsumer.csproj -c Release --no-build -- 'C:\absolute\scratch\new-synthetic-run'
dotnet test TimePilot.Analytics.ConsumerTests/TimePilot.Analytics.ConsumerTests.csproj -c Release
```

The independent test suite additionally covers malformed inputs, write/WAL
rejection, capture bounds, metadata integrity, DST/leap/year boundaries, cancellation
and row/performance limits. Running the existing 303 Community regression tests is
a separate maintainer check, not required for the external public-only build.

## Minimal Real Consumer Shape

```csharp
using TimePilot.Analytics.Contracts;
using TimePilot.Analytics.Sqlite;

// Explicit preparation of an offline rollback-journal source, not a live DB.
var prepared = OfflineAnalyticsSnapshot.Create(sourcePath, newOutputDirectory, token);
using IDailyAnalyticsReader reader = OfflineAnalyticsSnapshot.Open(prepared.DirectoryPath);
DailySnapshot result = reader.Read(new DailyQuery(
    new DateOnly(2024, 1, 1), new DateOnly(2025, 1, 1),
    TimeZoneInfo.FindSystemTimeZoneById("Korea Standard Time"),
    reader.SourceCapturedAt), token);
var ratio = result.Days[0].RecorderCalendarCoverage.RecorderToCalendarRatio;
```

Carry both prepared files between processes; do not replace SourceCapturedAt with
now. ObservedAt cannot be later than capture time. This ratio uses elapsed calendar
time and is not the Community UI's Windows-recordable coverage. All paths must be
explicit; the sample's synthetic generator is not a real-data initialization API.

## Version And Handoff Checklist

- Contract and adapter project version: 2.0.0. No NuGet package published.
- Schema profile: community-daily-v1, unversioned user_version=0 structural check.
- Snapshot artifact format: 1, analytics.db + snapshot.json.
- Independent fixture schema/generator: v1, DailyFixture.cs (pinned source).
- Handoff smoke scenario: v1, sample Program.cs (same required schema, fixed
  2024-02-28 example: active 2h, idle 1h, recorder 3h / calendar 24h).
- Live DB/WAL, #350 activity intervals and #351 UI/date navigation remain excluded.

Before merge, record the reviewed head SHA and actual external solution/test
results on #349. Keep the PR unmerged until separately approved. After merge,
record the full merge SHA, these versions, use/reproduction examples and final
results there; only then should Pro pin its independent baseline. The library
contract validation does not itself prove TimePilot.Pro.sln compatibility.
