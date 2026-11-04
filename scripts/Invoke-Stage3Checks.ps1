[CmdletBinding()]
param([string]$ScratchRoot='E:\Temp\Diskweft-s3-20261104',[string]$BuildRoot='D:\Temp\Diskweft-s3-build\payload')
$ErrorActionPreference='Stop'
[IO.Directory]::CreateDirectory($ScratchRoot) | Out-Null
$oldTemp=$env:TEMP;$oldTmp=$env:TMP
try {
    $env:TEMP=$ScratchRoot;$env:TMP=$ScratchRoot
    & "$PSScriptRoot\Build-Development.ps1" -OutputDirectory $BuildRoot -Test -TestEntry Stage3Tests
    & "$BuildRoot\Diskweft.Tests.exe" (Join-Path $ScratchRoot ('palette-'+[Guid]::NewGuid().ToString('N')))
    if($LASTEXITCODE -ne 0){throw 'S3 palette diagnostics failed.'}
}finally{$env:TEMP=$oldTemp;$env:TMP=$oldTmp}
