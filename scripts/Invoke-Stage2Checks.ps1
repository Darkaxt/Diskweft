[CmdletBinding()]
param([string]$ScratchRoot = 'E:\Temp\Diskweft-s2-20261104', [string]$BuildRoot = 'D:\Temp\Diskweft-s2-build')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
[IO.Directory]::CreateDirectory($ScratchRoot) | Out-Null
$oldTemp = $env:TEMP; $oldTmp = $env:TMP
try {
    $env:TEMP = $ScratchRoot; $env:TMP = $ScratchRoot
    & "$PSScriptRoot\Build-Development.ps1" -OutputDirectory $BuildRoot -Test -TestEntry Stage2Tests
    & "$BuildRoot\Diskweft.Tests.exe" (Join-Path $ScratchRoot ('records-' + [Guid]::NewGuid().ToString('N')))
    if ($LASTEXITCODE -ne 0) { throw 'S2 record workflow failed.' }
} finally { $env:TEMP = $oldTemp; $env:TMP = $oldTmp }
