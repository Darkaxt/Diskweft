[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$project=Split-Path -Parent $PSScriptRoot
$evidence=Join-Path $project '.local/s5'
$inventory=Get-Content "$evidence/inventory/inventory.json" -Raw | ConvertFrom-Json
$migration=Get-Content "$evidence/migration.json" -Raw | ConvertFrom-Json
$delivery=Get-Content "$project/.local/s4/delivery-verification-0.1.1.json" -Raw | ConvertFrom-Json
$install='C:\Program Files (x86)\Darkaxt\Diskweft'
$exe=Join-Path $install 'Diskweft.exe'
$expected='9903E7E15EC2F8A2931582187F69BF2903B7C891327057E6A2E0B60B6F6F1046'
if(-not $delivery.passed -or -not $migration.passed){throw 'Verified delivery and migration receipts are required'}
if((Get-FileHash -LiteralPath $exe).Hash -ne $expected){throw 'Installed executable no longer matches the delivered release'}
if([Diagnostics.FileVersionInfo]::GetVersionInfo($exe).FileVersion -ne '0.1.1'){throw 'Installed version mismatch'}
if((Get-FileHash -LiteralPath "$exe.config").Hash -ne '28472E5DA425ECCD7D67A8D830D94524E8BF08EF321991731DBDDA2D07A1BC75'){throw 'Installed DPI configuration mismatch'}
if((Get-ItemProperty 'HKCU:\Software\Darkaxt\Diskweft').ExePath -ne $exe){throw 'Installed executable registration mismatch'}
foreach($kind in @('Directory','Drive','Directory\Background')) {
 $argument=if($kind -like '*Background'){'%V'}else{'%1'}
 if((Get-Item "HKCU:\Software\Classes\$kind\shell\Diskweft\command").GetValue('') -ne ('"'+$exe+'" "'+$argument+'"')){throw 'Installed shell argument mismatch'}
}
$link=Join-Path ([Environment]::GetFolderPath('CommonStartMenu')) 'Programs\Diskweft\Diskweft.lnk'
if((New-Object -ComObject WScript.Shell).CreateShortcut($link).TargetPath -ne $exe){throw 'Installed Start menu route mismatch'}
$appData=[Environment]::GetFolderPath('ApplicationData')
$card=Get-Content (Join-Path $appData 'ProtagonistLabs\apps\diskweft.json') -Raw | ConvertFrom-Json
if($card.id -ne 'diskweft' -or $card.version -ne '0.1.1' -or $card.exe -ne $exe){throw 'Installed ecosystem identity mismatch'}
& "$PSScriptRoot/Retire-SpaceScan.ps1" -VerifyExistingUninstall
$retirement=Get-Content "$evidence/retirement.json" -Raw | ConvertFrom-Json
if(-not $retirement.passed -or $retirement.obsoleteCardCleanupPending){throw 'SpaceScan retirement is incomplete'}
$historyCount=0
foreach($file in $inventory.files | Where-Object {$_.root -eq "$appData\Darkaxt\Diskweft" -and $_.relative -like 'history\*'}) {
 if((Get-FileHash -LiteralPath $file.path).Hash -ne $file.sha256){throw 'A pre-existing Diskweft history file changed'}
 $historyCount++
}
$recovery=$migration.rollback.recovery
$recoveryFiles=Get-Content "$recovery/files.json" -Raw | ConvertFrom-Json
foreach($file in $recoveryFiles){if((Get-FileHash -LiteralPath $file.recovery).Hash -ne $file.sha256){throw 'Recovery file hash mismatch'}}
if((Get-FileHash -LiteralPath "$recovery/spacescan-card.json").Hash -ne $inventory.cardSha256){throw 'Original card recovery hash mismatch'}
foreach($item in $inventory.shortcuts){if((Get-FileHash -LiteralPath (Join-Path "$recovery/shortcuts" ([IO.Path]::GetFileName($item.path)))).Hash -ne $item.sha256){throw 'Shortcut recovery hash mismatch'}}
foreach($item in $inventory.registry){if((Get-FileHash -LiteralPath "$recovery/registry-inventory/$($item.export)").Hash -ne (Get-FileHash -LiteralPath "$evidence/inventory/$($item.export)").Hash){throw 'Registry recovery export mismatch'}}
foreach($name in @('installed-classic','installed-ntfs','launch')) {
 $result=Get-Content "$evidence/$name.json" -Raw | ConvertFrom-Json
 if(-not $result.passed){throw "Required installed workflow failed: $name"}
 if($name -ne 'launch' -and ($result.installedSha256 -ne $expected -or $result.workers -ne 0)){throw 'Native workflow receipt does not apply to this installed binary'}
}
$roots=@('D:\Temp\Diskweft-s4-optimization','E:\Temp\Diskweft-s4-optimization-20261104','D:\Temp\Diskweft-release-notes-fix','E:\Temp\Diskweft-release-notes-fix-20261104','E:\Temp\Diskweft-release-verification-20261104',"$env:LOCALAPPDATA\Temp\is-5TVAQFSOB3-uninstall.tmp")
foreach($root in $roots){if(Test-Path -LiteralPath $root){throw "Disposable task output remains: $root"}}
if(@(Get-ChildItem -LiteralPath "$appData\Darkaxt\Diskweft\history" -Filter 'e--temp-diskweft-release-verification-20261104-installed-fixture__*.scan').Count){throw 'Disposable fixture history remains'}
@{passed=$true;checkedUtc=[DateTime]::UtcNow.ToString('o');version='0.1.1';exeSha256=$expected;deliveredSource=$delivery.sourceCommit;installedRoutesVerified=$true;spaceScanUninstalled=$true;originalAndMigratedDataPreserved=$true;preExistingDiskweftHistoriesVerified=$historyCount;recoveryFilesVerified=$recoveryFiles.Count;registryAndShortcutRecoveryVerified=$true;actualInstalledWorkflowReceiptsApplicable=$true;fullReinstallExercised=$false;pathweftIntegrationExcluded=$true;scratchAbsent=$roots} | ConvertTo-Json -Depth 4 | Set-Content "$evidence/final-state.json" -Encoding UTF8
Get-Content "$evidence/final-state.json"
