# App Classification Recommendation Plan

Korean source planning document: `docs/features/APP_CLASSIFICATION_RECOMMENDATION.ko.md`.

## 1. Goal

App classification should reduce the work required to organize many recorded apps while keeping user control.

The first recommendation model should answer:

```text
Which category is likely for this unclassified app?
```

It should not silently change categories.

---

## 2. Principles

- Recommendations are suggestions, not automatic decisions.
- A category is applied only when the user approves it.
- User-assigned categories must not be overwritten automatically.
- The first target is unclassified apps.
- Already-classified apps may show a "review suggested" state later when a recommendation disagrees with the current category.
- Privacy-sensitive data such as window titles, URLs, document names, or command lines must not be used by default.

---

## 3. Community Scope

The Community edition can include a practical, local-first recommendation baseline:

- Known app dictionary for common apps, such as IDEs, browsers, messengers, game launchers, media players, and system apps.
- Conservative process-name and product-name matching.
- Recommendation reason text, such as "Known browser app" or "Known game launcher".
- Recommendation display in the App Category Management screen.
- Apply recommendation to selected unclassified apps.
- Keep manual category edits as user-owned choices.

This keeps the feature useful without depending on cloud lookup or invasive tracking.

---

## 4. Pro Or Deferred Scope

The following items should stay deferred or become Pro candidates:

- Internet search based classification.
- Browser, website, or document level classification.
- Rule-based automatic classification that runs continuously.
- Multi-PC recommendation sync.
- Advanced category analytics and long-term category trends.
- Bulk overwrite of user-assigned categories.
- Category change history with full audit views.

These features can be valuable, but they increase complexity, privacy risk, or product boundary risk.

---

## 5. Recommended Data Model

The current app category model stores one primary category on the `apps` table.

For recommendation support, add explicit source fields later instead of inferring intent from the category value alone.

Candidate fields:

```text
apps.primary_category_source
apps.primary_category_updated_at
apps.recommended_category_id
apps.recommended_category_reason
apps.recommended_category_confidence
apps.recommended_category_updated_at
```

Candidate source values:

```text
none
user
recommendation
import
system
```

Suggested meaning:

- `none`: no category has been assigned.
- `user`: the user explicitly chose the category.
- `recommendation`: the user accepted a recommendation.
- `import`: the category came from restore or import.
- `system`: the app was assigned by built-in system behavior.

Manual user changes should set `primary_category_source` to `user`.

Accepted recommendations should set `primary_category_source` to `recommendation`.

---

## 6. Recommendation Sources

Use conservative local data first:

- process name
- display name
- user alias
- product name
- file description
- company name
- known app dictionary
- known Windows system process list

Avoid weak broad rules such as "path contains Program Files". They usually do not classify intent well.

---

## 7. UI Direction

Add recommendation support to the App Category Management screen.

Possible columns:

- Recommended category
- Recommendation reason
- Category source
- Review needed

Possible actions:

- Apply recommendation to selected apps.
- Apply recommendations to all visible unclassified apps.
- Ignore recommendation for selected apps.
- Filter by apps with recommendations.
- Filter by review-needed apps.

The UI should make clear whether a category is:

- unassigned
- manually assigned
- accepted from a recommendation
- imported or system-assigned

---

## 8. Safety Rules

- Do not auto-apply recommendations in the background.
- Do not overwrite `user` categories unless the user explicitly chooses an advanced overwrite action.
- For bulk actions, show the number of affected apps before applying.
- Keep undo support for the most recent category change where practical.
- Do not use online lookup without explicit user action.

---

## 9. Implementation Order

1. Add category source fields and storage migration.
2. Mark existing manually managed categories as user-owned where possible.
3. Add a local known-app recommendation service.
4. Show recommended category and reason in App Category Management.
5. Add selected-app recommendation apply action.
6. Add visible unclassified-app batch apply action.
7. Defer online lookup, advanced history, and automatic rules.

---

## 10. Open Decisions

- Whether accepted recommendations should remain distinguishable from direct user choices forever.
- Whether ignored recommendations need to be stored.
- Whether user-defined categories should be eligible recommendation targets.
- Whether recommendation confidence should be displayed or only used internally.
- Whether category change history is needed before bulk recommendation apply.
