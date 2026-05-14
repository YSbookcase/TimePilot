# Activity Tracking Data Model Plan

## 1. Purpose

TimePilot should separate different kinds of computer time instead of treating all usage as one number.

The initial priority is time tracking, not resource monitoring.

The first model should track:

- TimePilot app runtime
- Foreground activity time
- Idle time after the configured inactivity threshold
- Process runtime, including background programs

CPU, memory, disk, and network tracking are deferred because they can increase database size and background overhead.

---

## 2. Core Concepts

### 2.1 Foreground Activity

Time spent with an app as the active foreground window.

This is the main answer to:

```text
Where did I spend my time?
```

### 2.2 Idle Time

Time after the user has not provided keyboard or mouse input for the configured threshold.

Idle time is stored separately from foreground time so reports can distinguish visible-but-inactive time from actual active time.

### 2.3 Process Runtime

Time during which a process was running, whether it was foreground or background.

This supports future analysis such as:

```text
Which programs were running while I was away?
Which programs ran in the background but were rarely foreground?
```

### 2.4 TimePilot App Runtime

Time during which TimePilot itself was running and able to record activity.

This is needed to distinguish tracked time from missing time:

```text
When was TimePilot not running?
How much of the day could not be observed?
```

---

## 3. Recommended Tables

### 3.1 app_runtime_sessions

TimePilot app runtime sessions.

```text
id
started_at
ended_at
duration_ms
last_heartbeat_at
shutdown_reason
system_booted_at
app_version
```

`system_booted_at` stores an estimated Windows system start time for the current boot session. It is calculated from the current UTC time and the system uptime. This is not the Windows login time or the time when the desktop appeared.

If the previous runtime session has no `ended_at`, the next app start should close it using the last heartbeat time when available.

The current shutdown reason values are:

```text
running
normal
unexpected
system-shutdown
clear-data
```

`system-shutdown` is used when the previous runtime session belongs to a different Windows boot session. This prevents Windows restart, shutdown, or power-button restart from being treated as an app crash.

### 3.2 apps

Application identity table.

```text
id
process_name
display_name
executable_path
first_seen_at
last_seen_at
user_alias
is_excluded
```

### 3.3 foreground_sessions

Foreground app sessions.

```text
id
app_id
started_at
ended_at
duration_ms
last_observed_at
```

Window titles are privacy-sensitive and should not be stored by default.

### 3.4 idle_sessions

Idle periods.

```text
id
started_at
ended_at
duration_ms
threshold_ms
foreground_app_id
```

`foreground_app_id` records the foreground app observed when the idle period started. This keeps idle time separate from active foreground time while still preserving useful context.

### 3.5 process_runtime_sessions

Time-only process runtime sessions.

```text
id
app_id
process_id
started_at
ended_at
duration_ms
first_observed_at
last_observed_at
tracking_scope
has_main_window
is_current_session_process
```

The process list can be sampled every 30 or 60 seconds at first. Resource metrics are intentionally excluded.

`tracking_scope`, `has_main_window`, and `is_current_session_process` preserve enough context to explain how the process was observed. They can also help distinguish current tracking-scope visibility from actual process termination.

---

## 4. Calculations

Actual active time is calculated by subtracting overlapping idle time from foreground time.

```text
actual_active_time = foreground_time - overlapping_idle_time
```

Background-only programs can be found by comparing process runtime sessions with foreground sessions.

```text
exists in process_runtime_sessions
rarely or never appears in foreground_sessions
```

### 4.1 Runtime Coverage And Missing Time

TimePilot runtime sessions are used to identify when the app could observe activity. Gaps between runtime sessions can be shown as missing or untracked time.

Current timeline behavior:

- Missing TimePilot runtime gaps are displayed in the timeline as display-only rows.
- These rows are not stored as separate DB records.
- Missing time does not imply a specific cause. It can mean TimePilot was closed, Windows was asleep, the PC was off, or the app was interrupted.

Future coverage statistics can use:

- Total TimePilot runtime for the selected day
- Total missing time
- Longest missing interval
- Coverage ratio
- The gap between `system_booted_at` and the first TimePilot `started_at` as a possible "TimePilot not running after boot" interval

### 4.2 Process Runtime Display Rules

The detail tab separates process runtime duration from the last observed time.

- Running session runtime duration is calculated up to the current time for display.
- Ended session runtime duration is calculated up to the stored end time.
- `LastObservedAt` is based on the actual `process_runtime_sessions.last_observed_at` value.
- `LastObservedAt` should not advance every second unless a process scan actually observed the process again.

This keeps the process scan interval distinct from the 1-second UI refresh interval.

### 4.3 Background Tracking Safe Mode

Risky background process tracking settings can cause heavy scanning or repeated failures on some machines. TimePilot records user approval for risky settings and applies a safe mode only when a repeated short unexpected-exit pattern is detected.

Risky setting thresholds:

```text
All processes + 10 seconds or less
User processes + 5 seconds or less
Any scope + 3 seconds or less
```

Safe mode criteria:

- The current setting is risky.
- The most recent runtime sessions ended as `unexpected`.
- The unexpected sessions were short.
- Shutdowns classified as `system-shutdown` are excluded.

Safe mode only disables background process runtime tracking. Foreground tracking, idle tracking, summaries, and timelines should continue to work.

---

## 5. Implementation Order

1. Add `app_runtime_sessions` so missing recording time can be identified.
2. Add foreground sessions.
3. Add idle sessions.
4. Store `apps`, `app_runtime_sessions`, `foreground_sessions`, and `idle_sessions` in SQLite.
5. Add time-only `process_runtime_sessions`.
6. Add runtime coverage and missing-time explanations.
7. Defer resource tracking until the time model is stable.

---

## 6. Deferred Items

- CPU usage tracking
- Memory usage tracking
- Disk I/O tracking
- Network usage tracking
- Command-line argument storage
- Default window title storage

These items need explicit privacy and storage-size decisions before implementation.
