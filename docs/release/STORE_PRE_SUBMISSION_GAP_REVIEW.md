# Store Pre-Submission Gap Review

This note maps the current open issues to Microsoft Store pre-submission readiness.
It is intended to prevent broad research issues from being mistaken for release blockers.

## Current Position

- Partner Center individual developer registration has been completed.
- Public release should remain a free early public test until Pro features and business operations are ready.
- The public app name should be resolved before reserving the Microsoft Store app name.
- Current open issues are mostly research or long-term architecture work, not direct submission blockers.

## Issue Triage

### Release-Blocking Before Store Submission

No currently open issue is a hard blocker by itself.

However, these checks should be completed before submission:

- Confirm the final public product name and Store title.
- Confirm the privacy policy and support page use the final product name and URL slug.
- Verify the current app does not enable optional detailed tracking by default.
- Verify Store screenshots do not imply Pro, cloud sync, URL tracking, or detailed tracking features that are not present.
- Verify install, launch, uninstall, tray, startup, backup, restore, export, and clear-local-data behavior.

### Should Be Documented Or Verified Before Submission

- #230 Local data encryption and app lock policy
  - Not required for the first free public test if the app continues to store only app-level usage locally by default.
  - Store privacy text should clearly say local data is not encrypted by default unless encryption is implemented.
  - If optional detailed tracking is added later, revisit encryption/app lock before enabling it broadly.

- #90 Optional detailed tracking mode
  - Not required for Store submission.
  - Must remain off and unimplemented unless explicit consent, privacy text, deletion policy, and storage schema are ready.
  - Current Store listing should avoid mentioning URL, window title, or browser history tracking as active features.

- #224 Pro feature separation
  - Not required for the first free public test.
  - Relevant before paid Pro, private Pro repository, licensing, or feature gating work begins.
  - Current listing should not advertise Pro features.

- #65 WinForms folder structure refactor
  - Not required for Store submission.
  - Useful before larger feature work, but release should prioritize user-visible stability and packaging checks.

## Post-Submission Candidate Work

- #230 can become a privacy/security milestone if detailed tracking is planned.
- #90 can become a Pro or advanced opt-in feature after privacy and deletion policies are ready.
- #224 should be revisited before any paid Pro release.
- #65 should continue as incremental refactoring when it reduces risk for concrete feature work.

## Recommended Next Development Focus

Before the first Store submission, prefer small verification and polish tasks:

- Finalize the public app name.
- Update user-visible branding once the name is final.
- Review the current summary, timeline, detail, settings, tray, backup, restore, and data-clear flows as Store screenshot candidates.
- Prepare Store listing text and screenshot checklist.
- Decide MSIX versus MSI/EXE packaging path.
- Run a fresh-install verification pass.
