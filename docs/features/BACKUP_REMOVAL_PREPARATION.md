# Backup And Removal Preparation (#324)

## Scope And Workflow

Preferences > Data and backup > Backup and removal preparation opens a separate
dialog. It does not save pending preferences, remove the app, launch an uninstaller,
request elevation, reset data or change Pro's public analytics contract.

1. Show the removal/reset warning and last known external backup success (path and
   time). History is not a current validation or permission to continue.
2. The user explicitly chooses a **new** ZIP outside the active data folder, entire
   package data root and application executable directory. Existing files are not
   overwritten. Symbolic-link/junction paths are rejected conservatively.
3. Reuse DataBackupService's SQLite snapshot backup/progress/archive validation,
   then additionally require the DB, settings and JSON object settings. Publish
   only the verified full ZIP from an owned staging file. Failed attempts clear
   readiness; a failed retry cannot reuse an older success.
4. Enable Open installed apps only after success in this dialog. Recheck the ZIP
   hash and inspect its contents on each click, so deletion or changes revoke
   readiness. A Windows Settings launch failure leaves a retry available only
   while the ZIP remains valid. Opening Settings is not an uninstall action.
5. The user closes the app from the tray and chooses removal in Windows. No backup
   includes future records. The recorder continues during this dialog; this is
   not a freeze/exit operation and does not promise an atomic DB/settings snapshot.

Windows removal initiated outside the app cannot be reliably intercepted. MSIX
removal/reset may delete package data. EXE/portable leftovers and external ZIPs
may remain. The app never deletes external backups or cleans up another installation
as part of this workflow. Plain ZIP backups contain private data and are not encrypted.
Cloud-synced folders, if accepted, can be uploaded by their provider, not by this app.
If cleanup fails because of permissions or file locks, an unpublished staging ZIP
may remain in the chosen external folder. It contains private data too; remove it
manually when it is no longer needed.

## Implementation Boundaries

- BackupRemovalPreparation owns external-path policy and verified-backup readiness.
  The full package root is protected even for a SharedStoreData development session.
- BackupRemovalForm owns the async dialog with progress and close/reentry guards.
- Form1.BackupRemoval composes the dialog and success-history updates. Automatic
  backup checks are suppressed while it is open. Normal recording is not restarted.
- LastExternalBackupAt/Path in AppSettings stores the last known success for
  accepted external manual/automatic/removal backups. Saving preferences defaults
  does not clear history. Missing/deleted files are never trusted from this metadata.
- Preferences Cancel does not undo an explicitly created backup or its history;
  other edited preferences still require Save. History-save failure is reported but
  does not turn a valid ZIP into a failed backup.
- The only system launch target is ms-settings:appsfeatures. It opens the Windows
  Apps page and leaves uninstall choice to the user. The app cannot guarantee that
  Windows removal UI will appear on managed or damaged systems.

The URI is documented by Microsoft:
https://learn.microsoft.com/en-us/windows/apps/develop/launch/launch-settings

## Verification And Publishing

Synthetic tests cover success, missing/corrupt DB/settings, package/internal paths,
failed retries, existing output preservation, changed/deleted ZIPs, history
roundtrip, UI async close/reentry, shell failure/retry and KO/EN layouts. They never
launch real Windows uninstall/settings UI or use production data.

Manually verify the Preferences entry, external file chooser (including cancel),
visible progress and completion, and the Windows installed-apps launch on Windows
10/11/MSIX before release. Do not actually uninstall daily data during testing.

Support draft/WordPress source and Store checklists are updated, not live-published.
The existing privacy draft has unrelated local edits, so it is intentionally not
included. Apply the removal wording in BACKUP_REMOVAL_PUBLICATION.ko.md to the
current privacy page during release review, preserving its other changes.
