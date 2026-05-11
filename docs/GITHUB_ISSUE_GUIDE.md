# GitHub Issue Guide - TimePilot

## 1. Purpose

Use this guide when creating GitHub Issue drafts for TimePilot development work.

Issues should be clear enough that a task can be started later without re-discovering the original intent.

TimePilot issues are tracked at:

- https://github.com/YSbookcase/TimePilot/issues

---

## 2. Available Labels

Use only the labels that apply to the task.

| Label | Meaning |
|---|---|
| `bug` | Fix incorrect behavior, errors, crashes, or broken flows. |
| `documentation` | Write or update project documentation. |
| `feature` | Add new user-facing or developer-facing functionality. |
| `priority: High` | Important work that should be handled quickly. |
| `priority: Medium` | Normal priority work. |
| `priority: Low` | Work that is useful but not urgent. |
| `refactor` | Improve structure or maintainability without changing behavior. |
| `research` | Investigate, test, or prototype before implementation. |
| `techDebt` | Track temporary implementation details or future cleanup. |

---

## 3. Title Rule

Write a short title that makes the task purpose clear.

Recommended format:

```text
[Type] Short task title
```

Examples:

```text
[Feature] Replace usage list with summary table
[Bug] Fix broken Korean status labels
[Refactor] Separate usage summary row mapping from Form
[Documentation] Add UI design guide
```

Use title types that match the task category, such as `Feature`, `Bug`, `Refactor`, `Documentation`, or `Research`.

---

## 4. Body Template

Use this structure for issue bodies.

```md
### Task Overview

Briefly describe what this issue will implement or resolve.

### Background

Explain why this work is needed.

### Tasks

- [ ] Task 1
- [ ] Task 2
- [ ] Task 3

### Done Criteria

- Define how we know this issue is complete.

### Related Files or Systems

- List related files, modules, screens, or systems.

### Notes

- Add implementation cautions, temporary decisions, or future follow-up notes.
```

---

## 5. Priority Rules

### `priority: High`

Use High when the issue includes one or more of the following:

- The app cannot be built.
- The app crashes.
- A core MVP feature does not work.
- Usage tracking produces unusable results.
- Local storage or data migration fails after storage is introduced.

### `priority: Medium`

Use Medium when the issue includes one or more of the following:

- A feature works but needs improvement.
- The user experience is meaningfully affected.
- A normal MVP feature is being developed.
- Structure needs improvement but development is not blocked.

### `priority: Low`

Use Low when the issue includes one or more of the following:

- Documentation cleanup.
- Naming cleanup.
- Removing temporary logs.
- Minor UI polish.
- Improvements that can wait.

---

## 6. Issue Draft Request Prompt

Use this prompt when asking an AI assistant to create an issue draft.

```md
# Issue Draft Request

Create a GitHub Issue draft based on the development content below.

# Available Labels

- bug: Incorrect behavior, errors, crashes, or broken flows
- documentation: Project documentation and explanatory materials
- feature: New feature development
- priority: High: Important work that should be handled quickly
- priority: Medium: Normal priority work
- priority: Low: Useful but not urgent work
- refactor: Structure and maintainability improvements without behavior changes
- research: Technical investigation, test, or prototype work
- techDebt: Temporary implementation or future cleanup

# Issue Rules

Write the issue using this structure.

## Title

Write a short and clear title that shows the task purpose.

Examples:
- [Feature] Replace usage list with summary table
- [Bug] Fix broken Korean status labels
- [Refactor] Separate usage summary row mapping from Form

## Labels

Choose only the labels that apply from the available labels.

Examples:
- feature
- priority: Medium

## Body

Use the structure below.

### Task Overview
Briefly describe what this issue will implement or resolve.

### Background
Explain why this work is needed.

### Tasks
- [ ] Task 1
- [ ] Task 2
- [ ] Task 3

### Done Criteria
- Define how we know this issue is complete.

### Related Files or Systems
- List related files, modules, screens, or systems.

### Notes
- Add implementation cautions, temporary decisions, or future follow-up notes.

# Priority Rules

## priority: High

Use High when:
- The app cannot be built.
- The app crashes.
- A core MVP feature does not work.
- Usage tracking produces unusable results.
- Local storage or data migration fails after storage is introduced.

## priority: Medium

Use Medium when:
- A feature works but needs improvement.
- The user experience is meaningfully affected.
- A normal MVP feature is being developed.
- Structure needs improvement but development is not blocked.

## priority: Low

Use Low when:
- Documentation cleanup is needed.
- Naming cleanup is needed.
- Temporary logs should be removed.
- Minor UI polish is needed.
- The improvement can wait.

# Development Content

```text
Enter the development content here.
```

# Output Format

Always output in this format.

```md
# Issue Draft

## Title
[Type] Title

## Labels
- label1
- label2

## Body

### Task Overview

### Background

### Tasks
- [ ] Task 1
- [ ] Task 2
- [ ] Task 3

### Done Criteria
- Done condition

### Related Files or Systems
- Related system

### Notes
- Additional note
```
```

---

## 7. TimePilot Notes

- Keep MVP issues small enough to finish and verify.
- Prefer one issue per meaningful behavior change.
- Separate UI, tracking, storage, and documentation issues when possible.
- Link related documentation, such as `docs/PROJECT_PLAN.md`, `docs/UI_DESIGN_GUIDE.md`, or `docs/features/ACTIVITY_TRACKING_MODEL.md`, when it affects the task.
