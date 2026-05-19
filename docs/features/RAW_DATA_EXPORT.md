# Raw Data Export

Raw data export writes TimePilot's internal SQLite tables to CSV files and bundles them into a ZIP file.

The regular `Export CSV` command exports user-friendly summary, timeline, and runtime segment data. Raw data export keeps column names and values close to the internal table structure.

## Purpose

- Let users inspect their data in external analysis tools.
- Help development and troubleshooting by exposing the stored source records.
- Provide source data for future statistics and visualization work.

This is not a backup and restore feature. Restorable backups should be handled separately using the SQLite database and settings files.

## Privacy Notice

The exported ZIP can include personal usage records such as app names, process names, executable paths, usage times, and runtime segments.

Users should be careful when sharing or storing the exported file.

## Included Files

### `apps.csv`

App identity and display metadata.

- `id`
- `process_name`
- `display_name`
- `executable_path`
- `first_seen_at`
- `last_seen_at`
- `user_alias`
- `is_excluded`

### `app_runtime_sessions.csv`

TimePilot application runtime sessions and shutdown reasons.

- `id`
- `started_at`
- `ended_at`
- `duration_ms`
- `last_heartbeat_at`
- `shutdown_reason`
- `system_booted_at`
- `app_version`

### `foreground_sessions.csv`

Foreground app usage sessions.

- `id`
- `app_id`
- `started_at`
- `ended_at`
- `duration_ms`
- `last_observed_at`

### `idle_sessions.csv`

Idle periods detected by TimePilot.

- `id`
- `started_at`
- `ended_at`
- `duration_ms`
- `threshold_ms`
- `foreground_app_id`

### `process_runtime_sessions.csv`

Process runtime sessions detected by background app tracking.

- `id`
- `app_id`
- `process_id`
- `started_at`
- `ended_at`
- `duration_ms`
- `first_observed_at`
- `last_observed_at`
- `tracking_scope`
- `has_main_window`
- `is_current_session_process`
