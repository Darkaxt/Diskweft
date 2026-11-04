$ErrorActionPreference='Stop'
$project=Split-Path -Parent $PSScriptRoot
& 'D:/Temp/Diskweft-release-notes-fix/benchmark/Diskweft.Tests.exe' native-mft 'E:\' "$project/.local/s4/maintenance-mft"
if($LASTEXITCODE -ne 0){throw 'Current native million-file NTFS latency/sorting acceptance failed'}
& "$PSScriptRoot/Verify-Stage2Ntfs.ps1" -Volume 'E:\' -BuildRoot 'D:/Temp/Diskweft-release-notes-fix/ntfs' -ScratchRoot 'E:/Temp/Diskweft-release-notes-fix-20261104'
