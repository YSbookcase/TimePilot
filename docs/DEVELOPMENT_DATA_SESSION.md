# Development With Existing Store Data

Use this opt-in workflow when the development PC is also the daily recording PC.
It avoids building an MSIX for every UI change. It is not a read-only test mode.

## Configure Once

First confirm the active data folder in the Store app's installation/data details.
Then run from the repository root, substituting that exact folder:

```powershell
.\scripts\configure-development-data.ps1 -DataDirectory "$env:LOCALAPPDATA\Packages\YSBookcase.ActiveLogbook_qx0xt5p8pr0jp\LocalState\TimePilot"
```

This generates the Git-ignored `TimePilot.WinForms/Properties/launchSettings.json`
with a `SharedStoreData` profile. It does not read, copy, or change the database.
Existing unrelated launch profiles are preserved. The local absolute path must
not be committed. Re-run the script to explicitly choose a different folder.

In Visual Studio select the WinForms startup project and `SharedStoreData` next
to the debug button. Build and debug normally. In a terminal:

```powershell
dotnet run --project TimePilot.WinForms --launch-profile SharedStoreData
```

An equivalent direct launch is:

```powershell
.\TimePilot.WinForms\bin\Debug\net8.0-windows10.0.19041.0\ActiveLogbook.exe --data-dir "C:\absolute\existing\data-folder"
```

## Daily Workflow

- Exit the running Store or older development app once before the first launch.
  The existing global single-instance mutex is retained; a second writer is not
  allowed. The development app never forcibly terminates the recording app.
- Keep the development app running after testing to continue recording into the
  same selected database. Exit it before rebuilding, or stop debugging first.
- The title and notification icon show `[DEV]` for an explicit data session.
- A fresh, verified full ZIP is created before each development launch under
  `%LOCALAPPDATA%\ActiveLogbookDevelopment\backups`. It uses SQLite's snapshot
  backup, not a copy of a potentially live DB file. These ZIPs contain private
  data, are not uploaded, and are not automatically deleted by backup retention.
  Repeated launches can consume disk space; review them manually when appropriate.
- Settings changed in this session are the real selected settings, not a separate
  test copy. Recording, classification, deletion and restore commands are real too.
  Do not use destructive actions casually. Window/sort changes also persist.
- Automatic startup synchronization, registration changes, and first-run startup
  prompts are suppressed in this mode. Startup controls in Preferences are locked.
  The Store startup registration is left intact; after a reboot the Store version
  may resume recording. Exit it before starting the development app again.
- To return to Store, exit the development app and open the installed Store app.
  It sees the continued data because its original folder was never moved.
  An older Store build may discard newer settings fields when saving; continuity
  of records does not guarantee preservation of settings unknown to that version.

## Safety And Limits

Without `--data-dir`, existing EXE/MSIX location selection is unchanged. With it,
the folder must be absolute and already contain `timepilot.db` and a valid typed
`settings.json`. Packaged launches and maintenance/sample-data argument combinations
are rejected. Missing data, invalid settings, or failed backup/verification abort
the launch; they do not create an empty database or fall back to another folder.

The option deliberately bypasses automatic location migration. It does not grant
automatic merging of records previously written into the legacy EXE folder, or
additional Windows permissions. Normal database initialization still runs once the
backup succeeds. Only use a build compatible with the Store database/settings.
Schema migrations or incompatible format changes require a disposable snapshot,
not this daily-data profile. A safety backup is recovery material, not a guarantee
that arbitrary development code cannot damage data or lose records.

Pro work should pin a known Community revision and use separate test data until
its read-only integration is verified. This launch profile is not a Pro data API.
MSIX remains necessary for packaging, Windows startup and Store update validation.

## Close And Relaunch Check

Check the local development app launched with `SharedStoreData`, not the installed
Store executable. Exit fully from the tray menu, then launch the same profile
again. Both launches must show `[DEV]`, the same database path in installation/data
details, and the previously recorded activity. Opening an EXE without arguments
may select another data location and is not an equivalent test.

Grey Windows startup and startup display-mode controls are intentional in this
profile. Change and validate actual Windows startup using the installed MSIX app.
