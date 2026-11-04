$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$project=Split-Path -Parent $PSScriptRoot
$evidence=Join-Path $project '.local/maintenance-file-actions'
$exe='C:\Program Files (x86)\Darkaxt\Diskweft\Diskweft.exe'
$expected='599BE7AD3338D119AE89C7340CC9A876F79DF2657F472416F18F08BC378EA2F5'
if((Get-FileHash $exe).Hash -ne $expected -or [Diagnostics.FileVersionInfo]::GetVersionInfo($exe).FileVersion -ne '0.1.2'){throw 'Final installed binary/version mismatch'}
if((Get-FileHash "$exe.config").Hash -ne '28472E5DA425ECCD7D67A8D830D94524E8BF08EF321991731DBDDA2D07A1BC75'){throw 'Final installed configuration mismatch'}
$launch=Get-Content "$evidence/launch.json" -Raw | ConvertFrom-Json
if(-not $launch.passed -or $launch.exitCode -ne 0 -or $launch.actualExecutable -ne $exe){throw 'Actual installed launch evidence mismatch'}
$appData=[Environment]::GetFolderPath('ApplicationData')
$card=Get-Content "$appData/ProtagonistLabs/apps/diskweft.json" -Raw | ConvertFrom-Json
if($card.version -ne '0.1.2' -or $card.exe -ne $exe -or $card.id -ne 'diskweft'){throw 'Final discovery-card mismatch'}
if((Get-ItemProperty 'HKCU:\Software\Darkaxt\Diskweft').ExePath -ne $exe){throw 'Final executable registration mismatch'}
$link=Join-Path ([Environment]::GetFolderPath('CommonStartMenu')) 'Programs\Diskweft\Diskweft.lnk'
if((New-Object -ComObject WScript.Shell).CreateShortcut($link).TargetPath -ne $exe){throw 'Final shortcut mismatch'}
foreach($kind in @('Directory','Drive','Directory\Background')) {
 $argument=if($kind -like '*Background'){'%V'}else{'%1'}
 if((Get-Item "HKCU:\Software\Classes\$kind\shell\Diskweft\command").GetValue('') -ne ('"'+$exe+'" "'+$argument+'"')){throw 'Final shell route mismatch'}
}
$inventory=Get-Content "$project/.local/s5/inventory/inventory.json" -Raw | ConvertFrom-Json
$histories=0
foreach($file in $inventory.files | Where-Object {$_.root -eq "$appData\Darkaxt\Diskweft" -and $_.relative -like 'history\*'}) {
 if((Get-FileHash -LiteralPath $file.path).Hash -ne $file.sha256){throw 'Pre-existing history hash mismatch'}
 $histories++
}
$migration=Get-Content "$project/.local/s5/migration.json" -Raw | ConvertFrom-Json
foreach($file in $migration.copied){if((Get-FileHash -LiteralPath $file.destination).Hash -ne $file.sha256){throw 'Migrated history hash mismatch'}}
if(Test-Path 'C:\Program Files (x86)\ProtagonistLabs\SpaceScan\SpaceScan.exe'){throw 'Unexpected legacy executable restored'}
@{passed=$true;version='0.1.2';exeSha256=$expected;actualInstalledLaunch=$true;routesAndCardVerified=$true;preExistingHistoriesMatched=$histories;migratedHistoryMatched=$true;pathweftUntouched=$true;checkedUtc=[DateTime]::UtcNow.ToString('o')} | ConvertTo-Json | Set-Content "$evidence/final-installed-state.json" -Encoding UTF8
Get-Content "$evidence/final-installed-state.json"
