[CmdletBinding()]
param([ValidateSet('Install','Installed','Portable','Uninstall')][string]$Phase,[Parameter(Mandatory=$true)][string]$VerifierHash)
$ErrorActionPreference='Stop'
$root='C:\Coordinator\Work'
$packages=Join-Path $root 'packages'
$evidence=Join-Path $root 'package-results'
[IO.Directory]::CreateDirectory($evidence) | Out-Null
$env:TEMP=$evidence;$env:TMP=$evidence
$exe='C:\Program Files (x86)\Darkaxt\Diskweft\Diskweft.exe'
$expected='599BE7AD3338D119AE89C7340CC9A876F79DF2657F472416F18F08BC378EA2F5'
if((Get-FileHash "$root/Verify-Package.ps1").Hash -ne $VerifierHash){throw 'Reviewed package-verifier dependency changed'}
if($Phase -eq 'Install') {
 $setup=Join-Path $packages 'Diskweft-0.1.2-setup.exe'
 if((Get-FileHash $setup).Hash -ne '7567776E9C33B634ED26A0C3E3E7AE50AFDF6CC1C924F5DDE9FB92896D28E336'){throw 'Setup mismatch'}
 if(Test-Path $exe){throw 'Do not overwrite an existing guest installation'}
 $process=Start-Process -FilePath $setup -ArgumentList @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART','/NOLAUNCH','/TASKS=shellmenu',('/LOG='+$evidence+'\install.log')) -WindowStyle Hidden -Wait -PassThru
 if($process.ExitCode -ne 0 -or (Get-FileHash $exe).Hash -ne $expected){throw 'Guest installation failed'}
 foreach($kind in @('Directory','Drive','Directory\Background')) {
  $argument=if($kind -like '*Background'){'%V'}else{'%1'}
  if((Get-Item "HKCU:\Software\Classes\$kind\shell\Diskweft\command").GetValue('') -ne ('"'+$exe+'" "'+$argument+'"')){throw 'Guest installed shell route mismatch'}
 }
} elseif($Phase -eq 'Installed') {
 if((Get-FileHash $exe).Hash -ne $expected){throw 'Installed hash mismatch'}
 & "$root/Verify-Package.ps1" -PackageRoot (Split-Path $exe) -OutputRoot "$evidence/installed" -ExpectedVersion '0.1.2'
} elseif($Phase -eq 'Portable') {
 $zip=Join-Path $packages 'Diskweft-0.1.2-portable.zip'
 if((Get-FileHash $zip).Hash -ne '03C8E4FEB08978F3C4ED28D6A47165C1FB14280C86611B6FE9A18EC6B6C374CC'){throw 'Portable ZIP mismatch'}
 $portable=Join-Path $packages 'portable'
 if(Test-Path $portable){throw 'Preserve an existing extraction'}
 Expand-Archive -LiteralPath $zip -DestinationPath $portable
 if((Get-FileHash "$portable/Diskweft.exe").Hash -ne $expected){throw 'Extracted hash mismatch'}
 & "$root/Verify-Package.ps1" -PackageRoot $portable -OutputRoot "$evidence/portable" -ExpectedVersion '0.1.2'
} else {
 if((Get-FileHash $exe).Hash -ne $expected){throw 'Wrong guest uninstall target'}
 $process=Start-Process -FilePath (Join-Path (Split-Path $exe) 'unins000.exe') -ArgumentList @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART') -WindowStyle Hidden -Wait -PassThru
 if($process.ExitCode -ne 0 -or (Test-Path $exe)){throw 'Guest native uninstall failed'}
}
@{passed=$true;phase=$Phase;version='0.1.2';exeSha256=$expected} | ConvertTo-Json | Set-Content "$evidence/$Phase.json" -Encoding UTF8
