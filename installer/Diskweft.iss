; Independent Diskweft identity. S5 owns any SpaceScan system replacement.
#ifndef AppVersion
  #define AppVersion "0.1.0"
#endif
#ifndef BuildRoot
  #define BuildRoot "..\.local\development"
#endif
[Setup]
AppId={{5F94B63F-3FCB-4EAF-AF7E-28AA846D6215}
AppName=Diskweft
AppVersion={#AppVersion}
AppPublisher=Darkaxt
AppPublisherURL=https://github.com/Darkaxt/Diskweft
VersionInfoVersion={#AppVersion}
PrivilegesRequired=admin
DefaultDirName={autopf}\Darkaxt\Diskweft
DefaultGroupName=Diskweft
UninstallDisplayIcon={app}\Diskweft.exe
UninstallDisplayName=Diskweft
ArchitecturesAllowed=x64compatible
CloseApplications=yes
CloseApplicationsFilter=Diskweft.exe
RestartApplications=no
OutputDir=..\dist
OutputBaseFilename=Diskweft-{#AppVersion}-setup
SetupIconFile=..\Diskweft.ico
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
LicenseFile=..\LICENSE
[Languages]
Name: "en"; MessagesFile: "compiler:Default.isl"
[Tasks]
Name: "shellmenu"; Description: "Add Scan with Diskweft to folder and drive context menus"; GroupDescription: "Windows integration:"; Flags: unchecked
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Shortcuts:"; Flags: unchecked
[Files]
Source: "{#BuildRoot}\Diskweft.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildRoot}\Diskweft.exe.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\Diskweft.ico"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\LICENSE"; DestDir: "{app}"; Flags: ignoreversion
[Icons]
Name: "{group}\Diskweft"; Filename: "{app}\Diskweft.exe"
Name: "{autodesktop}\Diskweft"; Filename: "{app}\Diskweft.exe"; Tasks: desktopicon
[Registry]
Root: HKCU; Subkey: "Software\Darkaxt\Diskweft"; ValueType: string; ValueName: "ExePath"; ValueData: "{app}\Diskweft.exe"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\Directory\shell\Diskweft"; ValueType: string; ValueData: "Scan with Diskweft"; Tasks: shellmenu; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\Directory\shell\Diskweft\command"; ValueType: string; ValueData: """{app}\Diskweft.exe"" ""%1"""; Tasks: shellmenu; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\Drive\shell\Diskweft"; ValueType: string; ValueData: "Scan with Diskweft"; Tasks: shellmenu; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\Drive\shell\Diskweft\command"; ValueType: string; ValueData: """{app}\Diskweft.exe"" ""%1"""; Tasks: shellmenu; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\Directory\Background\shell\Diskweft"; ValueType: string; ValueData: "Scan with Diskweft"; Tasks: shellmenu; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\Directory\Background\shell\Diskweft\command"; ValueType: string; ValueData: """{app}\Diskweft.exe"" ""%V"""; Tasks: shellmenu; Flags: uninsdeletekey
[Run]
Filename: "{app}\Diskweft.exe"; Description: "Start Diskweft"; Flags: nowait postinstall runasoriginaluser; Check: ShouldLaunch
[Code]
function ShouldLaunch: Boolean;
var
  I: Integer;
begin
  Result := True;
  for I := 1 to ParamCount do
    if CompareText(ParamStr(I), '/NOLAUNCH') = 0 then
      Result := False;
end;
