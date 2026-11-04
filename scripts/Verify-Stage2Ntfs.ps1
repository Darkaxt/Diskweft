[CmdletBinding()]
param([string]$Volume = 'E:\', [string]$BuildRoot = 'D:\Temp\Diskweft-s2-build', [string]$ScratchRoot = 'E:\Temp\Diskweft-s2-20261104')
$ErrorActionPreference = 'Stop'
$oldTemp=$env:TEMP; $oldTmp=$env:TMP
try {
    $env:TEMP=$ScratchRoot; $env:TMP=$ScratchRoot
    $output=Join-Path $ScratchRoot ('ntfs-' + [Guid]::NewGuid().ToString('N'))
    & "$BuildRoot\Diskweft.Tests.exe" $output $Volume
    if ($LASTEXITCODE -ne 0) { throw "Real NTFS acceptance failed: $output" }
    Get-Content (Join-Path $output 'result.json')
} finally { $env:TEMP=$oldTemp; $env:TMP=$oldTmp }
