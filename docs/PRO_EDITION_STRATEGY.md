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

- automatic app usage recording
- daily and specific-date summaries
- basic period summaries
- daily timeline
- basic detail tab views
- selected app runtime segment lists
- idle detection
- recording coverage and untracked-period guidance
- basic CSV export
- tray resident mode
- Windows startup preference
- local storage
- basic preferences
- data deletion and storage-location access
- basic multilingual UI
- basic diagnostics such as safe-mode notices and runtime shutdown reasons
- features already released in the public MIT repository

## Community Completion Baseline

For the current Community development cycle, TimePilot should reach this baseline:

- A user can install the app and understand today's app usage, active time, and timeline without extra explanation.
- A user can switch dates and inspect a past day in Summary, Details, and Timeline.
- If background app tracking is disabled or safe mode turns it off, the user can immediately understand the state.
- No-record days, TimePilot-not-running gaps, and Windows shutdown or restart sessions do not look like unexplained errors.
- The user can export their own data in a basic format for inspection in other tools.
- The app provides at least a basic path for understanding unknown apps, such as app display names or simple classification.
- Community behavior remains local-first and privacy-conscious by default.

Community areas that still need work include:

- Summary specific-date picker UX
- raw data export
- app display name or minimal classification support
- easier visual timeline reading
- recording coverage placement and interpretation
- idle-threshold storage for later idle statistics
- table sort state persistence and reset

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

- `#95` duplicated date calendar button cleanup
- `#105` recent runtime shutdown reason and unexpected exit display
- `#106` donation link in the Help menu
- `#108` store idle threshold used by each idle session
- `#113` detail-tab notice when background app tracking is disabled
- `#85` recording coverage display and criteria review
- `#71` table sort state persistence and reset
- `#70` raw data export

Issues that may need Community / Pro split:

- `#22` custom app display names
- `#55` custom period summary
- `#56` visual activity timeline
- `#57` hourly usage pattern visualization
- `#60` backup and restore
- `#87` app information search and user classification
- `#90` opt-in detailed tracking mode
- `#107` highlight long timeline usage segments

Examples:

- Custom app display names can keep basic aliases in Community, while rule-based classification, icon management, and category analytics can become Pro candidates.
- Custom period summaries can keep specific day, this week, last week, this month, last month, this year, and last year in Community, while long-term trend comparison and advanced dashboards can become Pro candidates.
- Visual timeline work can keep a basic daily bar-style timeline in Community, while long-range comparisons, tag layers, focus analysis, and automatic interpretation can become Pro candidates.

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
2. Finish the Community completion baseline: date selection, export, coverage guidance, basic app understanding, and daily visual timeline readability.
3. Update or create issues for add-on readiness and edition planning.
4. Refactor WinForms responsibilities out of `Form1.cs` incrementally.
5. Introduce internal UI registration points.
6. Only then start private Pro implementation when a concrete Pro feature is ready.

## Notes

- Avoid locking existing public features later.
- Keep Community useful and trustworthy.
- Treat privacy-sensitive tracking as explicit opt-in.
- Review licensing, payment, tax, refund, and distribution requirements before implementing paid distribution.
