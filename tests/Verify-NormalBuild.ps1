[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$BuildRoot,
    [Parameter(Mandatory = $true)][string]$ScratchRoot
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$projectRoot = Split-Path -Parent $PSScriptRoot
$exe = Join-Path ([IO.Path]::GetFullPath($BuildRoot)) 'Diskweft.exe'
$baseline = Join-Path $projectRoot '.local\baseline\SpaceScan-1.4.1.exe'
$scratch = [IO.Path]::GetFullPath($ScratchRoot)
if ($scratch -notmatch '^E:\\Temp\\Diskweft-(?:s[1-4]|s4-optimization|release-notes-fix)-[0-9]{8}$') { throw 'Use owned Diskweft acceptance scratch.' }
$run = Join-Path $scratch ('normal-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory((Join-Path $run 'fixture\child')) | Out-Null
[IO.File]::WriteAllBytes((Join-Path $run 'fixture\one.bin'), (New-Object byte[] 1024))
[IO.File]::WriteAllBytes((Join-Path $run 'fixture\child\two.bin'), (New-Object byte[] 2048))
[IO.File]::WriteAllBytes((Join-Path $run 'fixture\skip.tmp'), (New-Object byte[] 128))

function Read-UpstreamBindings {
    $bindings = [ordered]@{}
    foreach ($name in @('Software\SpaceScan','Software\Classes\Directory\shell\SpaceScan\command','Software\Classes\Drive\shell\SpaceScan\command','Software\Classes\Directory\Background\shell\SpaceScan\command')) {
        $key = [Microsoft.Win32.Registry]::CurrentUser.OpenSubKey($name)
        try {
            $bindings[$name] = if ($key) { @($key.GetValueNames() | Sort-Object | ForEach-Object { $_ + '=' + [string]$key.GetValue($_) }) } else { $null }
        } finally { if ($key) { $key.Dispose() } }
    }
    $appData = [Environment]::GetFolderPath('ApplicationData')
    foreach ($relative in @('SpaceScan\exclude.txt','ProtagonistLabs\SpaceScan\exclude.txt','ProtagonistLabs\apps\spacescan.json')) {
        $path = Join-Path $appData $relative
        $bindings[$relative] = if (Test-Path -LiteralPath $path) { (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash } else { $null }
    }
    return ($bindings | ConvertTo-Json -Depth 5 -Compress)
}
$before = Read-UpstreamBindings
$oldFork = $env:DISKWEFT_TEST_ROOT; $oldUpstream = $env:SPACESCAN_TEST_ROOT
$oldTemp = $env:TEMP; $oldTmp = $env:TMP
try {
    $env:DISKWEFT_TEST_ROOT = Join-Path $run 'fork-profile'
    $env:SPACESCAN_TEST_ROOT = Join-Path $run 'upstream-profile'
    $env:TEMP = $run; $env:TMP = $run
    $assembly = [Reflection.Assembly]::LoadFile($exe)
    $flags = [Reflection.BindingFlags]'Static,NonPublic,Public,Instance'
    $ui = $assembly.GetType('MainUI')
    if ($ui.GetMethod('DiagnosticScan', $flags) -or $ui.GetField('DiagnosticDrivePaths', $flags) -or $assembly.GetType('ScanSession').GetField('Enumerated', $flags) -or $assembly.GetType('Mft').GetField('RecordsObserved', $flags) -or $assembly.GetType('Mft').GetMethod('ReplayRecords', $flags) -or $assembly.GetType('Appearance').GetField('SystemOverride', $flags)) {
        throw 'Test actions/barriers leaked into the normal build.'
    }
    $eco = $assembly.GetType('Ecosystem')
    $data = $eco.GetField('Data', $flags).GetValue($null)
    if ($data -ne (Join-Path $env:DISKWEFT_TEST_ROOT 'Darkaxt\Diskweft')) { throw 'Independent profile mismatch.' }
    $feed = $assembly.GetType('Updater').GetField('Feed', $flags).GetValue($null)
    if ($feed -ne 'https://api.github.com/repos/Darkaxt/Diskweft/releases/latest') { throw 'Wrong updater feed.' }
    $shell = $ui.GetField('ShellKeys', $flags).GetValue($null)
    if (@($shell | Where-Object { -not $_.EndsWith('\Diskweft') }).Count -ne 0) { throw 'Wrong shell identity.' }
    foreach ($program in @(@{Exe=$baseline;Name='baseline'}, @{Exe=$exe;Name='fork'})) {
        $args = @(('"' + (Join-Path $run 'fixture') + '"'), '--csv', ('"' + (Join-Path $run ($program.Name + '.csv')) + '"'), '--report', ('"' + (Join-Path $run ($program.Name + '.html')) + '"'), '--snapshot', ('"' + (Join-Path $run ($program.Name + '.scan')) + '"'), '--exclude', '*.tmp')
        $process = Start-Process -FilePath $program.Exe -ArgumentList $args -WindowStyle Hidden -PassThru -Wait
        if ($process.ExitCode -ne 0) { throw "$($program.Name) CLI failed with exit $($process.ExitCode)." }
    }
    if (Compare-Object (Get-Content -LiteralPath (Join-Path $run 'baseline.csv')) (Get-Content -LiteralPath (Join-Path $run 'fork.csv'))) { throw 'CLI CSV differs from the retained baseline on unchanged controlled data.' }
    if (-not (Get-Content -LiteralPath (Join-Path $run 'fork.html') -Raw).Contains('Diskweft report')) { throw 'Report identity mismatch.' }
    if ((Read-UpstreamBindings) -ne $before) { throw 'Existing upstream bindings/settings changed during isolated normal-build verification.' }
    $installer = Get-Content -LiteralPath (Join-Path $projectRoot 'installer\Diskweft.iss') -Raw
    if (-not $installer.Contains('5F94B63F-3FCB-4EAF-AF7E-28AA846D6215') -or $installer.Contains('5B7F3C21-9D4A-4E86-B0C7-2A9E6F1D8B53') -or $installer.Contains('SpaceScan.exe') -or $installer.Contains('taskkill') -or $installer.Contains('MoveData')) { throw 'Installer identity/isolation mismatch.' }
    [ordered]@{
        passed=$true; normalVersion=[Diagnostics.FileVersionInfo]::GetVersionInfo($exe).FileVersion
        normalSha256=(Get-FileHash -LiteralPath $exe -Algorithm SHA256).Hash
        baselineCsvExact=$true; upstreamBindingsUnchanged=$true; testHooksAbsent=$true
        updaterFeed=$feed; installerSourceIsolated=$true; installerInstalled=$false
    } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $run 'result.json') -Encoding UTF8
    Write-Output "Normal-build identity/CLI evidence: $run"
} finally {
    $env:DISKWEFT_TEST_ROOT=$oldFork; $env:SPACESCAN_TEST_ROOT=$oldUpstream
    $env:TEMP=$oldTemp; $env:TMP=$oldTmp
}
