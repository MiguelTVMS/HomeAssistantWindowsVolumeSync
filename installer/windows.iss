; HomeAssistant Windows Volume Sync - Inno Setup Script
; Installs to %LOCALAPPDATA%\HomeAssistantWindowsVolumeSync (user context, no admin required)
; Startup-at-login is managed by the application itself via HKCU\...\Run — not this installer.

#ifndef AppVersion
  #define AppVersion "0.0.0"
#endif

#define AppName "HomeAssistant Windows Volume Sync"
#define AppPublisher "João Miguel Tabosa Vaz Marques Silva"
#define AppURL "https://github.com/MiguelTVMS/HomeAssistantWindowsVolumeSync"
#define AppExeName "HomeAssistantWindowsVolumeSync.exe"
#define AppId "{{A7F2B3C4-D5E6-4F7A-8B9C-0D1E2F3A4B5C}"

[Setup]
AppId={#AppId}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}/issues
AppUpdatesURL={#AppURL}/releases
DefaultDirName={localappdata}\HomeAssistantWindowsVolumeSync
DisableProgramGroupPage=yes
LicenseFile=..\LICENSE
OutputDir=..\installer-output
OutputBaseFilename=HomeAssistantWindowsVolumeSync-{#AppVersion}-win-x64-install
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=
ArchitecturesInstallIn64BitMode=x64compatible
ArchitecturesAllowed=x64compatible
MinVersion=10.0
UninstallDisplayName={#AppName}
UninstallDisplayIcon={app}\{#AppExeName}
CloseApplications=yes
CloseApplicationsFilter=*{#AppExeName}*
RestartApplications=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
; All published application files (self-contained build)
Source: "..\publish\self-contained\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
; Start Menu shortcut
Name: "{userprograms}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Comment: "Sync Windows volume to Home Assistant"

[Run]
; Optional: launch the app after installation
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(AppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
; Stop the running app gracefully before uninstall
Filename: "taskkill.exe"; Parameters: "/IM ""{#AppExeName}"" /F"; Flags: runhidden; RunOnceId: "StopApp"

[UninstallDelete]
; Clean up user settings on uninstall (optional — only if user agrees via standard uninstall)
; Settings are in %APPDATA%\HomeAssistantWindowsVolumeSync — not removed automatically
; to preserve user config across reinstalls. Uncomment to enable cleanup:
; Type: filesandordirs; Name: "{userappdata}\HomeAssistantWindowsVolumeSync"

[Code]
// Remove the startup registry entry when uninstalling
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usPostUninstall then
  begin
    RegDeleteValue(HKCU, 'SOFTWARE\Microsoft\Windows\CurrentVersion\Run', 'HomeAssistantWindowsVolumeSync');
  end;
end;
