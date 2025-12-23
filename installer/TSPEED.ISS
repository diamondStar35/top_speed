#define GameOrg "Playing in the dark"
#define GameOrgURL "playinginthedark.net"
#define GameLongName "Top Speed"
#define GameShortName "Tspeed"
#define GameGlobalVer "3"
#define GameDetailVer "3.0"
#define GameCR "©2003-2011 Playing in the dark"
#define GameBuild "3"

[Setup]
AppName={#GameLongName} {#GameGlobalVer}
AppVerName={#GameOrg} {#GameLongName} {#GameDetailVer}.{#GameBuild}
AppPublisher={#GameOrg} & Ruby AccessWare
AppPublisherURL=http://www.{#GameOrgURL}/
AppSupportURL=http://www.{#GameOrgURL}/
AppUpdatesURL=http://www.{#GameOrgURL}/
AppComments=A sound based racing game by {#GameOrg} (http://www.{#GameOrgURL}/), with special thanks to Ruby AccessWare (http://www.rubyaccess.com/)
AppMutex={#GameShortName}_{#GameGlobalVer}
DefaultDirName={pf}\{#GameOrg}\{#GameLongName} {#GameGlobalVer}
DefaultGroupName={#GameOrg}\{#GameLongName} {#GameGlobalVer}
Compression=lzma2
LZMAAlgorithm=1
LZMAMatchFinder=BT
LZMANumBlockThreads=2
SolidCompression=true
InternalCompressLevel=max
OutputBaseFilename={#GameShortName}_{#GameDetailVer}.{#GameBuild}
MinVersion=4.1.1998,5.0.2195
AppCopyright={#GameCR}
PrivilegesRequired=none
AlwaysShowDirOnReadyPage=true
AlwaysShowGroupOnReadyPage=true
ShowLanguageDialog=auto
AppVersion={#GameDetailVer}
AppID={{24F6F231-6FCA-462D-A856-B7F6DC2F0E90}
UninstallFilesDir={app}\Uninstall
AppContact=info@{#GameOrgURL}
UninstallDisplayName={#GameOrg} {#GameLongName} {#GameDetailVer}.{#GameBuild}
UserInfoPage=false
AlwaysShowComponentsList=true
AllowNoIcons=false
AlwaysUsePersonalGroup=true
DirExistsWarning=no
UninstallLogMode=append
VersionInfoVersion={#GameDetailVer}
VersionInfoCompany={#GameOrg} & Ruby AccessWare
VersionInfoDescription=A sound based racing game by {#GameOrg} (http://www.{#GameOrgURL}/), with special thanks to Ruby AccessWare (http://www.rubyaccess.com/)
VersionInfoCopyright={#GameCR}
VersionInfoTextVersion={#GameDetailVer}
VersionInfoProductName={#GameLongName}
VersionInfoProductVersion={#GameDetailVer}
DisableStartupPrompt=true
SetupIconFile={#GameShortName}\TopSpeed.ICO
UninstallDisplayIcon={app}\TopSpeed.ico
WizardImageFile={#GameShortName} instlogo.bmp
FlatComponentsList=true
ShowComponentSizes=true
ShowTasksTreeLines=true
;Encryption=true
WizardSmallImageFile={#GameShortName} instlogo.bmp
;Password=HumbleBumble97Km

[Types]
Name: full; Description: Full
Name: coreonly; Description: Core only
Name: custom; Description: Custom; Flags: iscustom

[Components]
Name: core; Description: {#GameLongName} {#GameDetailVer} Core; Types: full coreonly
Name: examples; Description: Track and vehicle Examples; Types: full
Name: scripts; Description: Screen reader script files; Types: full
Name: en; Description: Language: English; Types: full
Name: en\sounds; Description: Sounds; Types: full
Name: en\manual; Description: Manual; Types: full

[Tasks]
Name: {#GameShortName}_desktop; Description: {cm:CreateDesktopIcon}; GroupDescription: {cm:AdditionalIcons}; Components: core

[Files]
;Core
Source: {#GameShortName}\*; DestDir: {app}; Components: core; Flags: ignoreversion
Source: {#GameShortName}\effects\*; DestDir: {app}\Effects; Components: core; Flags: ignoreversion
;examples
Source: {#GameShortName}\Tracks\*; DestDir: {app}\Tracks; Components: examples; Flags: ignoreversion
Source: {#GameShortName}\Vehicles\*; DestDir: {app}\Vehicles; Components: examples; Flags: ignoreversion
;Scripts
Source: {#GameShortName}\Scripts\*; DestDir: {app}\Scripts; Components: scripts; Flags: ignoreversion
;English
Source: {#GameShortName}\Sounds\En\Menu\*; DestDir: {app}\Sounds\En\Menu; Components: en/sounds; Flags: ignoreversion
Source: {#GameShortName}\Sounds\En\Numbers\*; DestDir: {app}\Sounds\En\Numbers; Components: en/sounds; Flags: ignoreversion
Source: {#GameShortName}\Sounds\En\Music\*; DestDir: {app}\Sounds\En\Music; Components: en/sounds; Flags: ignoreversion
Source: {#GameShortName}\Sounds\En\Race\*; DestDir: {app}\Sounds\En\Race; Components: en/sounds; Flags: ignoreversion
Source: {#GameShortName}\Sounds\En\Race\Copilot\*; DestDir: {app}\Sounds\En\Race\Copilot; Components: en/sounds; Flags: ignoreversion
Source: {#GameShortName}\Sounds\En\Race\Info\*; DestDir: {app}\Sounds\En\Race\Info; Components: en/sounds; Flags: ignoreversion
Source: {#GameShortName}\Sounds\En\Race\Multiplayer\*; DestDir: {app}\Sounds\En\Race\Multiplayer; Components: en/sounds; Flags: ignoreversion
Source: {#GameShortName}\Sounds\En\Race\Time\*; DestDir: {app}\Sounds\En\Race\Time; Components: en/sounds; Flags: ignoreversion
Source: {#GameShortName}\Sounds\En\Tracks\*; DestDir: {app}\Sounds\En\Tracks; Components: en/sounds; Flags: ignoreversion
Source: {#GameShortName}\Sounds\En\Vehicles\*; DestDir: {app}\Sounds\En\Vehicles; Components: en/sounds; Flags: ignoreversion
Source: {#GameShortName}\Sounds\En.ogg; DestDir: {app}\Sounds; Components: en/sounds; Flags: ignoreversion
Source: {#GameShortName}\Manual\en.html; DestDir: {app}\Manual; Components: en/manual; Flags: isreadme ignoreversion

[INI]
Filename: {group}\{cm:ProgramOnTheWeb,{#GameOrg}}.url; Section: InternetShortcut; Key: URL; String: http://www.{#GameOrgURL}/; Flags: createkeyifdoesntexist
Filename: {group}\{cm:ProgramOnTheWeb,Ruby AccessWare}.url; Section: InternetShortcut; Key: URL; String: http://www.rubyaccess.com/; Flags: createkeyifdoesntexist
Filename: {group}\Donate for {#GameLongName} {#GameGlobalVer}.url; Section: InternetShortcut; Key: URL; String: http://www.{#GameOrgURL}/donate_e.php; Flags: createkeyifdoesntexist

[Icons]
Name: {group}\Play {#GameLongName} {#GameDetailVer}; Filename: {app}\TopSpeed.exe; WorkingDir: {app}; IconFilename: {app}\TopSpeed.ICO; Flags: createonlyiffileexists; Components: core
Name: {group}\{cm:UninstallProgram,{#GameLongName} {#GameGlobalVer}}; Filename: {uninstallexe}; Flags: createonlyiffileexists
Name: {group}\{#GameLongName} {#GameGlobalVer} Manual (English); Filename: {app}\Manual\en.html; Flags: runmaximized createonlyiffileexists; WorkingDir: {app}\manual; Components: en/manual
Name: {group}\{#GameLongName} {#GameGlobalVer} Configuration Tool; Filename: {app}\ConfigTool.exe; WorkingDir: {app}; Flags: createonlyiffileexists; Components: core
Name: {userdesktop}\{#GameLongName} {#GameGlobalVer}; Filename: {app}\topspeed.exe; Tasks: {#GameShortName}_desktop; WorkingDir: {app}; IconFilename: {app}\TopSpeed.ico; Flags: createonlyiffileexists; Components: core

[Run]
Filename: {app}\TopSpeed.exe; Description: {cm:LaunchProgram,{#GameLongName} {#GameGlobalVer}}; Flags: nowait postinstall skipifsilent unchecked; WorkingDir: {app}; Components: core

[InstallDelete]
Name: {group}; Type: filesandordirs

[UninstallDelete]
Name: {app}; Type: filesandordirs
Name: {group}; Type: filesandordirs

[Languages]
Name: en; MessagesFile: langs\en.isl

[Dirs]
Components: core; Name: {app}\Tracks
Components: core; Name: {app}\Vehicles
