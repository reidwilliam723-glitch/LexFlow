; LexFlow Installer Script
; Requires Inno Setup (https://jrsoftware.org/isdl.php)

[Setup]
AppName=LexFlow
AppVersion=2.0.0
AppPublisher=LexFlow
AppPublisherURL=https://lexflow.com
AppSupportURL=https://lexflow.com/support
AppUpdatesURL=https://lexflow.com/updates
DefaultDirName={localappdata}\LexFlow
DefaultGroupName=LexFlow
OutputBaseFilename=LexFlow-Setup-2.0.0
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
; SignTool=signtool
; SignTool=sign sign /f "certificate.pfx" /p "password" $f

[Files]
; Settings UI executable (single-process app)
Source: "publish\LexFlow.Settings.exe"; DestDir: "{app}"; Flags: ignoreversion
; License file
Source: "LICENSE"; DestDir: "{app}"; Flags: ignoreversion
; Privacy Policy
Source: "PRIVACY_POLICY.md"; DestDir: "{app}"; Flags: ignoreversion
; Terms of Service
Source: "TERMS_OF_SERVICE.md"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
; Desktop icon for settings
Name: "{userdesktop}\LexFlow"; Filename: "{app}\LexFlow.Settings.exe"; IconFilename: "{app}\LexFlow.Settings.exe"
; Start menu group
Name: "{group}\LexFlow"; Filename: "{app}\LexFlow.Settings.exe"
Name: "{group}\Uninstall LexFlow"; Filename: "{uninstallexe}"

[Run]
; Optionally start the app after installation
Filename: "{app}\LexFlow.Settings.exe"; Description: "Start LexFlow"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Remove app data directory on uninstall (optional - comment out to preserve data)
Type: filesandordirs; Name: "{localappdata}\LexFlow"

[Registry]
; No registry entries - auto-start is managed by the app itself via HKCU

[Code]
// Custom install steps
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    // Create data directory if it doesn't exist
    if not DirExists(ExpandConstant('{localappdata}\LexFlow')) then
      CreateDir(ExpandConstant('{localappdata}\LexFlow'));
      
    // Show welcome message
    if MsgBox('LexFlow has been installed successfully!' + #13#10 + #13#10 +
              'Use the tray icon to configure AI providers and preferences.' + #13#10 +
              'Double-press Ctrl to quickly enable/disable LexFlow.' + #13#10 + #13#10 +
              'Would you like to start LexFlow now?',
              mbInformation, MB_YESNO) = IDYES then
    begin
      ShellExec('', ExpandConstant('{app}\LexFlow.Settings.exe'), '', '', SW_SHOW, ewNoWait, 0);
    end;
  end;
end;

function InitializeSetup(): Boolean;
var
  ResultCode: Integer;
begin
  // Check if .NET 8.0 or later is installed
  if not RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', ResultCode) then
  begin
    MsgBox('.NET 8.0 or later is required to run LexFlow.' + #13#10 + #13#10 +
           'Please install .NET 8.0 Runtime from: https://dotnet.microsoft.com/download',
           mbError, MB_OK);
    Result := False;
    Exit;
  end;

  // Verify .NET 8.0 version (Release >= 528040 for .NET 8.0)
  if ResultCode < 528040 then
  begin
    MsgBox('.NET 8.0 or later is required to run LexFlow.' + #13#10 + #13#10 +
           'Please install .NET 8.0 Runtime from: https://dotnet.microsoft.com/download',
           mbError, MB_OK);
    Result := False;
    Exit;
  end;

  Result := True;
end;
