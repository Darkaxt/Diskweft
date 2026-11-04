[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$NetworkFixture,
    [string]$ScratchRoot='E:\Temp\Diskweft-s2-20261104', [string]$BuildRoot='D:\Temp\Diskweft-s2-build\regression')
$ErrorActionPreference='Stop'
$projectRoot=Split-Path -Parent $PSScriptRoot
& "$PSScriptRoot\Invoke-Stage1Checks.ps1" -ScratchRoot $ScratchRoot -BuildRoot $BuildRoot -NetworkFixture $NetworkFixture
& "$projectRoot\tests\Verify-NormalBuild.ps1" -ScratchRoot $ScratchRoot -BuildRoot $BuildRoot
