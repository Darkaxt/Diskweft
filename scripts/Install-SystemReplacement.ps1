[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$project=Split-Path -Parent $PSScriptRoot
$setup='E:\Temp\Diskweft-release-verification-20261104\0.1.1\Diskweft-0.1.1-setup.exe'
$install='C:\Program Files (x86)\Darkaxt\Diskweft'
$prepared=Get-Content "$project/.local/s5/recovery.json" -Raw | ConvertFrom-Json
if(-not $prepared.prepared -or -not (Test-Path "$($prepared.recovery)/files.json")){throw 'Verified recovery set required'}
if((Get-FileHash $setup).Hash -ne 'C149ACB540523B24656226F7F28C5D88492FE0C6915B5121E300839776B8880E'){throw 'Delivered installer hash mismatch'}
if(-not (Test-Path "$install/Diskweft.exe") -or (Get-FileHash "$install/Diskweft.exe").Hash -ne '1E3875D18BD55C93CF554EF4A761FCBED0F067BA7F8FB75362E6EA8CC7A5346B'){throw 'Expected the verified0.1.0 installation before this one-time update'}
if(@(Get-CimInstance Win32_Process -Filter "Name='Diskweft.exe' OR Name='SpaceScan.exe'").Count -ne 0){throw 'An application is running; preserve it before managed installation'}
$process=Start-Process -FilePath $setup -ArgumentList @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART','/NOLAUNCH','/TASKS=shellmenu',('/LOG='+$project+'\.local\s5\install.log')) -WindowStyle Hidden -Wait -PassThru
if($process.ExitCode -ne 0){throw "Installer returned $($process.ExitCode)"}
if((Get-FileHash "$install/Diskweft.exe").Hash -ne '9903E7E15EC2F8A2931582187F69BF2903B7C891327057E6A2E0B60B6F6F1046'){throw 'Installed executable mismatch'}
if((Get-FileHash "$install/Diskweft.exe.config").Hash -ne '28472E5DA425ECCD7D67A8D830D94524E8BF08EF321991731DBDDA2D07A1BC75'){throw 'Installed config mismatch'}
foreach($kind in @('Directory','Drive','Directory\Background')) {
 $argument=if($kind -like '*Background'){'%V'}else{'%1'}
 $actual=(Get-Item "HKCU:\Software\Classes\$kind\shell\Diskweft\command").GetValue('')
 if($actual -ne ('"'+$install+'\Diskweft.exe" "'+$argument+'"')){throw 'Installed shell route mismatch'}
}
@{installed=$true;version='0.1.1';exe="$install\Diskweft.exe";installerSha256=(Get-FileHash $setup).Hash;exeSha256=(Get-FileHash "$install/Diskweft.exe").Hash;configVerified=$true;shellRoutesVerified=$true;oldApplicationStillInstalled=Test-Path 'C:\Program Files (x86)\ProtagonistLabs\SpaceScan\SpaceScan.exe';retirementPending=$true} | ConvertTo-Json | Set-Content "$project/.local/s5/installation.json" -Encoding UTF8
