# Project Plan - TimePilot

## 1. Project Overview

**Project name:** TimePilot

**Goal:**
Develop a desktop application that automatically records and analyzes the user's PC usage patterns to support time management and self-improvement.

**Core concept:**
"A tool that shows exactly where my time disappears."

---

## 2. Development Purpose

- Help users objectively understand how they spend their time.
- Automatically record usage time for specific programs such as games, YouTube, IDEs, and other apps.
- Expand over time into a productivity and self-management tool.

---

## 3. Main Features

### 3.1 Process Tracking Feature (Core)

- Automatically detect running programs.
- Measure usage time based on the active foreground window.
- Separately track foreground usage time, idle time, and background process runtime.
- Focus on time tracking first; CPU, memory, disk, and network resource tracking are deferred to a later expansion phase.
- Check state at a regular interval, such as every 1 to 5 seconds.
- Keep the 1-second foreground detection loop lightweight.
- Run heavier DB queries and background process aggregation outside the UI thread where practical.
- Protect risky background process tracking settings with safe-mode behavior.

**Stored data**

- Program name and display name
- Start time
- End time
- Total usage time
- Tracking type, such as foreground, idle, or process runtime
- Runtime session status and shutdown reason
- Estimated Windows system start time for distinguishing app interruption from system restart

---

### 3.2 Time Aggregation

- Daily usage time
- Community: total usage time by program and a basic summary for a selected day
- Pro candidate: weekly, monthly, and yearly statistics with long-term trend comparison
- Runtime coverage and missing-time statistics are planned so users can understand how complete a day of data is.

---

### 3.3 Visualization (UI)

- Community: daily activity flow and basic summaries
- Pro candidate: usage-ratio charts, time-of-day patterns, and long-term top-app analysis

---

### 3.4 User Settings

- Excluded program settings, such as system processes
- Recording interval settings
- Auto-start option
- Tray resident mode
- Windows startup preference
- Performance diagnostics display setting
- Data folder access and local usage-record deletion

---

## 4. System Structure

The following is the long-term target structure. See `docs/architecture/WINFORMS_STRUCTURE.md` for the current WinForms responsibility map and code placement rules.

### 4.1 Overall Structure

- Solution
  - `TimePilot.Core` - business logic
  - `TimePilot.Infrastructure` - data storage
  - `TimePilot.UI` - UI
  - `TimePilot.App` - application entry point

---

### 4.2 Main Modules

#### 1. Process Tracker

- Detect the current active program.
- Detect foreground app changes.
- Detect running process appearance and disappearance for time-only background tracking.
- Resource metrics such as CPU and memory are not part of the initial tracking scope.

#### 2. Session Manager

- Manage foreground sessions, idle sessions, and process runtime sessions.
- Record start and end events.

#### 3. Data Storage

- Local database, with SQLite planned.
- Save and query logs.

#### 4. Analytics Engine

- Aggregate time.
- Calculate statistics.
- Explain missing or untracked intervals without assuming the cause.

---

## 4.3 Current MVP Implementation Notes

The current implementation is a WinForms MVP that keeps all tracking and UI in one desktop process. This is intentional for the early product stage.

Implemented capabilities include:

- Foreground app usage tracking
- Idle session tracking
- Background process runtime session tracking
- App metadata and icon display
- Summary, timeline, and detail tabs
- Selected app runtime segment list
- CSV export for usage data
- Tray resident mode
- Windows startup preference and first-run startup prompt
- Single-instance protection
- Windows installer and portable package build scripts
- Runtime missing-gap display in the timeline
- Background tracking safe mode for risky settings
- Performance diagnostics display setting

Current design principles:

- Store usage data locally in SQLite.
- Store timestamps in UTC and display them in local time in the UI.
- Avoid storing window titles, command lines, document names, or web page titles by default.
- Keep resource metrics deferred until the time model is stable.
- Prefer incremental WinForms improvements over a broad rewrite.
- Treat basic app classification and local recommendation as a user-approved aid, not an automatic background decision. See `docs/features/APP_CLASSIFICATION_RECOMMENDATION.md`.

---

## 5. Tech Stack

- Language: C#
- Platform: .NET Desktop App
- IDE: Visual Studio
- Database: SQLite
- UI:
  - Initial: WinForms or WPF
  - Later: possible UI improvements

---

## 6. Development Phases

### Phase 1 - MVP (Core Features)

- [x] Active window detection
- [x] Foreground usage time recording
- [x] Idle time detection and separation
- [x] Basic UI verification

---

### Phase 2 - Data Storage

- [x] SQLite integration
- [x] Foreground session storage
- [x] Idle session storage
- [x] App runtime session storage
- [x] Process runtime session storage
- [x] Query feature

---

### Phase 3 - UI Implementation

- [x] Basic dashboard
- [x] Daily summary display
- [x] Timeline display
- [x] Detail tab for process runtime
- [x] Selected app runtime segment list
- [x] Separate active usage time and idle time display

---

### Phase 4 - Community Reliability and Usability

- [x] Process runtime tracking for background programs, time-only
- [ ] Runtime coverage and missing-time statistics
- [ ] Improve timeline clarity for recording status and activity flow

---

### Phase 5 - Release Stability and Pro Separation

- [x] GitHub publishing and distribution
- [x] Windows installer
- [x] Korean and English UI with a first-run default based on the Windows display language
- [x] Full backup and full restore
  - Merge restore is a Pro candidate; see `docs/features/RESTORE_MERGE_POLICY.md` for the policy.
- [ ] Complete Microsoft Store public release and installation/update verification
- [ ] Manage advanced analytics, reporting, and detailed tracking in the private Pro repository

Do not expand weekly/monthly/yearly analytics, heatmaps, time-of-day patterns, goals and alerts, advanced backup/restore, detailed tracking, or data encryption as base Community functionality. Implement those features in the private Pro repository according to the boundary in `docs/PRO_EDITION_STRATEGY.ko.md`.

---

## 7. Differentiators

- Focused on behavior analysis, not just simple logging.
- Developer-friendly and extensible structure.
- Lightweight local-first design without cloud dependency.

---

## 8. Future Expansion Ideas

- Mobile integration
- Productivity score system
- AI-based usage pattern analysis
- Unity-based visualization UI with gamification elements
- Resource usage analysis for unknown or background programs

---

## 9. Expected Effects

- Improved self-management ability
- Reduced time-wasting patterns
- Useful as a development portfolio project

---

## 10. Risks and Considerations

- Minimize background performance impact.
- Consider privacy sensitivity and keep storage local.
- Ensure accurate active window detection logic.
- Avoid high-volume resource logging in the early product stage.
- Avoid treating Windows shutdown, restart, or power-button restart as an application crash.
- Make tracking intervals and UI refresh intervals clear to the user.

---

## Conclusion

TimePilot is not just a logging app. It has the potential to become a tool that changes user behavior.

The right strategy is to complete the MVP quickly, then gradually expand the UI and analytics features.
