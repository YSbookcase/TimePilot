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
- Extracts current database records that do not overlap the backup.
- Adds those records using the backup database's app ID and category ID model.
- Adds apps missing from the backup as new apps in the backup-based database.
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

Candidate records are not imported when their time interval overlaps the baseline.

The current interval comparison rule is:

```text
candidate.StartedAt < baseline.EndedAt
&& candidate.EndedAt > baseline.StartedAt
```

Intervals whose end and start touch exactly are not treated as overlapping.

---

## 4. App Matching Policy

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

## 5. Category Matching Policy

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

## 6. Settings Merge Policy

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

## 7. Record Merge Policy

First target tables:

- `foreground_sessions`
- `idle_sessions`
- `app_runtime_sessions`
- `process_runtime_sessions`
- `system_events`

Recommended policy:

- Import only non-overlapping intervals.
- For overlapping intervals, prefer the baseline record and exclude the candidate record.
- Tables with app/process context should remap their app IDs using app matching results.
- `system_events` do not have app IDs, so duplicate detection should be conservative by event time or time interval.

Cautions:

- Foreground and idle records can overlap in time while representing different meanings.
- Process runtime density depends on background tracking settings.
- App runtime sessions represent TimePilot's own execution, so merging them affects coverage analysis.

---

## 8. User-Facing Preview

Before applying merge restore, the user should know:

- Number of importable records
- Number of excluded overlapping records
- Number of newly added apps
- Number of newly added user categories
- Number of app conflict candidates
- Whether a safety backup will be created

The current Detailed analysis button is a first preview that reports keepable/importable record counts based on interval comparison.

New app/category counts and conflict candidates should be added in later analysis work.

---

## 9. Safety Rules

- Recommend creating a safety backup before merge restore.
- The default behavior should minimize current-data loss.
- Do not silently overwrite ambiguous apps or categories.
- Do not automatically change user-selected categories.
- Summarize merge results in the completion message or logs.

---

## 10. Implementation Order

1. Keep interval comparison logic as a pure service.
2. Analyze backup and current databases through temporary snapshots.
3. Add an app matching result model.
4. Add a category matching result model.
5. Add a merge plan object.
6. Add a merge plan preview UI.
7. Apply merge after safety backup creation.
8. Show a merge result summary.

---

## 11. Remaining Decisions

- Whether to auto-generate names for conflicting user categories
- Duplicate detection rules for `system_events`
- Whether merge restore needs a settings comparison screen
- Whether users should resolve conflict candidates manually or the first version should exclude them
- How to handle the same app installed in different paths across multiple PCs
