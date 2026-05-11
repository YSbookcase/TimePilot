# TimePilot Project Context

TimePilot is a desktop app that records and analyzes a user's PC usage patterns to support time management and self-improvement.

Core concept:

> Show exactly where the user's time disappears.

## External References

- GitHub repository: https://github.com/YSbookcase/TimePilot
- GitHub issues: https://github.com/YSbookcase/TimePilot/issues

Use `docs/PROJECT_PLAN.md` as the main agent-facing project reference when making architectural, feature, UI, storage, or roadmap decisions.

Use `docs/PROJECT_PLAN.ko.md` as the Korean source planning document when clarification, original intent, or natural-language planning context is needed.

For activity tracking and storage design, use `docs/features/ACTIVITY_TRACKING_MODEL.md` as the detailed reference. The Korean source version is `docs/features/ACTIVITY_TRACKING_MODEL.ko.md`.

For development conventions, commit messages, branch names, issue format, code style, and privacy rules, use `docs/DEVELOPMENT_GUIDE.md`. The Korean source version is `docs/DEVELOPMENT_GUIDE.ko.md`.

For GitHub Issue draft format, labels, priority rules, and issue prompt templates, use `docs/GITHUB_ISSUE_GUIDE.md`. The Korean source version is `docs/GITHUB_ISSUE_GUIDE.ko.md`.

For UI layout, visual hierarchy, dashboard structure, labels, and display privacy decisions, use `docs/UI_DESIGN_GUIDE.md`. The Korean source version is `docs/UI_DESIGN_GUIDE.ko.md`.

## Current Direction

- Prioritize a small MVP before broad expansion.
- Track active foreground windows and accumulate usage time.
- Keep data local by default, with SQLite planned for persistence.
- Gradually separate responsibilities into Core, Infrastructure, UI, and App layers as the project grows.
- Use `develop` as the default integration branch for active work, and merge to `main` only for prototype, alpha, or stable releases.
- Minimize background performance impact.
- Treat privacy carefully; avoid cloud dependence unless explicitly requested.

## MVP Priorities

- Active window detection
- Usage time recording
- Simple output or basic UI verification
- Clean, extensible code structure

## Development Notes

- Respect the existing WinForms implementation while improving it incrementally.
- Prefer clear domain classes for tracking, sessions, storage, and analytics.
- Avoid large rewrites unless they directly support the planned architecture.
- Keep UI changes practical and focused on making usage data understandable.
