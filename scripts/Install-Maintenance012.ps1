[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$project=Split-Path -Parent $PSScriptRoot
$evidence=Join-Path $project '.local/maintenance-file-actions'
$setup='E:\Temp\Diskweft-file-actions-resume-20261104\download\Diskweft-0.1.2-setup.exe'
$exe='C:\Program Files (x86)\Darkaxt\Diskweft\Diskweft.exe'
if((Get-FileHash $setup).Hash -ne '7567776E9C33B634ED26A0C3E3E7AE50AFDF6CC1C924F5DDE9FB92896D28E336'){throw 'Independently delivered setup mismatch'}
if((Get-FileHash $exe).Hash -ne '9903E7E15EC2F8A2931582187F69BF2903B7C891327057E6A2E0B60B6F6F1046'){throw 'Expected the existing verified 0.1.1 installation'}
if(@(Get-CimInstance Win32_Process -Filter "Name='Diskweft.exe'").Count){throw 'Preserve the running application before updating'}
$data=Join-Path ([Environment]::GetFolderPath('ApplicationData')) 'Darkaxt\Diskweft'
$before=@(Get-ChildItem -LiteralPath $data -File -Recurse | ForEach-Object {@{path=$_.FullName;sha256=(Get-FileHash -LiteralPath $_.FullName).Hash}})
$before | ConvertTo-Json -Depth 3 | Set-Content "$evidence/pre-update-data.json" -Encoding UTF8
$process=Start-Process -FilePath $setup -ArgumentList @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART','/NOLAUNCH','/TASKS=shellmenu',('/LOG='+$evidence+'\install-0.1.2.log')) -WindowStyle Hidden -Wait -PassThru
if($process.ExitCode -ne 0){throw "Native installer returned $($process.ExitCode)"}
if((Get-FileHash $exe).Hash -ne '599BE7AD3338D119AE89C7340CC9A876F79DF2657F472416F18F08BC378EA2F5'){throw 'Installed 0.1.2 executable mismatch'}
if((Get-FileHash "$exe.config").Hash -ne '28472E5DA425ECCD7D67A8D830D94524E8BF08EF321991731DBDDA2D07A1BC75'){throw 'Installed DPI configuration mismatch'}
foreach($file in $before){if((Get-FileHash -LiteralPath $file.path).Hash -ne $file.sha256){throw 'Installer changed existing user data'}}
foreach($kind in @('Directory','Drive','Directory\Background')) {
 $argument=if($kind -like '*Background'){'%V'}else{'%1'}
 if((Get-Item "HKCU:\Software\Classes\$kind\shell\Diskweft\command").GetValue('') -ne ('"'+$exe+'" "'+$argument+'"')){throw 'Installed shell argument mismatch'}
}
@{passed=$true;version='0.1.2';nativeExit=$process.ExitCode;exeSha256=(Get-FileHash $exe).Hash;existingDataFilesPreserved=$before.Count;rollbackPackage='D:\Projects\Diskweft\dist\0.1.1\Diskweft-0.1.1-setup.exe';pathweftUntouched=$true} | ConvertTo-Json | Set-Content "$evidence/installation-0.1.2.json" -Encoding UTF8
