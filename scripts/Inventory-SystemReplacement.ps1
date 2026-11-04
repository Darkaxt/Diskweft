[CmdletBinding()]
param([string]$OutputRoot)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
if(-not $OutputRoot){$OutputRoot=Join-Path (Split-Path -Parent $PSScriptRoot) '.local/s5/inventory'}
[IO.Directory]::CreateDirectory($OutputRoot) | Out-Null
$appData=[Environment]::GetFolderPath('ApplicationData')
$install='C:\Program Files (x86)\ProtagonistLabs\SpaceScan'
$files=@()
foreach($root in @($install,"$appData\ProtagonistLabs\SpaceScan","$appData\SpaceScan","$appData\Darkaxt\Diskweft")) {
 if(Test-Path -LiteralPath $root) {
  foreach($file in Get-ChildItem -LiteralPath $root -Recurse -File) {
   $files+=@{path=$file.FullName;root=$root;relative=$file.FullName.Substring($root.Length).TrimStart('\');bytes=$file.Length;sha256=(Get-FileHash -LiteralPath $file.FullName).Hash}
  }
 }
}
$registrations=@()
$registryKeys=@('HKCU\Software\SpaceScan','HKCU\Software\Diskweft')
foreach($kind in @('Directory','Drive','Directory\Background')) {
 foreach($name in @('SpaceScan','Diskweft')) {$registryKeys+="HKCU\Software\Classes\$kind\shell\$name"}
}
$uninstall='{5B7F3C21-9D4A-4E86-B0C7-2A9E6F1D8B53}_is1'
foreach($base in @('HKCU\Software','HKLM\Software','HKLM\Software\WOW6432Node')) {
 $registryKeys+="$base\Microsoft\Windows\CurrentVersion\Uninstall\$uninstall"
}
foreach($key in $registryKeys) {
 $provider=$key.Replace('HKCU\','Registry::HKEY_CURRENT_USER\').Replace('HKLM\','Registry::HKEY_LOCAL_MACHINE\')
 if(Test-Path -LiteralPath $provider) {
  $name=($key -replace '[^a-zA-Z0-9]','_')+'.reg'
  & "$env:WINDIR/System32/reg.exe" export $key (Join-Path $OutputRoot $name) /y | Out-Null
  if($LASTEXITCODE -ne 0){throw "Registry export failed: $key"}
  $registrations+=@{key=$key;export=$name;values=(Get-ItemProperty -LiteralPath $provider | Select-Object * -ExcludeProperty PS*)}
 }
}
$shortcuts=@()
$shell=New-Object -ComObject WScript.Shell
foreach($root in @([Environment]::GetFolderPath('DesktopDirectory'),[Environment]::GetFolderPath('CommonDesktopDirectory'),[Environment]::GetFolderPath('StartMenu'),[Environment]::GetFolderPath('CommonStartMenu'),"$appData\Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar")) {
 if(Test-Path $root) {
  foreach($file in Get-ChildItem -LiteralPath $root -Filter '*.lnk' -Recurse -File -ErrorAction SilentlyContinue) {
   $link=$shell.CreateShortcut($file.FullName)
   if($link.TargetPath -like '*SpaceScan*' -or $file.Name -like '*SpaceScan*') {
    $shortcuts+=@{path=$file.FullName;target=$link.TargetPath;arguments=$link.Arguments;workingDirectory=$link.WorkingDirectory;sha256=(Get-FileHash $file.FullName).Hash}
   }
  }
 }
}
$card=Join-Path $appData 'ProtagonistLabs\apps\spacescan.json'
$processes=@(Get-CimInstance Win32_Process -Filter "Name='SpaceScan.exe' OR Name='Diskweft.exe' OR Name='Pathweft.exe'" | Select-Object ProcessId,Name,ExecutablePath,CommandLine)
$result=@{capturedUtc=[DateTime]::UtcNow.ToString('o');files=$files;registry=$registrations;shortcuts=$shortcuts;processes=$processes;spaceScanCard=if(Test-Path $card){Get-Content $card -Raw | ConvertFrom-Json}else{$null};cardPath=$card;cardSha256=if(Test-Path $card){(Get-FileHash $card).Hash}else{$null};installedSpaceScanVersion=[Diagnostics.FileVersionInfo]::GetVersionInfo("$install\SpaceScan.exe").FileVersion;pathweftExe='D:\Tools\Pathweft\Pathweft.exe';pathweftVersion=[Diagnostics.FileVersionInfo]::GetVersionInfo('D:\Tools\Pathweft\Pathweft.exe').FileVersion}
$result | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $OutputRoot 'inventory.json') -Encoding UTF8
@{files=$files.Count;shortcuts=$shortcuts.Count;registryKeys=$registrations.Count;processes=$processes.Count;version=$result.installedSpaceScanVersion;pathweftVersion=$result.pathweftVersion;output=$OutputRoot} | ConvertTo-Json
