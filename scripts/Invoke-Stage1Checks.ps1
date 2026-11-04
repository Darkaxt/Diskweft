[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$ScratchRoot,
    [Parameter(Mandatory = $true)][string]$BuildRoot,
    [string]$NetworkFixture,
    [switch]$CompileOnly,
    [switch]$CoreOnly
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$scratch = [IO.Path]::GetFullPath($ScratchRoot)
$build = [IO.Path]::GetFullPath($BuildRoot)
if ($scratch -notmatch '^E:\\Temp\\Diskweft-(?:s[1-4]|s4-optimization)-[0-9]{8}$') { throw 'Unexpected Diskweft acceptance scratch root.' }
if ($build -notmatch '^D:\\Temp\\Diskweft-(?:s[1-4]-build|s4-optimization)(?:\\regression)?$') { throw 'Unexpected Diskweft native build root.' }
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
    if ($CoreOnly) { & (Join-Path $build 'Diskweft.Tests.exe') $runRoot --core }
    else { & (Join-Path $build 'Diskweft.Tests.exe') $runRoot }
    if ($LASTEXITCODE -ne 0) { throw "S1 acceptance failed. Inspect $runRoot\result.json" }
    Write-Output "Inspected acceptance evidence: $runRoot"
} finally {
    $env:TEMP = $oldTemp; $env:TMP = $oldTmp; $env:DISKWEFT_NETWORK_FIXTURE = $oldNetwork
}
