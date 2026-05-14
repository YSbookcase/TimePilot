# Development Guide - TimePilot

This document defines the development conventions for TimePilot.

The goal is to keep the project easy to read, easy to review, and simple enough to maintain while it grows from a WinForms MVP into a cleaner layered application.

---

## 1. Commit Message Convention

Use Conventional Commits.

Format:

```text
<type>: <short summary>
```

Use lowercase types and a short imperative-style summary.

Examples:

```text
docs: add activity tracking model
feat: add foreground window detection
fix: correct Korean UI labels
refactor: move usage logic to core
chore: update gitignore
```

### Types

| Type | Meaning |
|---|---|
| `feat` | New feature |
| `fix` | Bug fix |
| `docs` | Documentation change |
| `refactor` | Code restructuring without behavior changes |
| `chore` | Maintenance, config, dependency, or miscellaneous work |
| `test` | Test changes |
| `style` | Formatting-only changes |
| `perf` | Performance improvement |
| `build` | Build system or project file changes |
| `ci` | CI configuration changes |

### Commit Body

A commit body is optional.

Use it when the reason, tradeoff, or implementation detail is not obvious from the title.

Example:

```text
feat: add idle session detection

- Track idle state separately from foreground usage.
- Use Windows last-input time as the initial idle signal.
```

---

## 2. Branch Convention

Use short lowercase branch names.

Format:

```text
<category>/<topic>
```

Recommended categories:

```text
feature
fix
docs
refactor
chore
```

Examples:

```text
feature/foreground-app-info
docs/activity-tracking-model
fix/korean-ui-labels
refactor/core-usage-tracking
```

Main branches:

```text
main      stable branch
develop   optional integration branch while the project grows
```

For small documentation changes, working directly on the current branch is acceptable. Use a feature branch for larger code changes or risky refactors.

---

## 3. Issue Convention

Use issues for non-trivial work.

Use bracketed type names in issue titles.

Recommended title format:

```text
[<Type>] <short task description>
```

Examples:

```text
[Feature] Add foreground app display name
[Docs] Define SQLite schema
[Refactor] Move tracking logic to Core project
[Fix] Correct Korean UI labels
```

Recommended issue body:

```md
### Overview

### Background

### Tasks

### Done Criteria

### Related Files or Systems

### Notes
```

---

## 4. Code Convention

Follow standard C# naming conventions.

| Target | Convention | Example |
|---|---|---|
| Class, record, struct | PascalCase | `ForegroundAppInfo` |
| Method | PascalCase | `TryGetForegroundAppInfo` |
| Property | PascalCase | `ProcessName` |
| Local variable | camelCase | `processName` |
| Parameter | camelCase | `idleThresholdMs` |
| Private field | `_camelCase` | `_usageAccumulator` |
| Constant | PascalCase | `SampleIntervalMs` |
| Interface | `I` prefix + PascalCase | `IUsageRepository` |

Boolean names should be clear.

Examples:

```csharp
isIdle
canTrack
hasWindowTitle
shouldStoreWindowTitle
```

Prefer small classes with clear responsibilities.

Avoid mixing UI code, Windows API access, storage, and analytics rules in the same class when the code is no longer trivial.

---

## 5. Project Structure Convention

Follow the planned architecture gradually.

Target structure:

```text
TimePilot.Core
TimePilot.Infrastructure
TimePilot.WinForms
```

Responsibilities:

```text
TimePilot.Core
- Domain models
- Session logic
- Time aggregation
- Analytics rules

TimePilot.Infrastructure
- Windows API readers
- SQLite storage
- File system or OS integration

TimePilot.WinForms
- UI
- User interaction
- Display formatting
```

The current WinForms MVP may contain temporary implementation classes. Move them gradually as the architecture becomes clearer.

---

## 6. Documentation Convention

Use English documents as the main agent-facing reference.

Use Korean documents as the planning source when natural-language discussion or original intent is useful.

Examples:

```text
docs/PROJECT_PLAN.md
docs/PROJECT_PLAN.ko.md
docs/features/ACTIVITY_TRACKING_MODEL.md
docs/features/ACTIVITY_TRACKING_MODEL.ko.md
```

Detailed feature planning should be placed under:

```text
docs/features/
```

Database planning should be placed under:

```text
docs/database/
```

When both English and Korean versions exist:

- English version: concise implementation and agent reference
- Korean version: source planning document and discussion-friendly explanation

---

## 7. Privacy Convention

TimePilot is local-first and should avoid storing privacy-sensitive data by default.

Sensitive examples:

```text
window_title
command_line
full executable path
web page title
document name
```

Store these only after explicit user setting support is added.

Default tracking should focus on:

```text
process_name
display_name
foreground time
idle time
process runtime time
```

Resource metrics such as CPU, memory, disk, and network usage are deferred until the time model is stable.

---

## 8. Time And Storage Convention

Persist timestamps in UTC using round-trip date/time strings. UI code should convert timestamps to local time for display.

Current storage path:

```text
%LocalAppData%\TimePilot
```

Main local files:

```text
timepilot.db
settings.json
```

The app may keep user data and settings after uninstall. A complete manual cleanup requires deleting the local TimePilot data directory.

Runtime shutdown reasons should be explicit enough to avoid confusing app crashes with system lifecycle events.

Known `app_runtime_sessions.shutdown_reason` values:

```text
running
normal
unexpected
system-shutdown
clear-data
```

`system_booted_at` is an estimated Windows system start time derived from system uptime. It is not a login time and should be described carefully in UI, documentation, and issue discussions.

---

## 9. Build And Release Verification

For normal verification builds, use the alternate output path so local app binaries do not interfere with installed or running copies:

```powershell
dotnet build TimePilot.sln --no-restore /p:OutputPath=E:\Program_Study\TimePilot\TimePilot.WinForms\bin\CodexVerify\
```

For release or installer test artifacts, use:

```powershell
powershell.exe -ExecutionPolicy Bypass -File scripts\build-release.ps1 -Version <version>
```

This creates artifacts under:

```text
artifacts/release
```

Expected outputs:

```text
TimePilot-<version>-Setup.exe
TimePilot-<version>-win-x64-portable.zip
```

Installer builds require Inno Setup. Test installer versions should not be confused with public GitHub Releases.

Before release, verify at least:

- Build succeeds with no errors.
- The installer installs and launches TimePilot.
- Single-instance behavior works.
- Windows startup preference works.
- Uninstall removes installed binaries while preserving local data and settings.
- Risky background tracking safe mode does not trigger on normal Windows restart or power-button restart.

---

## 10. Documentation Maintenance

When behavior changes affect tracking semantics, storage schema, safety behavior, or release workflow, update the relevant docs in the same PR or a nearby documentation PR.

High-priority docs:

```text
docs/features/ACTIVITY_TRACKING_MODEL.md
docs/PROJECT_PLAN.md
docs/DEVELOPMENT_GUIDE.md
```

Keep issue and PR descriptions aligned with the current labels and the bracketed issue title style used in the repository.
