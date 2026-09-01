#define MyAppName "ActiveLogbook"
#define MyAppPublisher "YSbookcase"
#define MyAppExeName "ActiveLogbook.exe"
#ifndef AppVersion
#define AppVersion "0.2.2"
#endif
#ifndef SourceDir
#define SourceDir "..\..\artifacts\release\publish\win-x64"
#endif
#ifndef OutputDir
#define OutputDir "..\..\artifacts\release"
#endif

[Setup]
AppId={{B1C2D7C2-0B18-4F41-9B72-9D1B6B92F412}
AppName={#MyAppName}
AppVersion={#AppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL=https://ys-bookcase.com/active-logbook/
AppSupportURL=https://ys-bookcase.com/active-logbook/support/
AppUpdatesURL=https://github.com/YSbookcase/TimePilot/releases
AppVerName={#MyAppName} {#AppVersion}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
LicenseFile=..\..\LICENSE
OutputDir={#OutputDir}
OutputBaseFilename=ActiveLogbook-{#AppVersion}-Setup
SetupIconFile=..\..\TimePilot.WinForms\Assets\TimePilot.ico
Compression=lzma
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayName={#MyAppName}
UninstallDisplayIcon={app}\{#MyAppExeName}
UsePreviousAppDir=no
CloseApplications=yes
CloseApplicationsFilter={#MyAppExeName},TimePilot.WinForms.exe
RestartApplications=no

[Languages]
Name: "korean"; MessagesFile: "compiler:Languages\Korean.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[InstallDelete]
Type: filesandordirs; Name: "{app}\*"

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Parameters: "--set-ui-language {language}"; Flags: runhidden waituntilterminated
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent; Check: not IsAppRunning

[UninstallDelete]
Type: filesandordirs; Name: "{app}\*"
Type: dirifempty; Name: "{app}"

[Code]
function IsAppRunning(): Boolean;
begin
  Result := CheckForMutexes('TimePilot.SingleInstance');
end;

function ConfirmCloseRunningApp(): Boolean;
var
  MessageText: string;
begin
  if ActiveLanguage = 'korean' then
    MessageText :=
      'ActiveLogbook이 현재 실행 중입니다.' + #13#10 + #13#10 +
      '실행 중인 앱을 종료하고 계속하시겠습니까?'
  else
    MessageText :=
      'ActiveLogbook is currently running.' + #13#10 + #13#10 +
      'Do you want to close the running app and continue?';

  Result := MsgBox(MessageText, mbConfirmation, MB_YESNO) = IDYES;
end;

procedure RequestRunningAppShutdown();
var
  ResultCode: Integer;
  AppPath: string;
begin
  AppPath := ExpandConstant('{app}\{#MyAppExeName}');
  if FileExists(AppPath) then
  begin
    Exec(AppPath, '--shutdown', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    Sleep(1500);
  end;
end;

function WaitForRunningAppExit(): Boolean;
var
  Attempt: Integer;
begin
  Result := True;
  for Attempt := 1 to 20 do
  begin
    if not IsAppRunning() then
      exit;

    Sleep(500);
  end;

  Result := not IsAppRunning();
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  if IsAppRunning() then
  begin
    if not ConfirmCloseRunningApp() then
    begin
      if ActiveLanguage = 'korean' then
        Result := '사용자가 실행 중인 ActiveLogbook 종료를 취소했습니다.'
      else
        Result := 'The running ActiveLogbook instance was not closed.';

      exit;
    end;

    RequestRunningAppShutdown();
    WaitForRunningAppExit();
  end;

  Result := '';
end;

function InitializeUninstall(): Boolean;
begin
  if IsAppRunning() then
  begin
    if not ConfirmCloseRunningApp() then
    begin
      Result := False;
      exit;
    end;

    RequestRunningAppShutdown();
    WaitForRunningAppExit();
  end;

  Result := True;
end;
