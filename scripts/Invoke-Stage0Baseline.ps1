[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$ScratchRoot
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
if ($PSVersionTable.PSEdition -ne 'Desktop') {
    throw 'Use Windows PowerShell 5.1.'
}
$projectRoot = Split-Path -Parent $PSScriptRoot
$baseline = '313271a0bb34e63c8182c49e9c71caabbd782a4a'
$scratch = [IO.Path]::GetFullPath($ScratchRoot)
if (-not $scratch.StartsWith('E:\Temp\', [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Use an exact task-owned descendant of E:\Temp for S0 scratch.'
}
if (Test-Path -LiteralPath $scratch) { throw 'Scratch must not already exist.' }
$evidence = Join-Path $projectRoot '.local\baseline'
if (Test-Path -LiteralPath $evidence) { throw 'Baseline evidence already exists; inspect rather than overwrite.' }
$buildOutput = Join-Path $projectRoot 'SpaceScan.exe'
if (Test-Path -LiteralPath $buildOutput) { throw 'Inspect the pre-existing build output before proceeding.' }

& git -C $projectRoot diff --quiet $baseline -- App.cs SpaceScan.cs MainWindow.xaml WhatsNew.cs build.cmd app.manifest SpaceScan.ico WhatsNew whatsnew.txt
if ($LASTEXITCODE -ne 0) { throw 'Upstream production/build files differ from the pinned baseline.' }

[IO.Directory]::CreateDirectory($evidence) | Out-Null
$compilerScratch = Join-Path $scratch 'compiler'
[IO.Directory]::CreateDirectory($compilerScratch) | Out-Null
$originalTemp = $env:TEMP
$originalTmp = $env:TMP
try {
    $env:TEMP = $compilerScratch
    $env:TMP = $compilerScratch
    & (Join-Path $projectRoot 'build.cmd') 2>&1 | Tee-Object -FilePath (Join-Path $evidence 'build.log')
    if ($LASTEXITCODE -ne 0) { throw 'Existing upstream build.cmd failed.' }
    if (-not (Test-Path -LiteralPath $buildOutput)) { throw 'Compiler did not produce the expected assembly.' }

    $baselineExe = Join-Path $evidence 'SpaceScan-1.4.1.exe'
    Move-Item -LiteralPath $buildOutput -Destination $baselineExe
    $version = [Diagnostics.FileVersionInfo]::GetVersionInfo($baselineExe).FileVersion
    if ($version -ne '1.4.1') { throw "Unexpected baseline file version: $version" }
    & (Join-Path $PSScriptRoot 'Test-ScannerBaseline.ps1') -AssemblyPath $baselineExe -ScratchRoot (Join-Path $scratch 'scanner-fixture') -ResultPath (Join-Path $evidence 'scanner-result.json')
    $result = Get-Content -LiteralPath (Join-Path $evidence 'scanner-result.json') -Raw | ConvertFrom-Json
    if (@($result.cases | Where-Object { -not $_.passed }).Count -ne 0) {
        throw 'A baseline fixture assertion failed.'
    }
    [ordered]@{
        sourceCommit = $baseline
        fileVersion = $version
        compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
        signatureStatus = (Get-AuthenticodeSignature -LiteralPath $baselineExe).Status.ToString()
        assemblySha256 = $result.assemblySha256
        scratch = $scratch
        scratchConsumer = 'Exited S0 compiler and reflection baseline; eligible for transactional cleanup.'
        retainedConsumer = 'S1-S4 baseline correctness and performance comparisons.'
    } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $evidence 'provenance.json') -Encoding UTF8
}
finally {
    $env:TEMP = $originalTemp
    $env:TMP = $originalTmp
}

Write-Output 'S0 baseline verification passed. Clean the exact scratch through transactional-cleanup.'
