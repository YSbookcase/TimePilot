# UI Design Guide - TimePilot

## 1. UI Goal

TimePilot UI should help users quickly understand where their time went today.

The first priority is clarity. The UI should make usage data easy to compare before adding richer visual decoration.

Core UI question:

```text
Where did my time go today?
```

---

## 2. MVP UI Direction

- Use a practical desktop dashboard style.
- Show the current tracking state at the top.
- Use a table as the main usage view.
- Prioritize today's usage before weekly or monthly analytics.
- Separate active usage time and idle time visually.
- Keep charts secondary until the table data is reliable.

The MVP UI should not behave like a raw log viewer. Logs can be useful for development, but the user-facing screen should summarize and compare data.

---

## 3. Recommended MVP Layout

### 3.1 Top Status Area

Show the live tracking state.

Recommended fields:

- Current foreground app
- Current state, such as active or idle
- Today's total active time
- Today's total idle time
- Last updated time

### 3.2 Main Usage Table

Use the table as the primary screen area.

Recommended columns:

- App name
- Active usage time
- Usage ratio
- Last detected time
- Status or category

The table should support sorting by usage time once the data model is stable.

### 3.3 Summary Area

Use a small summary area after the table is useful.

Recommended summaries:

- Top apps today
- Total active time
- Total idle time
- Most recent foreground app

---

## 4. Visual Direction

- Prefer compact, readable desktop UI over marketing-style layouts.
- Use clear labels and consistent spacing.
- Avoid decorative layouts that reduce data readability.
- Avoid showing too many panels before the underlying tracking data is trustworthy.
- Make the most important number easy to scan: active usage time by app.

For WinForms MVP work, `DataGridView` is preferred over `ListBox` for usage records because each app has multiple meaningful fields.

---

## 5. Privacy Rules

- Do not store or display window titles by default.
- Use process names or app display names as the default visible identity.
- Treat detailed activity context as opt-in.
- Keep local-first behavior aligned with the project plan.

---

## 6. Growth Path

Recommended UI growth order:

1. Current status and app usage table
2. Daily active and idle time summary
3. Basic filtering and sorting
4. Simple charts for usage ratio
5. Weekly and monthly analytics
6. Settings for excluded apps and tracking interval

Avoid adding broad dashboard features before foreground usage tracking, idle tracking, and local persistence are stable.
