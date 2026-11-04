[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$PackageRoot,[Parameter(Mandatory=$true)][string]$OutputRoot)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
[IO.Directory]::CreateDirectory($OutputRoot) | Out-Null
$exe=Join-Path $PackageRoot 'Diskweft.exe'
if(-not (Test-Path "$exe.config")){throw 'Runtime DPI configuration missing'}
if(-not (Test-Path "$PackageRoot/LICENSE")){throw 'MIT notice missing'}
$version=[Diagnostics.FileVersionInfo]::GetVersionInfo($exe)
if($version.ProductName -ne 'Diskweft' -or $version.FileVersion -ne '0.1.0'){throw 'Packaged identity/version mismatch'}
$env:DISKWEFT_TEST_ROOT=Join-Path $OutputRoot 'profile'
$env:TEMP=$OutputRoot;$env:TMP=$OutputRoot
$fixture=Join-Path $OutputRoot 'fixture'
[IO.Directory]::CreateDirectory("$fixture/child") | Out-Null
[IO.File]::WriteAllBytes("$fixture/child/file.bin",(New-Object byte[] 1234))
$assembly=[Reflection.Assembly]::LoadFrom($exe)
$flags=[Reflection.BindingFlags]'Public,NonPublic,Static,Instance'
if($assembly.GetType('MainUI').GetMethod('DiagnosticScan',$flags)){throw 'Packaged test hooks present'}
$cli=Start-Process -FilePath $exe -ArgumentList @(('"'+$fixture+'"'),'--csv',('"'+$OutputRoot+'/report.csv"'),'--snapshot',('"'+$OutputRoot+'/snapshot.scan"')) -WindowStyle Hidden -Wait -PassThru
if($cli.ExitCode -ne 0){throw 'Packaged CLI failed'}
Add-Type -AssemblyName PresentationFramework
$application=New-Object Windows.Application
$ui=[Activator]::CreateInstance($assembly.GetType('MainUI'),$true)
$window=$ui.Window
$uiType=$ui.GetType()
$state=@{error=$null;complete=$false;files=0L;bytes=0L;ownedExit=$false}
$timer=New-Object Windows.Threading.DispatcherTimer
$timer.Interval=[TimeSpan]::FromMilliseconds(100)
$timer.Add_Tick({
    try {
        if($uiType.GetField('busy',$flags).GetValue($ui)){return}
        if($uiType.GetField('ownedWorkers',$flags).GetValue($ui) -ne 0){return}
        $node=$uiType.GetField('root',$flags).GetValue($ui)
        if($null -eq $node){return}
        $state.files=$node.Files;$state.bytes=$node.Size
        if($node.Outcome -ne 'complete' -or $node.Files -ne 1 -or $node.Size -ne 1234){throw 'Installed native scan totals/provenance mismatch'}
        $state.complete=$true;$state.ownedExit=$true
        $timer.Stop();$window.Close()
    }catch{$state.error=$_.Exception.ToString();$timer.Stop();$window.Close()}
})
$window.Add_Loaded({
    try {
        $uiType.GetField('pathBox',$flags).GetValue($ui).Text=$fixture
        $uiType.GetMethod('StartOrStop',$flags).Invoke($ui,@()) | Out-Null
        $timer.Start()
    }catch{$state.error=$_.Exception.ToString();$window.Close()}
})
$application.Run($window) | Out-Null
if($state.error){throw $state.error}
if(-not $state.complete){throw 'Native packaged workflow did not complete'}
[ordered]@{passed=$true;version=$version.FileVersion;sha256=(Get-FileHash $exe).Hash;signature=(Get-AuthenticodeSignature $exe).Status.ToString();cli=$true;native=$true;files=$state.files;bytes=$state.bytes;ownedWorkersExited=$state.ownedExit;configSha256=(Get-FileHash "$exe.config").Hash} | ConvertTo-Json | Set-Content "$OutputRoot/result.json" -Encoding UTF8
