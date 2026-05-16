# TimePilot Community / Pro Strategy

This document summarizes how TimePilot can remain a useful public MIT-licensed Community edition while leaving room for a future paid Pro edition.

The Korean source planning document is `docs/PRO_EDITION_STRATEGY.ko.md`.

## Core Principle

Code and features already published in the public MIT repository should be treated as Community features. Pro-only implementation code should not be committed to the public repository.

The public repository may contain:

- Community features
- shared abstractions
- extension points
- documentation
- issue planning

The private Pro repository may contain:

- Pro-only implementation
- license checks
- advanced analytics
- advanced reports
- automation features
- paid distribution logic

## Community Scope

Community should remain useful on its own and keep the core promise of TimePilot: showing where the user's time disappears.

Natural Community features include:

- daily and specific-date summaries
- daily timeline
- basic detail tab views
- basic CSV export
- tray resident mode
- Windows startup preference
- local storage
- basic preferences
- basic multilingual UI
- features already released in the public MIT repository

## Pro Candidate Scope

Pro should add advanced understanding, long-term analysis, automation, and data management rather than taking away existing Community behavior.

Good Pro candidates include:

- weekly, monthly, and yearly analytics dashboards
- long-term trend comparison
- hourly usage pattern visualization
- advanced visual timeline
- goals and overuse alerts
- focus and context-switching analysis
- Excel or PDF report generation
- backup, restore, and encryption
- multi-PC data merge
- advanced app aliases, icons, and categories
- opt-in detailed tracking modes
- browser, website, or document-level tracking
- automated insights

## Current Issue Classification

Likely Community improvements:

- `#101` detail tab tracking-scope filters and explanations
- `#95` duplicated date calendar button cleanup
- `#85` recording coverage display and criteria review
- `#71` table sort state persistence and reset

Issues that may need Community / Pro split:

- `#22` custom app display names
- `#55` custom period summary
- `#56` visual activity timeline
- `#57` hourly usage pattern visualization
- `#60` backup and restore
- `#70` raw data export
- `#87` app information search and user classification
- `#90` opt-in detailed tracking mode

Structural prerequisites:

- `#65` WinForms code folder structure cleanup
- `#36` tracking engine and UI separation review

## Add-on Readiness

The current WinForms MVP still keeps many responsibilities inside `Form1.cs`. A Pro add-on model will be difficult if new tabs, menus, settings, analytics panels, and export actions must keep being added directly to `Form1`.

Future refactoring should introduce extension points for:

- tab registration
- menu registration
- settings sections
- analytics cards or panels
- export/report actions
- feature availability checks
- Community fallback behavior when Pro modules are absent

Example direction:

```csharp
internal interface ITimePilotFeatureModule
{
    string Id { get; }

    void Register(TimePilotFeatureRegistry registry);
}
```

This is a direction, not a finalized design.

## Roadmap

1. Document the Community / Pro boundary.
2. Update or create issues for add-on readiness and edition planning.
3. Refactor WinForms responsibilities out of `Form1.cs` incrementally.
4. Introduce internal UI registration points.
5. Only then start private Pro implementation when a concrete Pro feature is ready.

## Notes

- Avoid locking existing public features later.
- Keep Community useful and trustworthy.
- Treat privacy-sensitive tracking as explicit opt-in.
- Review licensing, payment, tax, refund, and distribution requirements before implementing paid distribution.
