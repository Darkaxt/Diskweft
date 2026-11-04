[CmdletBinding()]
param([string]$RecoveryRoot='G:/Backup/Diskweft/SpaceScan-20261104')
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$project=Split-Path -Parent $PSScriptRoot
$inventoryRoot=Join-Path $project '.local/s5/inventory'
$inventory=Get-Content "$inventoryRoot/inventory.json" -Raw | ConvertFrom-Json
$recovery=[IO.Path]::GetFullPath($RecoveryRoot)
if($recovery -ne 'G:\Backup\Diskweft\SpaceScan-20261104' -or -not (Test-Path G:\)){throw 'Expected the available designated recovery drive'}
if(Test-Path $recovery){throw 'Preserve an existing recovery set; do not overwrite it'}
if(@(Get-CimInstance Win32_Process -Filter "Name='SpaceScan.exe' OR Name='Diskweft.exe'").Count -ne 0){throw 'Close the original/replacement app before copying its data'}
[IO.Directory]::CreateDirectory($recovery) | Out-Null
$appData=[Environment]::GetFolderPath('ApplicationData')
$sourceRoots=@{
 'C:\Program Files (x86)\ProtagonistLabs\SpaceScan'='original-install'
 "$appData\ProtagonistLabs\SpaceScan"='original-data'
 "$appData\SpaceScan"='legacy-data'
 "$appData\Darkaxt\Diskweft"='previous-diskweft-data'
}
$receipts=@()
foreach($file in $inventory.files) {
 if((Get-FileHash -LiteralPath $file.path).Hash -ne $file.sha256){throw "Source changed after inventory: $($file.path)"}
 $target=Join-Path (Join-Path $recovery $sourceRoots[$file.root]) $file.relative
 [IO.Directory]::CreateDirectory((Split-Path -Parent $target)) | Out-Null
 Copy-Item -LiteralPath $file.path -Destination $target
 if((Get-FileHash $target).Hash -ne $file.sha256){throw 'Recovery copy mismatch'}
 $receipts+=@{source=$file.path;recovery=$target;sha256=$file.sha256;bytes=$file.bytes}
}
Copy-Item -LiteralPath $inventoryRoot -Destination "$recovery/registry-inventory" -Recurse
Copy-Item -LiteralPath $inventory.cardPath -Destination "$recovery/spacescan-card.json"
if((Get-FileHash "$recovery/spacescan-card.json").Hash -ne $inventory.cardSha256){throw 'Recovery card mismatch'}
[IO.Directory]::CreateDirectory("$recovery/shortcuts") | Out-Null
foreach($link in $inventory.shortcuts) {
 $target=Join-Path "$recovery/shortcuts" ([IO.Path]::GetFileName($link.path))
 Copy-Item -LiteralPath $link.path -Destination $target
 if((Get-FileHash $target).Hash -ne $link.sha256){throw 'Recovery shortcut mismatch'}
}
$receipts | ConvertTo-Json -Depth 4 | Set-Content "$recovery/files.json" -Encoding UTF8
@{prepared=$true;recovery=$recovery;verifiedFiles=$receipts.Count;originalDataPreserved=$true;purpose='Exact pre-migration data/install/registry/shortcut recovery. Restore original files to recorded source paths and import reviewed .reg exports through the administrator broker; restore card last. Diskweft data created after migration is preserved separately, never overwritten blindly.'} | ConvertTo-Json | Set-Content "$project/.local/s5/recovery.json" -Encoding UTF8
