# Brand Naming Review

This note records the Store-release naming concern discovered during release preparation.
It is a product planning note, not legal advice.

## Current Risk

The original project name, `TimePilot`, should be treated as risky for public Store release.

Research found an existing `TimePilot` product and company in the time and attendance space:

- TimePilot Corporation operates products under the `TimePilot` name for employee time clocks, mobile time tracking, PC time clocks, and cloud/on-premise time attendance software.
- A `TIMEPILOT` trademark record exists for computerized time and attendance hardware and software.

The current app scope is not identical because it focuses on local personal PC usage understanding rather than employee attendance and payroll. However, the overlap around time tracking, PC software, productivity, and usage records is close enough that the public product name should change before Microsoft Store submission.

## Rejected Candidate: DeskTrace

`DeskTrace` was briefly selected as a public-facing candidate, but it should not be used.

Later review found `DeskTrack`, a time tracking, employee monitoring, and productivity analytics product with mobile app presence. Because `DeskTrace` and `DeskTrack` are close in spelling, sound, and product area, `DeskTrace` is too risky for the same release context.

## Branding Decision

The public product name is now resolved as `ActiveLogbook`.

Use `ActiveLogbook` for public-facing Store, website, privacy, support, and app display text. Use `액티브 로그북` only as a Korean reading aid when the surrounding text is Korean.

Complete the public-facing rename before:

- reserving a Microsoft Store app name
- publishing Store listing text
- finalizing public website slugs
- updating privacy and support pages
- publishing a paid or Pro edition

The internal repository, namespaces, storage path, event keys, and project files do not need to be renamed immediately. The first pass should change only user-visible branding.

## Selected Name: ActiveLogbook

`ActiveLogbook` is the selected public product name.

Why it fits:

- It communicates a personal activity record rather than employee surveillance.
- It matches the product direction: helping the user understand what they did on the computer, where time went, and how focus changed.
- It avoids the direct `TimePilot` conflict in the time and attendance software space.
- Initial web searches did not show an obvious active PC usage monitoring product using the exact `ActiveLogbook` name.

Known cautions:

- `Active` and `Logbook` are common descriptive words, so the Store listing should use a descriptive subtitle.
- Search results include generic "active logbook" phrases in other domains such as driver logs and aviation logs.
- This initial review is not a substitute for a formal trademark search.

## Naming Plan

- Public app name: `ActiveLogbook`
- Korean reading aid: `액티브 로그북`
- Store title: `ActiveLogbook - PC Usage Insights`
- Korean Store title: `ActiveLogbook - PC 사용 기록 분석`
- Product description first line: `ActiveLogbook helps you understand what you did on your Windows PC and where your time went.`
- Korean description first line: `ActiveLogbook(액티브 로그북)은 Windows PC에서 무엇을 했고 시간이 어디에 쓰였는지 이해할 수 있게 도와주는 로컬 사용 기록 앱입니다.`
- Public page: `https://ys-bookcase.com/active-logbook/`
- Privacy policy: `https://ys-bookcase.com/active-logbook/privacy-policy/`
- Support page: `https://ys-bookcase.com/active-logbook/support/`
- Support email: `support@ys-bookcase.com`

## Publisher And Business Name Notes

For the current individual Microsoft Store developer registration, use a publisher display name that is clearly tied to the existing personal brand but does not pretend to be a registered company.

Recommended individual publisher display name:

- `YS Bookcase`

If a business registration or company developer account is created later, keep the company publisher name distinct from the individual account name. This avoids confusion if Microsoft treats publisher display names as account-specific identifiers, and it makes the personal test release easier to separate from a later commercial publishing account.

Preferred future business brand candidate:

- English: `YS Bookcase Works`
- Korean: `와이에스 북케이스 웍스`
- Natural Korean description: `YS Bookcase 제작소`

Why `Works` fits:

- It can cover Windows apps, games, web content, reviews, video production, and advertising-related work.
- It sounds broader than `Software`, which may be too narrow if game and media projects grow.
- It sounds less like a photo, video, or performance studio than `Studio`.
- It sounds more production-ready than `Labs`, which can feel experimental.
- It is more distinctive than `Digital`, which is broad but generic.

Other name styles considered:

- `YS Bookcase Software`: strong for apps and tools, but narrow for games, video, and content.
- `YS Bookcase Studio`: good for games and media, but can sound like photo/video/performance work and less like software tools.
- `YS Bookcase Labs`: good for experimental apps and prototypes, but lighter as a commercial publisher name.
- `YS Bookcase Digital`: broad enough for web, apps, content, and advertising, but less distinctive.

The current direction is to use `YS Bookcase` for the individual developer account and reserve `YS Bookcase Works` as the likely future business or publisher brand if commercial releases become serious enough to justify business registration.

## Store Release Impact

Before Store submission, update:

- app window titles and About dialog
- README product name and links
- privacy policy and support page titles
- WordPress page slugs
- installer display name
- Store listing screenshots and descriptions
- release notes

The GitHub repository name can remain `TimePilot` during the transition if needed, but public-facing Store and website branding should use `ActiveLogbook`.

## References

- Existing TimePilot product site: https://www.timepilot.com/
- TimePilot trademark listing: https://trademarks.justia.com/789/68/timepilot-78968293.html
- DeskTrack product site: https://desktrack.timentask.com/
- Microsoft Store policies: https://learn.microsoft.com/en-us/windows/apps/publish/store-policies
