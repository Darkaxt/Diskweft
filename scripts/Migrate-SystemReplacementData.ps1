[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$project=Split-Path -Parent $PSScriptRoot
$inventory=Get-Content "$project/.local/s5/inventory/inventory.json" -Raw | ConvertFrom-Json
$recovery=(Get-Content "$project/.local/s5/recovery.json" -Raw | ConvertFrom-Json).recovery
$appData=[Environment]::GetFolderPath('ApplicationData')
$source="$appData\ProtagonistLabs\SpaceScan"
$destination="$appData\Darkaxt\Diskweft"
$copied=@()
foreach($file in $inventory.files | Where-Object {$_.root -eq $source -and ($_.relative -like 'history\*.scan' -or $_.relative -eq 'exclude.txt')}) {
 if((Get-FileHash $file.path).Hash -ne $file.sha256){throw 'Original user data changed after the recovery snapshot'}
 $target=Join-Path $destination $file.relative
 if(Test-Path $target) {
  if((Get-FileHash $target).Hash -ne $file.sha256){throw 'Conflicting destination data must be preserved and resolved'}
 } else {
  [IO.Directory]::CreateDirectory((Split-Path -Parent $target)) | Out-Null
  Copy-Item -LiteralPath $file.path -Destination $target
 }
 if((Get-FileHash $target).Hash -ne $file.sha256){throw 'Migrated data mismatch'}
 $copied+=@{source=$file.path;destination=$target;sha256=$file.sha256}
}
$flags=[Reflection.BindingFlags]'Public,NonPublic,Static,Instance'
$fork=[Reflection.Assembly]::LoadFrom('C:\Program Files (x86)\Darkaxt\Diskweft\Diskweft.exe')
$original=[Reflection.Assembly]::LoadFrom("$recovery/original-install/SpaceScan.exe")
$history=@()
foreach($file in $copied | Where-Object {$_.destination.EndsWith('.scan')}) {
 $argsFork=New-Object object[] 3
 $argsFork[0]=[string]$file.destination;$argsFork[1]=[DateTime]::MinValue;$argsFork[2]=''
 $loaded=$fork.GetType('Snapshot').GetMethod('Load',$flags).Invoke($null,$argsFork)
 if($loaded.Outcome -ne 'legacy-unknown'){throw 'Legacy provenance was promoted incorrectly'}
 $oldFile=Join-Path "$recovery/original-data" $file.destination.Substring($destination.Length).TrimStart('\')
 $argsOld=New-Object object[] 3
 $argsOld[0]=[string]$oldFile;$argsOld[1]=[DateTime]::MinValue;$argsOld[2]=''
 $restored=$original.GetType('Snapshot').GetMethod('Load',$flags).Invoke($null,$argsOld)
 if($loaded.Size -ne $restored.Size -or $loaded.Files -ne $restored.Files -or $argsFork[2] -ne $argsOld[2]){throw 'Recovery snapshot round-trip totals mismatch'}
 $listed=$fork.GetType('History').GetMethod('List',$flags).Invoke($null,@([string]$argsFork[2]))
 if(-not $listed.Contains($file.destination)){throw 'Migrated snapshot is absent from installed history lookup'}
 $history+=@{path=$file.destination;files=$loaded.Files;bytes=$loaded.Size;outcome=$loaded.Outcome;root=$argsFork[2];recoverableWithOriginalReader=$true;listedByInstalledHistory=$true}
}
$recoveryFiles=Get-Content "$recovery/files.json" -Raw | ConvertFrom-Json
foreach($file in $recoveryFiles) {
 if((Get-FileHash $file.recovery).Hash -ne $file.sha256){throw 'Recovery material changed'}
}
$fixture='E:\Temp\Diskweft-release-verification-20261104\rollback-fixture'
[IO.Directory]::CreateDirectory($fixture) | Out-Null
[IO.File]::WriteAllBytes("$fixture/probe.bin",(New-Object byte[] 1234))
$original.GetType('Stats').GetMethod('Reset',$flags).Invoke($null,@()) | Out-Null
$original.GetType('Scanner').GetField('Cancel',$flags).SetValue($null,$false)
$node=[Activator]::CreateInstance($original.GetType('Node'),$true);$node.Name=$fixture
$original.GetType('Scanner').GetMethod('Scan',$flags).Invoke($null,@($node,$fixture)) | Out-Null
if($node.Files -ne 1 -or $node.Size -ne 1234){throw 'Recovered original scanner failed controlled execution'}
@{passed=$true;copied=$copied;history=$history;originalDataPreserved=$true;existingDiskweftDataPreserved=$true;versionAcknowledgementNotMigrated=$true;rollback=@{hashes=$true;originalSnapshotReader=$true;originalScannerExecuted=$true;registryAndShortcutsExported=$true;fullReinstallExercised=$false;recovery=$recovery};pathweftUntouched=$true} | ConvertTo-Json -Depth 7 | Set-Content "$project/.local/s5/migration.json" -Encoding UTF8
