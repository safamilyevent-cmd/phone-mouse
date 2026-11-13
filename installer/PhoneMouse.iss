#define AppName "Phone Mouse"
#define AppVersion "1.0.0"
#define SourceDir "..\\dist\\PhoneMouse-win-x64"
#define IconFile "..\\PhoneMouse\\PhoneMouse.Desktop\\Assets\\PhoneMouse.ico"

[Setup]
AppId={{9A6C90C3-4F0D-4F1E-9D9A-3B7A6D7A0E21}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher=Phone Mouse
DefaultDirName={autopf}\\Phone Mouse
DefaultGroupName=Phone Mouse
UninstallDisplayIcon={app}\\PhoneMouse.exe
SetupIconFile={#IconFile}
OutputDir=..\\dist
OutputBaseFilename=PhoneMouse-Setup
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
PrivilegesRequired=admin
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
DisableProgramGroupPage=yes

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\\Russian.isl"

[Tasks]
Name: "desktopicon"; Description: "Создать ярлык на рабочем столе"; GroupDescription: "Дополнительные параметры:"; Flags: unchecked

[Files]
Source: "{#SourceDir}\\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\\Phone Mouse"; Filename: "{app}\\PhoneMouse.exe"; IconFilename: "{app}\\PhoneMouse.exe"
Name: "{autodesktop}\\Phone Mouse"; Filename: "{app}\\PhoneMouse.exe"; Tasks: desktopicon; IconFilename: "{app}\\PhoneMouse.exe"

[Run]
Filename: "{app}\\PhoneMouse.exe"; Description: "Запустить Phone Mouse"; Flags: postinstall nowait skipifsilent
