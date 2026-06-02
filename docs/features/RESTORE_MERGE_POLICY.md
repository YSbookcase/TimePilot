# Restore Merge Policy

Korean source planning document: `docs/features/RESTORE_MERGE_POLICY.ko.md`.

## 1. Goal

TimePilot's current full restore replaces the current data with the backup `timepilot.db` and settings file.

Merge restore is different. It is for cases where the user wants to return to a backup state while keeping records collected on the current PC after that backup.

This document defines the policy for mapping apps, categories, settings, and records before merge restore is implemented.

---

## 2. Restore Modes

### Full Restore

- Replaces the current database and settings file with the backup files.
- This is the fastest and simplest restore mode.
- Apps, records, category changes, and setting changes collected after the backup can disappear from the current data.
- The app list can shrink back to the backup state.

### Backup-Based Restore With Later Records

- Uses the backup database as the baseline.
- Prefers backup records before the backup reference time.
- Keeps current records after the backup reference time.
- The first implementation candidate is to replace current records before the reference time with backup records.
- Apps that exist only in the backup can be added to the current database or kept as new apps in the backup-based database.
- Fits the user's expectation of returning to the backup state while preserving later records.

### Import Backup Records Into Current Data

- Uses the current database as the baseline.
- Extracts backup records that do not overlap current data.
- Adds those records using the current database's app ID and category ID model.
- Fits cases where the user wants to keep current settings and current app/category state.

---

## 3. Mapping Direction

Merge restore depends on baseline and candidate direction.

```text
Backup-based restore with later records
baseline = backup
candidate = current
```

```text
Import backup records into current data
baseline = current
candidate = backup
```

Candidate records are not mixed into the baseline when their time interval overlaps it.

The first implementation should prefer one side around a reference time.

```text
Backup-based restore with later records
before backup reference time = prefer backup
after backup reference time = prefer current
```

This avoids creating a plausible but false timeline by stitching together gaps from two databases that disagree about the same time range.

The current interval comparison rule is:

```text
candidate.StartedAt < baseline.EndedAt
&& candidate.EndedAt > baseline.StartedAt
```

Intervals whose end and start touch exactly are not treated as overlapping.

---

## 4. Backup Reference Time

The backup reference time decides how far the backup data should be trusted as the source of truth.

Recommended priority:

1. `CreatedAtUtc` from backup `metadata.json`
2. `MAX(app_runtime_sessions.last_heartbeat_at)` in the backup database
3. The last observed timestamp across the backup database

Last observed timestamp candidates:

- `foreground_sessions`: `MAX(COALESCE(ended_at, last_observed_at, started_at))`
- `process_runtime_sessions`: `MAX(COALESCE(ended_at, last_observed_at, started_at))`
- `app_runtime_sessions`: `MAX(COALESCE(ended_at, last_heartbeat_at, started_at))`
- `idle_sessions`: `MAX(COALESCE(ended_at, started_at))`
- `system_events`: `MAX(occurred_at)`

Running sessions can have `ended_at = NULL` because they were not normally ended at backup time. In those cases, use `last_heartbeat_at` or `last_observed_at` as the last observed timestamp.

Policy:

- Use the metadata creation time by default.
- If metadata is missing or unreliable, use the database's internal last observed timestamp as a fallback.
- If the two values differ significantly, show it as a warning or reference detail in Detailed analysis.
- For sessions crossing the reference time, the first implementation should prefer one side based on the restore direction instead of slicing and mixing records.

---

## 5. App Matching Policy

App matching should be conservative at first.

Priority:

1. `process_name`
2. `executable_path`
3. `display_name`

Recommended policy:

- Treat the same `process_name` as the first same-app candidate.
- If both sides have `executable_path` and the paths differ, treat the app as a conflict candidate.
- Do not automatically merge conflict candidates; surface them later in UI or logs.
- Apps that exist only in one database can be added to the baseline database as new apps.
- Newly added apps can use `import` as their category source.

Cautions:

- The same process name can still represent different apps.
- Executable paths can differ due to install location, portable apps, or updates.
- User aliases are display information and should not be the primary identity key.

---

## 6. Category Matching Policy

Categories carry user intent more strongly than app identity, so they need more conservative handling.

Built-in categories:

- Match by canonical name.
- Prefer the baseline database's color when colors differ.
- Treat built-in categories as system categories that are not deleted.

User categories:

- Treat the same name as a same-category candidate.
- The same name can still have a different color or meaning, so automatic merging must remain conservative.
- On conflict, prefer the baseline category and connect imported apps to the baseline category.
- Whether to add missing user categories or leave imported apps uncategorized can be decided by later policy or UI.

Recommended first implementation:

- Match built-in categories by canonical name.
- Match user categories by name.
- Add missing user categories to the baseline database and mark them with `import` source.
- Do not automatically overwrite `user` category choices.

---

## 7. Settings Merge Policy

Settings should be handled differently per restore mode.

Backup-based restore with later records:

- Keep backup settings as the baseline.
- Do not overwrite them just because current records are imported.
- Settings strongly tied to the current PC, such as Windows startup, language, or performance diagnostics, should be surfaced for user review.

Import backup records into current data:

- Keep current settings.
- Do not import backup settings.
- A backup settings comparison screen can be deferred.

---

## 8. Record Merge Policy

First target tables:

- `foreground_sessions`
- `idle_sessions`
- `app_runtime_sessions`
- `process_runtime_sessions`
- `system_events`

Recommended first policy:

- Split the data around the reference time and use only the preferred side for that range.
- Do not slice overlapping intervals into smaller mixed fragments.
- Prefer backup records before the reference time and current records after the reference time.
- For sessions crossing the reference time, the first implementation should prefer one side based on the restore direction or exclude the session.
- Tables with app/process context should remap their app IDs using app matching results.
- `system_events` do not have app IDs, so duplicate detection should be conservative by event time or time interval.

Cautions:

- Foreground and idle records can overlap in time while representing different meanings.
- Process runtime density depends on background tracking settings.
- App runtime sessions represent TimePilot's own execution, so merging them affects coverage analysis.

---

## 9. User-Facing Preview

Before applying merge restore, the user should know:

- Number of importable records
- Number of excluded overlapping records
- Backup reference time
- Database internal last observed timestamp
- Number of current records that would be replaced before the reference time
- Number of current records that would be kept after the reference time
- Number of newly added apps
- Number of newly added user categories
- Number of app conflict candidates
- Whether a safety backup will be created

The current Detailed analysis button is a first preview that reports keepable/importable record counts based on interval comparison. Later work should extend it into diagnostic information about replacement/keep counts around the reference time.

New app/category counts and conflict candidates should be added in later analysis work.

---

## 10. Safety Rules

- Recommend creating a safety backup before merge restore.
- The default behavior should minimize current-data loss.
- Do not stitch overlapping time ranges from two databases into a false-looking timeline.
- Do not silently overwrite ambiguous apps or categories.
- Do not automatically change user-selected categories.
- Summarize merge results in the completion message or logs.

---

## 11. Implementation Order

1. Keep interval comparison logic as a pure service.
2. Analyze backup and current databases through temporary snapshots.
3. Compute the backup reference time and database internal last observed timestamp.
4. Compute replace/keep counts around the reference time.
5. Add an app matching result model.
6. Add a category matching result model.
7. Add a merge plan object.
8. Add a merge plan preview UI.
9. Apply merge after safety backup creation.
10. Show a merge result summary.

---

## 12. Remaining Decisions

- Whether to auto-generate names for conflicting user categories
- Duplicate detection rules for `system_events`
- Whether merge restore needs a settings comparison screen
- Whether users should resolve conflict candidates manually or the first version should exclude them
- How to handle the same app installed in different paths across multiple PCs
- Acceptable difference between metadata creation time and database internal last observed time
