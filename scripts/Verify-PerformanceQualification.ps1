[CmdletBinding()]
param([string]$ResultName='mft-optimized2')
$ErrorActionPreference='Stop'
$project=Split-Path -Parent $PSScriptRoot
$benchmark=Join-Path $project '.local/s4/benchmark'
# One cohesive S4 qualification, under one explicit administrator consent.
# Source for the VHD cmdlet pipeline:
# https://learn.microsoft.com/powershell/module/hyper-v/new-vhd
& "$PSScriptRoot/Invoke-PerformanceChecks.ps1" -Mode Ntfs -BuildRoot $benchmark -OutputRoot "$project/.local/s4/performance/$ResultName"
& "$PSScriptRoot/Verify-ClassicVhdPerformance.ps1" -BuildRoot $benchmark
& "$PSScriptRoot/Verify-Stage2Ntfs.ps1" -Volume 'E:\' -BuildRoot 'D:/Temp/Diskweft-s4-optimization/native-ntfs' -ScratchRoot 'E:/Temp/Diskweft-s4-optimization-20261104'
