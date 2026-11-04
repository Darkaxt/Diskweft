[CmdletBinding()]
param([switch]$VerifyExistingUninstall)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$project=Split-Path -Parent $PSScriptRoot
$inventory=Get-Content "$project/.local/s5/inventory/inventory.json" -Raw | ConvertFrom-Json
$migration=Get-Content "$project/.local/s5/migration.json" -Raw | ConvertFrom-Json
$classic=Get-Content "$project/.local/s5/installed-classic.json" -Raw | ConvertFrom-Json
if(-not $migration.passed -or -not $classic.passed -or -not $classic.actualProfile){throw 'Installed profile/migration acceptance must pass first'}
if(@(Get-CimInstance Win32_Process -Filter "Name='Diskweft.exe' OR Name='SpaceScan.exe'").Count -ne 0){throw 'Preserve a running application; do not retire it while active'}
$recoveryFiles=Get-Content "$($migration.rollback.recovery)/files.json" -Raw | ConvertFrom-Json
foreach($file in $recoveryFiles) {
 if((Get-FileHash -LiteralPath $file.recovery).Hash -ne $file.sha256){throw 'Recovery material changed before retirement'}
}
if(-not $VerifyExistingUninstall){& "$PSScriptRoot/Verify-InstalledReplacement.ps1" -Mode Ntfs}
$native=Get-Content "$project/.local/s5/installed-ntfs.json" -Raw | ConvertFrom-Json
if(-not $native.passed -or -not $native.actualProfile -or $native.workers -ne 0){throw 'Installed actual-profile raw NTFS acceptance failed'}
$old='C:\Program Files (x86)\ProtagonistLabs\SpaceScan'
$dataRoot=Join-Path ([Environment]::GetFolderPath('ApplicationData')) 'ProtagonistLabs\SpaceScan'
$uninstaller=Join-Path $old 'unins000.exe'
$expected=@($inventory.files | Where-Object {$_.path -eq $uninstaller})
foreach($file in $inventory.files | Where-Object {$_.root -eq $dataRoot}) {
 if((Get-FileHash -LiteralPath $file.path).Hash -ne $file.sha256){throw 'Original user data changed before retirement'}
}
if($VerifyExistingUninstall) {
 $log=Get-Content "$project/.local/s5/spacescan-uninstall.log" -Raw
 if(-not $log.Contains('Uninstallation process succeeded.') -or -not $log.Contains('Removed all? Yes')){throw 'Original successful native uninstall receipt is required for read-only reconciliation'}
 $nativeUninstallExit=0
} else {
 if($expected.Count -ne 1 -or (Get-FileHash -LiteralPath $uninstaller).Hash -ne $expected[0].sha256){throw 'Original uninstaller identity mismatch'}
 $process=Start-Process -FilePath $uninstaller -ArgumentList @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART',('/LOG='+$project+'\.local\s5\spacescan-uninstall.log')) -WindowStyle Hidden -Wait -PassThru
 if($process.ExitCode -ne 0){throw "Original uninstaller returned $($process.ExitCode)"}
 $nativeUninstallExit=$process.ExitCode
}
if(Test-Path "$old/SpaceScan.exe"){throw 'Original executable remains after native uninstall'}
$uninstallKey='Registry::HKEY_LOCAL_MACHINE\Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\{5B7F3C21-9D4A-4E86-B0C7-2A9E6F1D8B53}_is1'
if(Test-Path $uninstallKey){throw 'Original uninstall registration remains'}
foreach($kind in @('Directory','Drive','Directory\Background')) {
 if(Test-Path "HKCU:\Software\Classes\$kind\shell\SpaceScan"){throw 'Original shell registration remains'}
}
if(Test-Path 'HKCU:\Software\SpaceScan'){throw 'Original executable registration remains'}
foreach($link in $inventory.shortcuts){if(Test-Path -LiteralPath $link.path){throw 'Original Start menu shortcut remains'}}
foreach($file in $inventory.files | Where-Object {$_.root -eq $dataRoot}) {
 if((Get-FileHash -LiteralPath $file.path).Hash -ne $file.sha256){throw 'Native uninstall altered original user data'}
}
foreach($file in $migration.copied) {
 if((Get-FileHash -LiteralPath ([string]$file.destination)).Hash -ne $file.sha256){throw 'Migrated data changed during uninstall'}
}
@{passed=$true;spaceScanUninstalled=$true;nativeUninstallerExit=$nativeUninstallExit;readOnlyReconciliation=[bool]$VerifyExistingUninstall;uninstallReplayed=$false;oldExecutableAbsent=$true;oldRegistryAbsent=$true;oldShortcutsAbsent=$true;originalDataPreserved=$true;migratedDataPreserved=$true;installedNativeNtfs=$true;pathweftUntouched=$true;obsoleteCardCleanupPending=Test-Path $inventory.cardPath;recovery=$migration.rollback.recovery} | ConvertTo-Json | Set-Content "$project/.local/s5/retirement.json" -Encoding UTF8
