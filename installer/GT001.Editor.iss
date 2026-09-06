; Builds a conventional, elevated Windows installer for the framework-dependent WPF publish.
; AppVersion and SourceDir are supplied by scripts\build-installer.ps1.

#ifndef AppVersion
  #error AppVersion must be supplied with /DAppVersion=x.y.z
#endif
#ifndef SourceDir
  #error SourceDir must be supplied with /DSourceDir=publish-folder
#endif

#define AppName "GT-001 Editor"
#define AppExeName "GT001.Editor.App.exe"

[Setup]
AppId={{BFB8243E-53C2-4CD7-ABFB-452A4DEAEF11}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher=GT-001 Editor
DefaultDirName={autopf}\GT-001
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
OutputDir=..\artifacts\installer
OutputBaseFilename=GT001.Editor-Setup-{#AppVersion}-win-x64
SetupIconFile=..\GTe.ico
UninstallDisplayIcon={app}\{#AppExeName}
Compression=lzma2
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
CloseApplications=yes
RestartApplications=no

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

; The installation directory is installer-owned. Clear obsolete runtime files from
; earlier self-contained versions before placing the current framework-dependent app.
[InstallDelete]
Type: filesandordirs; Name: "{app}\*"

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Launch {#AppName}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}"
