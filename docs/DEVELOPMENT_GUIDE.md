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
develop   default integration branch for active development
```

### Branch Flow

- `main` is the stable release branch.
- `develop` is the default integration branch for active work.
- Feature, documentation, refactor, fix, and chore branches should open pull requests into `develop`.
- Merge `develop` into `main` only when preparing a prototype, alpha, or stable release.
- Avoid working directly on `main` unless the change is an urgent release correction.

Recommended pull request targets:

```text
feature/*  -> develop
docs/*     -> develop
refactor/* -> develop
fix/*      -> develop
chore/*    -> develop
develop    -> main, only for release preparation
```

---

## 3. Issue Convention

Use issues for non-trivial work.

Use `docs/GITHUB_ISSUE_GUIDE.md` for the full GitHub Issue draft format, labels, and priority rules.

Use the same general type names as commit messages, but issue titles may use the bracket format from the issue guide.

Recommended title format:

```text
[Type] <short task description>
```

Examples:

```text
[Feature] Add foreground app display name
[Documentation] Define SQLite schema
[Refactor] Move tracking logic to Core project
[Bug] Correct Korean UI labels
```

Recommended issue body:

```md
## Goal

## Tasks

## Notes

## Done Criteria
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
