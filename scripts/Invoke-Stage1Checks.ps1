[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$ScratchRoot,
    [Parameter(Mandatory = $true)][string]$BuildRoot,
    [string]$NetworkFixture,
    [switch]$CompileOnly
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$scratch = [IO.Path]::GetFullPath($ScratchRoot)
$build = [IO.Path]::GetFullPath($BuildRoot)
if (-not $scratch.StartsWith('E:\Temp\Diskweft-s1-', [StringComparison]::OrdinalIgnoreCase)) { throw 'Unexpected S1 scratch root.' }
if (-not $build.StartsWith('D:\Temp\Diskweft-s1-build', [StringComparison]::OrdinalIgnoreCase)) { throw 'Unexpected S1 native build root.' }
[IO.Directory]::CreateDirectory($scratch) | Out-Null
[IO.Directory]::CreateDirectory($build) | Out-Null
$oldTemp = $env:TEMP; $oldTmp = $env:TMP; $oldNetwork = $env:DISKWEFT_NETWORK_FIXTURE
try {
    $env:TEMP = $scratch; $env:TMP = $scratch
    & (Join-Path $PSScriptRoot 'Build-Development.ps1') -OutputDirectory $build
    & (Join-Path $projectRoot 'tests\Test-IdentityContract.ps1') -AssemblyPath (Join-Path $build 'Diskweft.exe')
    if ($LASTEXITCODE -ne 0) { throw 'Identity contract failed.' }
    if ($CompileOnly) { return }
    if (-not $NetworkFixture) { throw 'Specify the approved read-only network fixture.' }
    & (Join-Path $PSScriptRoot 'Build-Development.ps1') -OutputDirectory $build -Test
    $runRoot = Join-Path $scratch ('acceptance-' + [Guid]::NewGuid().ToString('N'))
    $env:DISKWEFT_NETWORK_FIXTURE = $NetworkFixture
    & (Join-Path $build 'Diskweft.Tests.exe') $runRoot
    if ($LASTEXITCODE -ne 0) { throw "S1 acceptance failed. Inspect $runRoot\result.json" }
    Write-Output "Inspected acceptance evidence: $runRoot"
} finally {
    $env:TEMP = $oldTemp; $env:TMP = $oldTmp; $env:DISKWEFT_NETWORK_FIXTURE = $oldNetwork
}
