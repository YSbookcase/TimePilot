# Brand Naming Review

This note records the Store-release naming concern discovered during release preparation.
It is a product planning note, not legal advice.

## Current Risk

The current project name, `TimePilot`, should be treated as risky for public Store release.

Research found an existing `TimePilot` product and company in the time and attendance space:

- TimePilot Corporation operates products under the `TimePilot` name for employee time clocks, mobile time tracking, PC time clocks, and cloud/on-premise time attendance software.
- A `TIMEPILOT` trademark record exists for computerized time and attendance hardware and software.

TimePilot's current app scope is not identical because it focuses on local PC usage monitoring rather than employee attendance and payroll. However, the overlap around time tracking, PC software, productivity, and usage records is close enough that the name should be reconsidered before Microsoft Store submission.

## Preferred Direction

Choose a new public product name before:

- reserving a Microsoft Store app name
- publishing Store listing text
- finalizing public website slugs
- updating privacy and support pages
- publishing a paid or Pro edition

The internal repository, namespaces, and project files do not need to be renamed immediately. The first pass can change only user-visible branding.

## Candidate: DeskTrace

`DeskTrace` is currently the preferred candidate.

Why it fits:

- It suggests desktop or PC activity records.
- It is more directly connected to computer usage monitoring than a generic productivity name.
- Initial web searches did not show an obvious active software product or trademark using the exact `DeskTrace` name.
- It avoids the direct `TimePilot` conflict in the time and attendance software space.

Known cautions:

- Many products use the generic word `Trace`, so the Store listing should use a descriptive subtitle such as `DeskTrace - PC Usage Monitor`.
- `desktrace.com` appears in domain and redirect index results, so the public website can use the existing owner domain instead: `https://ys-bookcase.com/desktrace/`.
- This initial review is not a substitute for a formal trademark search.

## Provisional Naming Plan

- Public app name: `DeskTrace`
- Store title candidate: `DeskTrace - PC Usage Monitor`
- Product description first line: `DeskTrace helps you understand how your Windows PC time is spent.`
- Public page candidate: `https://ys-bookcase.com/desktrace/`
- Privacy policy candidate: `https://ys-bookcase.com/desktrace/privacy-policy/`
- Support page candidate: `https://ys-bookcase.com/desktrace/support/`
- Support email: `support@ys-bookcase.com`

## Store Release Impact

Before Store submission, update:

- app window titles and About dialog
- README product name and links
- privacy policy and support page titles
- WordPress page slugs
- installer display name
- Store listing screenshots and descriptions
- release notes

The GitHub repository name can remain `TimePilot` during the transition if needed, but public-facing Store and website branding should use the final product name.

## References

- Existing TimePilot product site: https://www.timepilot.com/
- TimePilot trademark listing: https://trademarks.justia.com/789/68/timepilot-78968293.html
- Microsoft Store policies: https://learn.microsoft.com/en-us/windows/apps/publish/store-policies
