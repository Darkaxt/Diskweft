$ErrorActionPreference='Stop'
$project=Split-Path -Parent $PSScriptRoot
$framework=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319'
$scratch='E:\Temp\Diskweft-release-verification-20261104'
$probe=Join-Path $scratch 'InstalledLaunchProbe.exe'
$exe='C:\Program Files (x86)\Darkaxt\Diskweft\Diskweft.exe'
$link='C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Diskweft\Diskweft.lnk'
$shortcut=(New-Object -ComObject WScript.Shell).CreateShortcut($link)
if($shortcut.TargetPath -ne $exe){throw 'Installed Start menu shortcut target mismatch'}
if((Get-FileHash $exe).Hash -ne '9903E7E15EC2F8A2931582187F69BF2903B7C891327057E6A2E0B60B6F6F1046'){throw 'Installed executable hash mismatch'}
if($env:DISKWEFT_TEST_ROOT){throw 'Actual launch must not inherit an isolated profile'}
$source=Join-Path $project 'tests\InstalledLaunchProbe.cs'
$compileArguments=@('/nologo','/target:exe','/optimize',('/lib:'+(Join-Path $framework 'WPF')),'/r:WindowsBase.dll','/r:UIAutomationClient.dll','/r:UIAutomationTypes.dll','/r:System.Web.Extensions.dll',('/out:'+$probe),$source)
$compileArguments | ConvertTo-Json | Set-Content "$project/.local/s5/launch-compile.json" -Encoding UTF8
& (Join-Path $framework 'csc.exe') @compileArguments
if($LASTEXITCODE -ne 0){throw 'Installed launch probe compilation failed'}
& $probe $exe "$scratch\installed-fixture" "$project/.local/s5/launch.json"
if($LASTEXITCODE -ne 0){throw 'Actual installed bare-path launch verification failed'}
