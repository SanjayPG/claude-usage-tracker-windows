; Builds ClaudeUsageTracker-Setup-<version>.exe with Inno Setup 6.
;
;   ISCC.exe installer\ClaudeUsageTracker.iss /DSourceExe=<path to self-contained ClaudeUsageTray.exe> /DOutputDir=<folder>
;
; Per-user install (no admin prompt) into %LOCALAPPDATA%\Programs. The exe is self-contained,
; so the .NET runtime is not a prerequisite.

#ifndef SourceExe
  #error Pass /DSourceExe=<path to ClaudeUsageTray.exe>
#endif
#ifndef OutputDir
  #define OutputDir "..\dist"
#endif
#define AppVersion "0.1.0"

[Setup]
AppId={{8F3D2C1A-6B4E-4A57-9C0D-5E1B7A2F9D34}
AppName=Claude Usage Tracker
AppVersion={#AppVersion}
AppPublisher=Sanjay Gorai
AppPublisherURL=https://github.com/SanjayPG/claude-usage-tracker-windows
AppSupportURL=https://github.com/SanjayPG/claude-usage-tracker-windows/issues
DefaultDirName={localappdata}\Programs\ClaudeUsageTracker
DefaultGroupName=Claude Usage Tracker
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputDir={#OutputDir}
OutputBaseFilename=ClaudeUsageTracker-Setup-v{#AppVersion}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
UninstallDisplayName=Claude Usage Tracker
UninstallDisplayIcon={app}\ClaudeUsageTray.exe
LicenseFile=..\LICENSE
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
; The app holds this mutex while running, so Setup asks to close it before replacing the exe.
AppMutex=Local\ClaudeUsageTray.SingleInstance
CloseApplications=no

[Tasks]
Name: "autostart"; Description: "Start Claude Usage Tracker when I sign in to Windows"; Flags: unchecked

[Files]
Source: "{#SourceExe}"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\LICENSE"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\Claude Usage Tracker"; Filename: "{app}\ClaudeUsageTray.exe"

[Registry]
; Same value the app's own "Start with Windows" menu item writes, so the two stay in step.
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "ClaudeUsageTray"; ValueData: """{app}\ClaudeUsageTray.exe"""; Tasks: autostart; Flags: uninsdeletevalue

[Run]
Filename: "{app}\ClaudeUsageTray.exe"; Description: "Start Claude Usage Tracker now"; Flags: nowait postinstall skipifsilent

[UninstallRun]
; A tray app has no window to close, so stop it before its files are removed.
Filename: "{sys}\taskkill.exe"; Parameters: "/F /IM ClaudeUsageTray.exe"; Flags: runhidden; RunOnceId: "StopTray"

[UninstallDelete]
; Settings and the last reading the app saved for itself.
Type: filesandordirs; Name: "{userappdata}\ClaudeUsageTray"
