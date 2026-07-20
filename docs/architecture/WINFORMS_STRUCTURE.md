# TimePilot WinForms Structure

This document describes the current TimePilot Community WinForms composition and the rules for placing new code.

## Current State

The initial MVP placed most UI refresh, tracking, date navigation, data management, settings, and menu behavior in `Form1.cs`.

After the first responsibility-separation pass, `Form1.cs` keeps state fields, coordinator construction, and high-level composition order. Feature behavior is organized into responsibility-based partial files and supporting coordinators, controls, and services.

This is **responsibility-based organization of one `Form1` class**. Partial files are not independent modules or plugins.

## Main Partial Responsibilities

- `Form1.Initialization.cs`: UI, storage, tracker, and event initialization
- `Form1.Lifecycle.cs`: window, tray, placement, and sampling lifecycle
- `Form1.SystemEvents.cs`: startup notices, safe mode, and Windows events
- `Form1.ProcessTracking.cs`: background process scanning and persistence
- `Form1.Refresh.cs`: async reads, cache policy, and snapshot application
- `Form1.Summary.cs`, `Form1.Detail.cs`, `Form1.Timeline.cs`: view-specific behavior
- `Form1.DateNavigation.cs`: date selectors, recorded-date calendar, and rollover
- `Form1.AppActions.cs`: classification, web search, and shared app actions
- `Form1.Preferences.cs`: preference application and data clearing
- `Form1.DataExport.cs`, `Form1.DataBackup.cs`, `Form1.DataRestore.cs`: data operations
- `Form1.TableLayout.cs`: column layout and sort persistence
- `Form1.Status.cs`: status composition, wait cursor, and diagnostics
- `Form1.Localization.cs`: runtime UI language application
- `Form1.CoverageSummary.cs`: coverage and input-activity presentation
- `Form1.HeaderToolTip.cs`, `Form1.Menu.cs`, `Form1.SupportActions.cs`: supporting UI
- `Form1.Common.cs`, `Form1.DesignPreview.cs`: small shared helpers and designer data

## Placement Rules

1. Add behavior to the existing partial or service that owns the responsibility.
2. Keep database reads, aggregation, and policy decisions outside Form partials when practical.
3. When cross-partial calls grow, introduce a service or coordinator contract.
4. Keep `Form1.cs` limited to state and composition.
5. Do not use a new partial to hide domain logic.
6. Follow the existing asynchronous patterns for database and file operations.

## Supporting Structure

- `TimePilot.WinForms/KYS24/Features`: internal registry contracts for Community/Pro candidate features, menus, tabs, settings sections, analytics panels, and export actions.
- `TimePilot.WinForms/KYS24/Analytics`: read-only daily analytics contracts over the existing storage layer. This prepares long-range analytics such as an annual calendar heatmap without coupling future UI directly to SQLite storage details.

The current `Analytics` contract is limited to daily active time, recorded idle time, top app, and coverage metrics. It does not include Pro UI, license checks, or dynamic module loading.

## Remaining Architecture Work

- Actual `TimePilot.Core`, `TimePilot.Infrastructure`, and `TimePilot.WinForms` project separation
- Broader read-only analytics contracts between WinForms and storage
- Registration contracts connected to actual menu, analytics-window, and settings-section flows
- Community fallback behavior when optional modules are absent
- Build and distribution boundaries between public Community and private Pro code

The current structure is ready for extension-contract design, but it is not a dynamic plugin system.

## First Pro Reference Feature

The first Pro reference feature should be an **annual calendar heatmap for usage statistics**.

Initial scope:

- daily active-time intensity
- hover details for active time, recorded idle time, top app, and coverage
- navigation from a day to the existing Community daily summary
- year navigation
- distinction between no activity and TimePilot not running

Prefer a separate analytics window opened from a registered menu command before supporting dynamic tab registration. Use this feature to design read-only daily-stat providers, menu registration, language context, and Community fallback contracts.

