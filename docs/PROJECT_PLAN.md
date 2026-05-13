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

**Stored data**

- Program name and display name
- Start time
- End time
- Total usage time
- Tracking type, such as foreground, idle, or process runtime

---

### 3.2 Time Aggregation

- Daily usage time
- Weekly and monthly statistics
- Total usage time by program

---

### 3.3 Visualization (UI)

- Usage ratio by program with charts
- Usage patterns by time range
- Top list of most-used apps

---

### 3.4 User Settings

- Excluded program settings, such as system processes
- Recording interval settings
- Auto-start option

---

## 4. System Structure

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

- [ ] Active window detection
- [ ] Foreground usage time recording
- [ ] Idle time detection and separation
- [ ] Console output verification

---

### Phase 2 - Data Storage

- [ ] SQLite integration
- [ ] Foreground session storage
- [ ] Idle session storage
- [ ] Query feature

---

### Phase 3 - UI Implementation

- [ ] Basic dashboard
- [ ] Daily record display
- [ ] Separate active usage time and idle time display

---

### Phase 4 - Analytics Features

- [ ] Weekly and monthly statistics
- [ ] Top app analysis
- [ ] Process runtime tracking for background programs, time-only

---

### Phase 5 - Expansion

- [ ] Alert feature for time limit warnings
- [ ] Goal setting feature
- [ ] Resource usage tracking, such as CPU and memory, if needed
- [ ] GitHub publishing and distribution

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

---

## Conclusion

TimePilot is not just a logging app. It has the potential to become a tool that changes user behavior.

The right strategy is to complete the MVP quickly, then gradually expand the UI and analytics features.
