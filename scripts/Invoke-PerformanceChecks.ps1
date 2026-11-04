[CmdletBinding()]
param(
    [ValidateSet('Prepare','Classic','Ntfs')][string]$Mode,
    [string]$BuildRoot = 'D:/Temp/Diskweft-s4-build',
    [string]$Fixture = 'D:/Temp/Diskweft-s4-fixture/million',
    [string]$Volume = 'E:\',
    [string]$OutputRoot = 'D:/Projects/Diskweft/.local/s4/performance',
    [int]$Processor = -1
)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$Fixture=[IO.Path]::GetFullPath($Fixture)
$OutputRoot=[IO.Path]::GetFullPath($OutputRoot)
[IO.Directory]::CreateDirectory($OutputRoot) | Out-Null
$probe=Join-Path $BuildRoot 'Diskweft.Tests.exe'
if($Mode -eq 'Prepare') {
    & "$PSScriptRoot/Build-Development.ps1" -OutputDirectory $BuildRoot -Test -TestEntry Stage4Bench
    & $probe prepare $Fixture "$OutputRoot/fixture.json"
    if($LASTEXITCODE -ne 0){throw 'Fixture generation failed'}
    return
}
$engine=if($Mode -eq 'Ntfs'){'mft'}else{'classic'}
$target=if($Mode -eq 'Ntfs'){$Volume}else{$Fixture}
$baseline=Join-Path $root '.local/baseline/SpaceScan-1.4.1.exe'
if((Get-FileHash $baseline).Hash -ne '1EACBA02C172A76D21E7B0BED77DA51783D04CFE1B0926DD2D5E51EF7E650DEF'){throw 'Retained baseline hash mismatch'}
# Restrict these short-lived probe processes to the same logical processor.
# The ordinary application and host scheduler configuration remain untouched.
$previousProcessor=$env:DISKWEFT_BENCH_PROCESSOR
try {
$env:DISKWEFT_BENCH_PROCESSOR=if($Processor -ge 0){[string]$Processor}else{$null}
# Both engines receive one unmeasured warm-up. No cache flush or machine settings changes.
foreach($kind in @('baseline','fork')) {
    & $probe $kind $engine $target "$OutputRoot/$engine-$kind-warmup.json" $baseline
    if($LASTEXITCODE -ne 0){throw "$kind warm-up failed"}
}
foreach($pair in 1..3) {
    $order=if($pair%2 -eq 1){@('baseline','fork')}else{@('fork','baseline')}
    foreach($kind in $order) {
        & $probe $kind $engine $target "$OutputRoot/$engine-$kind-$pair.json" $baseline
        if($LASTEXITCODE -ne 0){throw "$kind pair $pair failed"}
    }
}
$base=@(1..3 | ForEach-Object { Get-Content "$OutputRoot/$engine-baseline-$_.json" -Raw | ConvertFrom-Json })
$fork=@(1..3 | ForEach-Object { Get-Content "$OutputRoot/$engine-fork-$_.json" -Raw | ConvertFrom-Json })
$baseMedian=($base.milliseconds | Sort-Object)[1]
$forkMedian=($fork.milliseconds | Sort-Object)[1]
$increase=100*($forkMedian/$baseMedian-1)
$paired=@(0..2 | ForEach-Object {100*($fork[$_].milliseconds/$base[$_].milliseconds-1)})
$pairedMedian=($paired | Sort-Object)[1]
$result=[ordered]@{passed=($increase -le 10 -and $pairedMedian -le 10);engine=$engine;baselineMedianMs=$baseMedian;forkMedianMs=$forkMedian;increasePercent=$increase;pairedIncreases=$paired;pairedMedianIncreasePercent=$pairedMedian;baseline=$base;fork=$fork;processor=$Processor;cache='One warm-up each; alternate first engine across paired runs; optional identical process-only affinity recorded by processor (-1 means inherited); ordinary host load, no cache flush'}
$result | ConvertTo-Json -Depth 5 | Set-Content "$OutputRoot/$engine-summary.json" -Encoding UTF8
if(-not $result.passed){throw "$engine scan-time increase exceeds 10 percent: ratio of medians $increase, median paired increase $pairedMedian"}
} finally { $env:DISKWEFT_BENCH_PROCESSOR=$previousProcessor }
