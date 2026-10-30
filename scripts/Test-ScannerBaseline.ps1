[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$AssemblyPath,
    [Parameter(Mandatory = $true)][string]$ScratchRoot,
    [Parameter(Mandatory = $true)][string]$ResultPath
)

# Controlled baseline of existing scanner behavior. No GUI/installer entry point,
# arbitrary user-tree enumeration, administrator action or direct cleanup.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

if ($PSVersionTable.PSEdition -ne 'Desktop') {
    throw 'Use Windows PowerShell 5.1 for the .NET Framework baseline.'
}
$assemblyFile = (Resolve-Path -LiteralPath $AssemblyPath).Path
$fixturePath = [IO.Path]::GetFullPath($ScratchRoot)
$tempAnchor = [IO.Path]::GetFullPath('E:\Temp\')
if (-not $fixturePath.StartsWith($tempAnchor, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'S0 fixtures must use an exact descendant of E:\Temp.'
}
if (Test-Path -LiteralPath $fixturePath) {
    throw 'Refusing to reuse an existing fixture path.'
}
$resultFile = [IO.Path]::GetFullPath($ResultPath)
if (Test-Path -LiteralPath $resultFile) {
    throw 'Refusing to overwrite an existing baseline result.'
}

[IO.Directory]::CreateDirectory($fixturePath) | Out-Null
$nested = Join-Path $fixturePath 'child\nested'
[IO.Directory]::CreateDirectory($nested) | Out-Null
[IO.Directory]::CreateDirectory((Join-Path $fixturePath 'empty')) | Out-Null
[IO.File]::WriteAllBytes((Join-Path $fixturePath 'root.bin'), (New-Object byte[] 1000))
[IO.File]::WriteAllBytes((Join-Path $fixturePath 'child\child.bin'), (New-Object byte[] 2000))
[IO.File]::WriteAllBytes((Join-Path $nested 'deep.bin'), (New-Object byte[] 4000))
[IO.File]::WriteAllBytes((Join-Path $fixturePath 'skip.tmp'), (New-Object byte[] 500))
[IO.File]::WriteAllBytes((Join-Path $nested 'zero.dat'), (New-Object byte[] 0))

$env:SPACESCAN_TEST_ROOT = Join-Path $fixturePath 'isolated-profile'
$assembly = [Reflection.Assembly]::LoadFile($assemblyFile)
$nodeType = $assembly.GetType('Node', $true)
$scannerType = $assembly.GetType('Scanner', $true)
$statsType = $assembly.GetType('Stats', $true)
$excludeType = $assembly.GetType('Exclude', $true)
$scanMethod = $scannerType.GetMethod('Scan')
$setExclusions = $excludeType.GetMethod('Set')

function Invoke-FixtureScan {
    param([string[]]$Patterns = @(), [bool]$Cancelled = $false)
    $statsType.GetMethod('Reset').Invoke($null, $null) | Out-Null
    $setExclusions.Invoke($null, [object[]]@(, [string[]]$Patterns)) | Out-Null
    $scannerType.GetField('Cancel').SetValue($null, $Cancelled)
    $scannerType.GetField('FilesSeen').SetValue($null, [long]0)
    $scannerType.GetField('Total').SetValue($null, [long]0)
    $scannerType.GetField('DeniedCount').SetValue($null, [int]0)
    $scannerType.GetField('CollectExt').SetValue($null, $true)
    $node = [Activator]::CreateInstance($nodeType)
    $nodeType.GetField('Name').SetValue($node, $fixturePath)
    $scanMethod.Invoke($null, [object[]]@($node, $fixturePath)) | Out-Null
    $statsType.GetMethod('Trim').Invoke($null, $null) | Out-Null
    return $node
}

function Assert-Total {
    param($Node, [long]$Bytes, [long]$Files, [string]$Case)
    if ($Node.Size -ne $Bytes -or $Node.Files -ne $Files) {
        throw "$Case mismatch: $($Node.Size) bytes / $($Node.Files) files; expected $Bytes / $Files."
    }
}

$full = Invoke-FixtureScan
Assert-Total $full 7500 5 'Full tree'
$child = @($full.Dirs | Where-Object { $_.Name -eq 'child' })[0]
Assert-Total $child 6000 3 'Nested subtree'
$empty = @($full.Dirs | Where-Object { $_.Name -eq 'empty' })[0]
Assert-Total $empty 0 0 'Empty directory'
$top = $statsType.GetField('Top').GetValue($null)
$extensions = $statsType.GetField('Ext').GetValue($null)
if ($top.Count -ne 5 -or $top[0].Name -ne 'deep.bin' -or $top[0].Size -ne 4000) {
    throw 'Largest-file baseline mismatch.'
}
if ($extensions['.bin'][0] -ne 7000 -or $extensions['.bin'][1] -ne 3) {
    throw 'Extension baseline mismatch.'
}
$nameExcluded = Invoke-FixtureScan -Patterns @('*.tmp')
Assert-Total $nameExcluded 7000 4 'Name exclusion'
$pathExcluded = Invoke-FixtureScan -Patterns @((Join-Path $fixturePath 'child'))
Assert-Total $pathExcluded 1500 2 'Subtree exclusion'
$cancelled = Invoke-FixtureScan -Cancelled $true
Assert-Total $cancelled 0 0 'Pre-cancelled scan'
$scannerType.GetField('Cancel').SetValue($null, $false)
$setExclusions.Invoke($null, [object[]]@(, [string[]]@())) | Out-Null

$result = [ordered]@{
    schema = 1
    observedUtc = [DateTime]::UtcNow.ToString('o')
    assemblyVersion = $assembly.GetName().Version.ToString()
    assemblySha256 = (Get-FileHash -LiteralPath $assemblyFile -Algorithm SHA256).Hash.ToLowerInvariant()
    guiLaunched = $false
    elevatedMftTested = $false
    cases = @(
        @{ name = 'full-tree'; bytes = 7500; files = 5; passed = $true },
        @{ name = 'nested-subtree'; bytes = 6000; files = 3; passed = $true },
        @{ name = 'empty-directory'; bytes = 0; files = 0; passed = $true },
        @{ name = 'largest-files'; largestBytes = 4000; entries = 5; passed = $true },
        @{ name = 'extension-totals'; bytes = 7000; files = 3; passed = $true },
        @{ name = 'name-exclusion'; bytes = 7000; files = 4; passed = $true },
        @{ name = 'subtree-exclusion'; bytes = 1500; files = 2; passed = $true },
        @{ name = 'pre-cancelled-scan'; bytes = 0; files = 0; passed = $true }
    )
    coverage = 'Existing ordinary scanner only; no UI, live publication, mid-scan cancellation, MFT or performance claim.'
}
[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($resultFile)) | Out-Null
$result | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $resultFile -Encoding UTF8
Write-Output ($result | ConvertTo-Json -Depth 6 -Compress)
