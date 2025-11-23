; School Pay List System - Professional Installer
; Inno Setup Script - Creates professional MSI-style setup.exe
; 
; HOW TO USE:
; 1. Download Inno Setup from: https://jrsoftware.org/isdl.php
; 2. Open this file in Inno Setup
; 3. Click "Build" or Ctrl+F9
; 4. Setup file will be generated in output directory
;
; RESULT: Professional SchoolPayListSystem_Setup.exe (single file installer)

[Setup]
; Application Information
AppId={{SCHOOL-PAY-LIST-SYSTEM-1.0.0}}
AppName=School Pay List System
AppVersion=1.0.0
AppPublisher=Shree Computer Services
AppPublisherURL=https://www.shreecs.com
AppSupportURL=https://www.shreecs.com
AppUpdatesURL=https://www.shreecs.com
AppContact=support@shreecs.com

; Installation Settings
DefaultDirName={autopf}\School Pay List System
DefaultGroupName=School Pay List System
AllowNoIcons=no
AllowUNCPath=no
AlwaysRestart=no
ArchitecturesInstallIn64BitMode=x64
DisableDirPage=no
DirExistsWarning=no

; Output Settings
OutputDir=c:\Users\agraw\OneDrive\Desktop
OutputBaseFilename=SchoolPayListSystem_Setup
Compression=lzma2
SolidCompression=yes
InternalCompressLevel=max

; Visual Style
WizardStyle=modern
WizardResizable=yes
WizardImageFile=
WizardSmallImageFile=

; Icon Configuration
SetupIconFile=c:\Users\agraw\OneDrive\Desktop\new_app_vs\SchoolPayListSystem\SchoolPayListSystem.App\Assets\software_logo.ico
UninstallDisplayIcon={app}\SchoolPayListSystem.App.exe

; Security and Privileges
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=commandline

; Version Information
VersionInfoVersion=1.0.0.0
VersionInfoCompany=Shree Computer Services
VersionInfoProductName=School Pay List System
VersionInfoProductVersion=1.0.0
VersionInfoCopyright=Copyright (C) 2025 Shree Computer Services. All Rights Reserved.

; Windows Version Requirements
MinVersion=6.1.7601

; Miscellaneous
ShowLanguageDialog=auto
UsePreviousTasks=yes
RestartIfNeededByRun=yes
ChangesAssociations=no
ChangesEnvironment=no
DiskSpanning=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[CustomMessages]
WelcomeLabel1=Welcome to School Pay List System Setup Wizard
WelcomeLabel2=This will install School Pay List System v1.0.0 on your computer.%n%nIt is recommended that you close all running programs before continuing.%n%nClick Next to continue.

[Tasks]
Name: "desktopicon"; Description: "Create a &Desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked
Name: "quicklaunchicon"; Description: "Create a &Quick Launch shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked
Name: "associatefiles"; Description: "&Associate .paylist files with this application"; GroupDescription: "File associations:"; Flags: unchecked

[Files]
; Source: Path to release files
; Copy all files from the Release folder with directory structure
Source: "c:\Users\agraw\OneDrive\Desktop\SchoolPayListSystem_Release\SchoolPayListSystem.App.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "c:\Users\agraw\OneDrive\Desktop\SchoolPayListSystem_Release\*.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "c:\Users\agraw\OneDrive\Desktop\SchoolPayListSystem_Release\*.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "c:\Users\agraw\OneDrive\Desktop\SchoolPayListSystem_Release\Assets\*"; DestDir: "{app}\Assets"; Flags: ignoreversion recursesubdirs

[Icons]
; Start Menu
Name: "{group}\School Pay List System"; Filename: "{app}\SchoolPayListSystem.App.exe"; IconFilename: "{app}\Assets\software_logo.ico"; Comment: "Launch School Pay List System"
Name: "{group}\Uninstall School Pay List System"; Filename: "{uninstallexe}"; IconFilename: "{app}\Assets\software_logo.ico"

; Desktop (if task selected)
Name: "{commondesktop}\School Pay List System"; Filename: "{app}\SchoolPayListSystem.App.exe"; IconFilename: "{app}\Assets\software_logo.ico"; Tasks: desktopicon; Comment: "Launch School Pay List System"

; Quick Launch (if task selected)
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\School Pay List System"; Filename: "{app}\SchoolPayListSystem.App.exe"; IconFilename: "{app}\Assets\software_logo.ico"; Tasks: quicklaunchicon

[Run]
; Run application after installation
Filename: "{app}\SchoolPayListSystem.App.exe"; Description: "&Launch School Pay List System"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Delete these directories/files when uninstalling
Type: dirifempty; Name: "{app}"
Type: dirifempty; Name: "{localappdata}\School Pay List System"

[Registry]
; Optional: Add file associations
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\.paylist\UserChoice"; ValueType: string; ValueName: "School Pay List"; Flags: createvalueifdoesntexist uninsdeletevalue; Tasks: associatefiles

[Code]
{ Custom code section for advanced installer features }

procedure CurPageChanged(CurPageID: Integer);
begin
  case CurPageID of
    wpWelcome:
      begin
        WizardForm.NextButton.Caption := '&Next >';
        WizardForm.CancelButton.Caption := 'Cancel';
      end;
    wpSelectDir:
      begin
        WizardForm.NextButton.Caption := '&Next >';
      end;
    wpSelectTasks:
      begin
        WizardForm.NextButton.Caption := '&Next >';
      end;
    wpReady:
      begin
        WizardForm.NextButton.Caption := '&Install';
      end;
    wpInstalling:
      begin
        WizardForm.NextButton.Caption := 'Installing...';
        WizardForm.NextButton.Enabled := False;
      end;
    wpFinished:
      begin
        WizardForm.NextButton.Caption := '&Finish';
      end;
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    MsgBox('School Pay List System has been successfully installed!' + #13#10#13#10 +
            'Key Information:' + #13#10 +
            '• Database will be created automatically on first run' + #13#10 +
            '• Default login: admin / password: admin' + #13#10 +
            '• Shortcuts have been created on your Desktop and Start Menu' + #13#10 +
            '• For help, see README.txt in the installation folder' + #13#10#13#10 +
            'Click OK to finish setup.', mbInformation, MB_OK);
  end;
end;

{ Pre-install checks }
function InitializeSetup(): Boolean;
begin
  Result := True;
  { Add any pre-installation checks here }
end;

{ Allow abort during installation }
procedure CancelButtonClick(CurPageID: Integer; var Cancel, Confirm: Boolean);
begin
  if CurPageID = wpInstalling then
  begin
    Confirm := True;
  end;
end;


