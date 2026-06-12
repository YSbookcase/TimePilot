# Optional Detail Tracking Mode

## 1. Purpose

TimePilot's default tracking unit is app usage time.

Optional detail tracking defines how TimePilot may record more detailed information only when the user explicitly opts in.

This feature can be valuable, but it also increases privacy sensitivity. It must be disabled by default, and users must understand the collection scope before enabling it.

---

## 2. Principles

- The default mode records app-level usage only.
- Window titles, document names, browser domains, and URLs are not stored by default.
- Detail tracking should be offered as staged opt-in levels.
- Detail tracking should prefer per-app opt-in, but global apply can be offered when the user explicitly chooses it.
- Enabling global apply must show a confirmation dialog about sensitive data collection.
- Per-app detail tracking is disabled by default.
- Detail data remains local.
- Enabling detail tracking must show a privacy notice.
- Detail data needs separate deletion, export, backup, and restore handling.

---

## 3. Tracking Levels

### 3.1 Level 0: App-Level Tracking

This is the default mode.

Recorded data:

- App name
- Process name
- Executable path
- Usage time
- Idle time
- Runtime segments

Not recorded:

- Window title
- Document name
- Web page title
- Browser domain
- Full URL

### 3.2 Level 1: Window Title Tracking

This level records the active window title only when the user enables it.

Examples:

```text
Visual Studio - TimePilotStorage.cs
Google Chrome - GitHub Issues
Microsoft Word - Report.docx
```

Benefits:

- Can likely be implemented inside the desktop app.
- Helps explain what the user was doing inside an app.

Risks:

- May include document names, chat room names, email subjects, or web page titles.
- May store private information the user did not expect to record.

Policy:

- Disabled by default
- Requires a clear notice
- Requires deletion or retention controls

### 3.3 Level 2: Browser Domain Tracking

This level records browser usage by domain.

Examples:

```text
github.com
learn.microsoft.com
youtube.com
```

Benefits:

- Less sensitive than full URLs.
- Useful for web usage classification and analysis.

Risks:

- Domains can still reveal work, interests, health, finance, or private activity.
- Browser-specific implementation constraints apply.

Policy:

- Disabled by default
- Requires a stronger privacy notice than window title tracking
- Requires browser-specific research

### 3.4 Level 3: Full URL Tracking

This level records full URLs.

Example:

```text
https://example.com/path?query=value
```

Risks:

- URLs can include search terms, document IDs, tokens, and personal identifiers.
- Backup/export files become much more sensitive.

Policy:

- Defer for the community app until stronger controls exist.
- Consider only as an advanced option or Pro candidate.
- Requires deletion, masking, and retention policy first.

---

## 4. Implementation Options

### 4.1 Window Title Based

Read and record the active window title.

Benefits:

- Can start within the desktop app.
- Works across browsers, document editors, IDEs, and other apps.

Drawbacks:

- Title formats vary by app.
- Domains and URLs cannot be obtained reliably.
- Private information can be mixed in.

Priority:

- First implementation candidate

### 4.2 Browser History Database

Read local browser history databases for Chrome, Edge, Firefox, and similar browsers.

Benefits:

- Can provide clearer domain or URL data.

Drawbacks:

- Browser DB files can be locked while the browser is running.
- Storage schemas differ by browser.
- Data may not match the current active tab in real time.
- Privacy risk is high.

Priority:

- Research candidate

### 4.3 Browser Extension

Use browser extensions to send the active tab domain or URL to TimePilot.

Benefits:

- More accurate active tab information.
- Browser permissions can be shown explicitly to the user.

Drawbacks:

- Requires extension development and distribution.
- Requires app-extension communication.
- Adds security and maintenance cost.

Priority:

- Long-term candidate

---

## 5. UI Direction

Preferences should contain a separate detail tracking section.

Example:

```text
Detail tracking
[ ] Enable detail tracking

Per-app detail tracking
App name              Window title    Domain    URL
Google Chrome        [ ]             [ ]       [ ]
Visual Studio        [ ]             -         -
Microsoft Word       [ ]             -         -
```

Detail tracking should present per-app management as the primary workflow, while still allowing global apply when the user needs it.

- Per-app settings are the default management model.
- If global apply is enabled, the selected detail tracking level can apply to all target apps, so a strong confirmation dialog is required.
- Global apply must be explicitly selected by the user and disabled by default.
- Per-app settings should show a basic notice explaining what data will be recorded for that app.
- Browser domain or URL options should be shown or enabled only for browser apps.
- Per-app settings can be linked from App Category Management or a dedicated detail tracking management screen.

Enabling global detail tracking should show a confirmation dialog. Enabling detail tracking for a specific app should show a basic notice, with an additional confirmation if needed.

Example:

```text
Detail tracking can store sensitive information such as window titles, websites, and document names.
This data is stored locally, but it can be included in backup or export files.
Enable it only when needed.
```

---

## 6. Storage Policy

Detail data should be stored separately from normal app usage records.

Candidate table:

```text
detail_activity_sessions
- id
- foreground_session_id
- app_id
- detail_type
- title
- domain
- url
- started_at
- ended_at
- duration_ms
```

Full URL storage should remain deferred until its policy is finalized.

---

## 7. Deletion And Export Policy

Detail tracking data is more sensitive than normal usage records.

Needed controls:

- Delete detail tracking data only
- Delete detail tracking data by date range
- Choose whether backup/export includes detail data
- Strengthen raw export privacy notices

---

## 8. Current Decision

- The default remains app-level tracking.
- Detail tracking is disabled by default.
- Detail tracking should prefer per-app opt-in, while global apply can be used after a strong confirmation.
- The first implementation candidate is window title tracking.
- Browser domain tracking requires more research.
- Full URL tracking is deferred.
- Detail tracking data should be stored in separate tables.
- Optional detail tracking is a Pro candidate feature.
- The public Community repository should keep policy, UX expectations, storage contracts, and extension points only.
- Actual detail collection for window titles, browser domains, URLs, or document names should remain in a private Pro module or a separately approved future implementation.
- When Pro modules are absent, Community UI should hide, disable, or clearly explain unavailable detail tracking features.
