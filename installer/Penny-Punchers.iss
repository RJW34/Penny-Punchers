; Compile through tools/build_installer.py so payload hashes are checked first.
#ifndef PayloadDir
  #error PayloadDir must point to the verified staged Windows package.
#endif
#ifndef InstallerOutputDir
  #error InstallerOutputDir must be supplied by the build tool.
#endif

[Setup]
AppId={{E20D99CC-399A-4C0C-B661-DA18FB522236}
AppName=Penny Punchers
AppVersion=1.0
AppVerName=Penny Punchers 1.0
AppPublisher=RJW34
AppPublisherURL=https://github.com/RJW34/Penny-Punchers
AppSupportURL=https://github.com/RJW34/Penny-Punchers/issues
AppUpdatesURL=https://github.com/RJW34/Penny-Punchers/releases
DefaultDirName={localappdata}\Programs\Penny Punchers
DefaultGroupName=Penny Punchers
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
WizardStyle=modern
Compression=lzma2/normal
LZMANumBlockThreads=2
SolidCompression=yes
OutputDir={#InstallerOutputDir}
OutputBaseFilename=Penny-Punchers-1.0-Windows-Setup
VersionInfoVersion=1.0.0.0
VersionInfoProductName=Penny Punchers
VersionInfoDescription=Penny Punchers 1.0 installer
UninstallDisplayName=Penny Punchers 1.0
UninstallDisplayIcon={app}\StrikeLedger.exe
SetupLogging=yes
CloseApplications=yes
RestartApplications=no
AllowNoIcons=yes

[Tasks]
Name: desktopicon; Description: "Create a &desktop shortcut"; GroupDescription: "Shortcuts:"; Flags: unchecked

[Files]
Source: "{#PayloadDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Penny Punchers"; Filename: "{app}\StrikeLedger.exe"; WorkingDir: "{app}"
Name: "{group}\Tester guide"; Filename: "{app}\TESTER_GUIDE.txt"; WorkingDir: "{app}"
Name: "{group}\Uninstall Penny Punchers"; Filename: "{uninstallexe}"
Name: "{autodesktop}\Penny Punchers"; Filename: "{app}\StrikeLedger.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\StrikeLedger.exe"; Description: "Launch Penny Punchers"; WorkingDir: "{app}"; Flags: nowait postinstall skipifsilent unchecked

; No broad deletion or AppData cleanup: uninstall removes its installed files,
; shortcuts and registration while preserving the player's settings/replays.
