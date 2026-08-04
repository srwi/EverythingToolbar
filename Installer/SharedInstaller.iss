#include "InnoDependencyInstaller/CodeDependencies.iss"
#include "WixUninstaller.iss"
#include "DotNetInstaller.iss"

#define MyAppName "EverythingToolbar"
#define MyAppPublisher "Stephan Rumswinkel"
#define MyAppURL "https://www.github.com/srwi/EverythingToolbar"
#define MyAppExeName "EverythingToolbar.Launcher.exe"

[Setup]
AppId={{b5f0ac2d-98da-4392-9d12-78444db9caa9}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
UninstallDisplayIcon={app}\{#MyAppExeName}
ArchitecturesAllowed={#ArchitecturesAllowed}
ArchitecturesInstallIn64BitMode={#ArchitecturesInstallIn64BitMode}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
OutputDir=output
OutputBaseFilename={#OutputBaseName}
SetupIconFile=..\EverythingToolbar\Images\AppIcon.ico
SolidCompression=yes
WizardStyle=modern
DisableWelcomePage=no
CloseApplications=force

[Files]
; Launcher files
Source: "{#LauncherBuildDir}\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion restartreplace uninsrestartdelete; Check: IsLauncherSelected
Source: "{#LauncherBuildDir}\*"; DestDir: "{app}"; Flags: ignoreversion restartreplace recursesubdirs createallsubdirs uninsrestartdelete; Check: IsLauncherSelected
; Deskband files (COM DLL)
Source: "{#DeskbandBuildDir}\*"; DestDir: "{app}"; Flags: ignoreversion restartreplace recursesubdirs createallsubdirs uninsrestartdelete; Check: IsDeskbandSelected

[Registry]
; Deskband COM registration entries
Root: HKCR; Subkey: "CLSID\{{9D39B79C-E03C-4757-B1B6-ECCE843748F3}"; ValueType: string; ValueName: ""; ValueData: "EverythingToolbar"; Flags: uninsdeletekey; Check: IsDeskbandSelected
Root: HKCR; Subkey: "CLSID\{{9D39B79C-E03C-4757-B1B6-ECCE843748F3}\Implemented Categories"; ValueType: string; ValueName: ""; ValueData: ""; Flags: uninsdeletekey; Check: IsDeskbandSelected
Root: HKCR; Subkey: "CLSID\{{9D39B79C-E03C-4757-B1B6-ECCE843748F3}\Implemented Categories\{{00021492-0000-0000-c000-000000000046}"; ValueType: string; ValueName: ""; ValueData: ""; Flags: uninsdeletekey; Check: IsDeskbandSelected
Root: HKCR; Subkey: "CLSID\{{9D39B79C-E03C-4757-B1B6-ECCE843748F3}\InProcServer32"; ValueType: string; ValueName: ""; ValueData: "{app}\EverythingToolbar.Deskband.comhost.dll"; Flags: uninsdeletekey; Check: IsDeskbandSelected
Root: HKCR; Subkey: "CLSID\{{9D39B79C-E03C-4757-B1B6-ECCE843748F3}\InProcServer32"; ValueType: string; ValueName: "ThreadingModel"; ValueData: "Both"; Flags: uninsdeletekey; Check: IsDeskbandSelected
Root: HKCR; Subkey: "CLSID\{{9D39B79C-E03C-4757-B1B6-ECCE843748F3}\ProgID"; ValueType: string; ValueName: ""; ValueData: "EverythingToolbar.Deskband.Server"; Flags: uninsdeletekey; Check: IsDeskbandSelected
Root: HKCR; Subkey: "EverythingToolbar.Deskband.Server"; ValueType: string; ValueName: ""; ValueData: "EverythingToolbar.Deskband.Server"; Flags: uninsdeletekey; Check: IsDeskbandSelected
Root: HKCR; Subkey: "EverythingToolbar.Deskband.Server\CLSID"; ValueType: string; ValueName: ""; ValueData: "{{9D39B79C-E03C-4757-B1B6-ECCE843748F3}"; Flags: uninsdeletekey; Check: IsDeskbandSelected

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Check: IsLauncherSelected

[UninstallDelete]
; Removing the installation directory is done in case some files were not removed by the uninstaller
Type: filesandordirs; Name: "{app}"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent; Check: IsLauncherSelected

[Code]
var
  InstallTypePage: TInputOptionWizardPage;
  AdminNoticeLabel: TNewStaticText;
  SelectedInstallMode: Integer;
  IsUpgrade: Boolean;
  SkipInstallTypePage: Boolean;
  ExplorerWasKilled: Boolean;
  OrigAutoRestartShell: Cardinal;

function IsLauncherSelected: Boolean;
begin
  Result := SelectedInstallMode = 0;
end;

function IsDeskbandSelected: Boolean;
begin
  Result := SelectedInstallMode = 1;
end;

function IsWindows11OrLater: Boolean;
begin
  Result := GetWindowsVersion >= $0A00016B;
end;

procedure KillAppIfRunning;
var
  ResultCode: Integer;
begin
  Exec('taskkill.exe', '/F /IM "{#MyAppExeName}"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
end;

procedure KillExplorerForDeskband;
var
  ResultCode: Integer;
begin
  // Save and disable AutoRestartShell to prevent Windows from auto-restarting Explorer
  if not RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon', 'AutoRestartShell', OrigAutoRestartShell) then
    OrigAutoRestartShell := 1;
  RegWriteDWordValue(HKLM, 'SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon', 'AutoRestartShell', 0);

  Exec('taskkill.exe', '/F /IM explorer.exe', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  ExplorerWasKilled := True;
  Sleep(1000);
end;

procedure RestartExplorer;
var
  ResultCode: Integer;
begin
  if not ExplorerWasKilled then
    Exit;

  RegWriteDWordValue(HKLM, 'SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon', 'AutoRestartShell', OrigAutoRestartShell);
  Exec(ExpandConstant('{win}\explorer.exe'), '', '', SW_SHOWNORMAL, ewNoWait, ResultCode);
  ExplorerWasKilled := False;
end;

function GetInstalledVersion(var sInstalledVersion: String): Boolean;
var
  sUnInstPath: String;
  sUnInstallString: String;
begin
  Result := False;
  sInstalledVersion := '';
  sUnInstPath := ExpandConstant('Software\Microsoft\Windows\CurrentVersion\Uninstall\{#SetupSetting("AppId")}_is1');

  // Check HKLM first
  if RegQueryStringValue(HKLM, sUnInstPath, 'UninstallString', sUnInstallString) then
  begin
    if RegQueryStringValue(HKLM, sUnInstPath, 'DisplayVersion', sInstalledVersion) then
    begin
      Result := True;
      Exit;
    end;
  end
  // Check HKCU if not found in HKLM
  else if RegQueryStringValue(HKCU, sUnInstPath, 'UninstallString', sUnInstallString) then
  begin
    if RegQueryStringValue(HKCU, sUnInstPath, 'DisplayVersion', sInstalledVersion) then
    begin
      Result := True;
      Exit;
    end;
  end;
end;

function IsSameVersionInstalled: Boolean;
var
  sInstalledVersion: String;
begin
  Result := False;
  if GetInstalledVersion(sInstalledVersion) then
  begin
    Result := (sInstalledVersion = '{#MyAppVersion}');
  end;
end;

function IsDeskbandInstalled: Boolean;
begin
  // Detect previous deskband install by presence of the registered CLSID
  Result := RegKeyExists(HKCR, 'CLSID\{9D39B79C-E03C-4757-B1B6-ECCE843748F3}');
end;

function SetSelectedModeFromPrevious: Boolean;
begin
  if IsDeskbandInstalled then
    SelectedInstallMode := 1
  else
    SelectedInstallMode := 0;

  Result := True;
end;

function InitializeUninstall: Boolean;
begin
  KillAppIfRunning;
  if IsDeskbandInstalled then
    KillExplorerForDeskband;
  Result := True;
end;

function InitializeSetup: Boolean;
var
  modeArg: String;
  modeArgSupplied: Boolean;
  sPrevVersion: String;
begin
  IsUpgrade := False;
  SkipInstallTypePage := False;

  // If exactly the same version is installed, cancel
  if IsSameVersionInstalled then
  begin
    MsgBox('EverythingToolbar version {#MyAppVersion} is already installed on this computer.' + #13#10 + #13#10 +
           'Installation will be cancelled.',
           mbInformation, MB_OK);
    Result := False;
    Exit;
  end;

  // Detect whether an older version is installed (upgrade scenario)
  if GetInstalledVersion(sPrevVersion) then
  begin
    IsUpgrade := True;
  end;

  // Read whether a /mode param was supplied (bool) and fallback param (launcher default)
  modeArgSupplied := (ExpandConstant('{param:mode}') <> '');
  modeArg := LowerCase(ExpandConstant('{param:mode|launcher}'));

  if IsUpgrade and (not modeArgSupplied) then
  begin
    // Auto-select previously installed mode and skip the selection page
    SetSelectedModeFromPrevious();
    SkipInstallTypePage := True;
  end
  else
  begin
    // Respect explicit CLI option or fallback logic
    if modeArg = 'launcher' then
      SelectedInstallMode := 0
    else if modeArg = 'deskband' then
      SelectedInstallMode := 1
    else
      if not IsWindows11OrLater and IsAdminInstallMode then
        SelectedInstallMode := 1
      else
        SelectedInstallMode := 0;
  end;

  // Enforce admin for deskband in all modes
  if (IsDeskbandSelected) and not IsAdminInstallMode then
  begin
    MsgBox('The Deskband installation requires administrator privileges. ' +
           'Please rerun the installer as administrator or use /mode=launcher.',
           mbError, MB_OK);
    Result := False;
    exit;
  end;

  AddDotNet80DesktopDependency;
  Result := UninstallWixVersion();
end;

procedure InitializeWizard;
begin
  // If this is an upgrade and we already know the previous mode, skip the selection page
  if SkipInstallTypePage then
  begin
    // Nothing to show; selection was already set in InitializeSetup
    exit;
  end;

  InstallTypePage := CreateInputOptionPage(wpWelcome,
    'Choose Installation Type', 'Select how you want to install EverythingToolbar',
    'Please specify which installation method you would like to use, then click Next.',
    True, False);
  InstallTypePage.Add(''#13#10'Launcher (Recommended for Windows 11):'#13#10'Pins EverythingToolbar as a regular taskbar icon ' +
                      'or embeds a search box on the Windows 11 taskbar. ' +
                      'This is the only option compatible with unmodified Windows 11 installations.'#13#10'');
  InstallTypePage.Add(''#13#10'Deskband (Requires Windows 10 or StartAllBack / ExplorerPatcher):'#13#10'' +
                      'Integrates the search bar directly into the taskbar. Only works on Windows 10 or ' +
                      'Windows 11 with third-party tools that restore deskband support.'#13#10'');

  // Set selection from CLI or fallback logic
  InstallTypePage.SelectedValueIndex := SelectedInstallMode;

  AdminNoticeLabel := TNewStaticText.Create(InstallTypePage);
  AdminNoticeLabel.Parent := InstallTypePage.Surface;
  AdminNoticeLabel.AutoSize := False;
  AdminNoticeLabel.Left := 0;
  AdminNoticeLabel.Top := InstallTypePage.Surface.Height - 100;
  AdminNoticeLabel.Width := InstallTypePage.SurfaceWidth;
  AdminNoticeLabel.Height := 80;
  AdminNoticeLabel.WordWrap := True;
  AdminNoticeLabel.Font.Style := [fsBold];
  AdminNoticeLabel.Font.Color := clRed;

  if not IsAdminInstallMode then
  begin
    InstallTypePage.CheckListBox.ItemEnabled[1] := False;
    InstallTypePage.SelectedValueIndex := 0;
    SelectedInstallMode := 0;
    AdminNoticeLabel.Caption := 'Note: To use the Deskband option, you must select “Install for all users.”';
    AdminNoticeLabel.Visible := True;
  end
  else
  begin
    AdminNoticeLabel.Visible := False;
  end;
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;

  if Assigned(InstallTypePage) and (CurPageID = InstallTypePage.ID) then
  begin
    SelectedInstallMode := InstallTypePage.SelectedValueIndex;
  end;
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  Result := '';
  KillAppIfRunning;
  if IsDeskbandSelected then
    KillExplorerForDeskband;
end;

procedure DeinitializeSetup;
begin
  RestartExplorer;
end;

procedure DeinitializeUninstall;
begin
  RestartExplorer;
end;