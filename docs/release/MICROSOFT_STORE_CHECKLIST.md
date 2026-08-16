# Microsoft Store Release Checklist

This checklist tracks non-code work needed before submitting TimePilot to the Microsoft Store.

TimePilot should continue to treat the Store release as an early public test until the product, privacy policy, packaging, and support flows are stable.

## Release Position

- [ ] Decide whether the next Store target is `v0.2.x` or `v0.3.0`.
- [ ] Keep the first Store listing free unless paid Pro operations, tax, payout, and support flows are ready.
- [ ] Use an individual developer account unless a company account is intentionally prepared later.
- [ ] State clearly that TimePilot is in early public testing before `v1.0`.
- [x] Resolve the public product name before reserving the Microsoft Store app name.
- [x] Review `docs/release/BRAND_NAMING_REVIEW.md` before finalizing Store branding.

## Partner Center

- [ ] Create or confirm a Microsoft Partner Center developer account.
- [ ] Reserve the app name.
- [ ] Confirm publisher display name before submission.
- [ ] Select category and age rating.
- [x] Prepare support contact information.
- [x] Prepare a public privacy policy URL.

## Prepared Public Links

- Current temporary official page: https://ys-bookcase.com/timepilot/
- Current temporary support page: https://ys-bookcase.com/timepilot/support/
- Current temporary privacy policy: https://ys-bookcase.com/timepilot/privacy-policy/
- DeskTrace official page target: https://ys-bookcase.com/desktrace/
- DeskTrace support page target: https://ys-bookcase.com/desktrace/support/
- DeskTrace privacy policy target: https://ys-bookcase.com/desktrace/privacy-policy/
- Support email: support@ys-bookcase.com

## Packaging Choice

Microsoft currently supports both MSIX and MSI/EXE submission paths for Win32 apps.

### MSIX path

- [ ] Create an MSIX package for TimePilot.
- [ ] Verify install, launch, uninstall, and update behavior.
- [ ] Verify local data is preserved or removed according to the intended uninstall policy.
- [ ] Confirm startup registration behavior works under packaged deployment.
- [ ] Confirm single-instance behavior works under packaged deployment.

### MSI/EXE path

- [ ] Confirm the existing Inno Setup installer can be submitted as a Store installer.
- [ ] Host a versioned HTTPS installer URL.
- [ ] Sign the installer and relevant PE files with a certificate chaining to a CA in the Microsoft Trusted Root Program.
- [ ] Document update responsibility because Store-managed updates are not available for this path.
- [ ] Verify installer, launch, uninstall, and local data preservation behavior.

## Store Listing Assets

- [ ] App short description.
- [ ] App full description.
- [ ] Feature list focused on local usage tracking.
- [ ] Screenshots for summary, timeline, detail, settings, and tray behavior.
- [ ] App icon and Store images.
- [ ] Release notes.
- [ ] Known limitations.
- [ ] Link to GitHub repository.
- [x] Link to support page or GitHub Issues.

## Privacy And Trust

- [x] Publish the TimePilot-specific privacy policy page.
- [ ] Link the privacy policy in Partner Center.
- [x] Link the privacy policy from README or app support documentation.
- [ ] Explain that usage data is stored locally by default.
- [ ] Explain that TimePilot does not send usage records to a developer server by default.
- [ ] Explain that TimePilot does not collect window titles, URLs, web page titles, document names, command lines, keystrokes, or screenshots by default.
- [ ] Explain data export and backup responsibilities.
- [ ] Explain how users can delete local data.
- [ ] Keep the privacy policy updated when tracking behavior changes.

## Functional Verification

- [ ] Build succeeds.
- [ ] Tests pass.
- [ ] Fresh install launches TimePilot.
- [ ] Foreground app usage is recorded.
- [ ] Idle time is separated.
- [ ] Background process runtime tracking behaves as expected.
- [ ] Tray resident mode works.
- [ ] Windows startup preference works.
- [ ] Single-instance behavior works.
- [ ] CSV export works.
- [ ] Raw data export works.
- [ ] Backup creation works.
- [ ] Restore flow works according to the current restore policy.
- [ ] Clear local data flow works.

## References

- Microsoft Store policies: https://learn.microsoft.com/en-us/windows/apps/publish/store-policies
- Publish your first Windows app: https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/publish-first-app
- Get started with Microsoft Store: https://learn.microsoft.com/en-us/windows/apps/publish/get-started
- Choose a distribution path: https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/choose-distribution-path

