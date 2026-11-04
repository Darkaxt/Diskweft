[CmdletBinding()]
param([ValidateSet('Classic','Ntfs')][string]$Mode='Classic')
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$project=Split-Path -Parent $PSScriptRoot
$exe='C:\Program Files (x86)\Darkaxt\Diskweft\Diskweft.exe'
if((Get-FileHash $exe).Hash -ne '9903E7E15EC2F8A2931582187F69BF2903B7C891327057E6A2E0B60B6F6F1046'){throw 'Actual installed package mismatch'}
if($env:DISKWEFT_TEST_ROOT){throw 'Actual installed profile verification must not inherit a test profile'}
Add-Type -AssemblyName PresentationFramework
$assembly=[Reflection.Assembly]::LoadFrom($exe)
$flags=[Reflection.BindingFlags]'Public,NonPublic,Static,Instance'
$uiType=$assembly.GetType('MainUI')
$profile=Join-Path ([Environment]::GetFolderPath('ApplicationData')) 'Darkaxt\Diskweft'
if($assembly.GetType('Ecosystem').GetField('Data',$flags).GetValue($null) -ne $profile){throw 'Actual installed profile mismatch'}
$fixture='E:\Temp\Diskweft-release-verification-20261104\installed-fixture'
[IO.Directory]::CreateDirectory($fixture) | Out-Null
[IO.File]::WriteAllBytes("$fixture/probe.bin",(New-Object byte[] 1234))
$target=if($Mode -eq 'Ntfs'){'E:\'}else{$fixture}
$historyType=$assembly.GetType('History')
$historyBefore=@($historyType.GetMethod('List',$flags).Invoke($null,@($target)))
$application=New-Object Windows.Application
$uiType.GetField('startPath',$flags).SetValue($null,$target)
$ui=[Activator]::CreateInstance($uiType,$true)
$window=$ui.Window
$state=@{passed=$false;error=$null;files=0L;bytes=0L;outcome='';engine='';workers=-1}
$timer=New-Object Windows.Threading.DispatcherTimer
$timer.Interval=[TimeSpan]::FromMilliseconds(100)
$timer.Add_Tick({
 try {
  # Only windows owned by this verification process; close the first-run notes.
  foreach($other in @($application.Windows)){if($other -ne $window){$other.Close()}}
  if($uiType.GetField('busy',$flags).GetValue($ui)){return}
  if($uiType.GetField('ownedWorkers',$flags).GetValue($ui) -ne 0){return}
  $node=$uiType.GetField('root',$flags).GetValue($ui)
  if($null -eq $node){return}
  $session=$uiType.GetField('scanSession',$flags).GetValue($ui)
  $engine=if($null -ne $session){$session.Mode}else{$uiType.GetField('status',$flags).GetValue($ui).Text}
  $state.files=$node.Files;$state.bytes=$node.Size;$state.outcome=$node.Outcome;$state.engine=$engine;$state.workers=0
  if($Mode -eq 'Classic') {
   if($node.Files -ne 1 -or $node.Size -ne 1234 -or $node.Outcome -ne 'complete'){throw 'Installed classic totals/provenance mismatch'}
  } else {
   if($node.Files -lt 100000 -or -not $engine.Contains('MFT (progressive records)') -or $node.Outcome -notin @('complete','incomplete')){throw 'Installed raw NTFS verification failed or fell back'}
  }
  $state.passed=$true;$timer.Stop();$window.Close()
 }catch{$state.error=$_.Exception.ToString();$timer.Stop();$window.Close()}
})
$window.Add_Loaded({$timer.Start()})
$application.Run($window) | Out-Null
if($state.error){throw $state.error}
if(-not $state.passed){throw 'Actual installed native workflow ended without success'}
$state.actualProfile=$true;$state.installedSha256=(Get-FileHash $exe).Hash
$state.newHistory=@($historyType.GetMethod('List',$flags).Invoke($null,@($target)) | Where-Object {$_ -notin $historyBefore})
$state | ConvertTo-Json -Depth 5 | Set-Content "$project/.local/s5/installed-$($Mode.ToLowerInvariant()).json" -Encoding UTF8
