[CmdletBinding()]
param([ValidateSet('Diagnostic','Qualify','Native')][string]$Mode='Diagnostic')
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$project=Split-Path -Parent $PSScriptRoot
$root='D:\Temp\Diskweft-s4-optimization\classic'
$image=Join-Path $root 'fixture.vhdx'
$mount=Join-Path $root 'mount'
$output=Join-Path $project ('.local/s4/performance/classic-'+$Mode.ToLowerInvariant()+$(if($Mode -ne 'Native'){'-cpu0'}else{''}))
$build=Join-Path $project '.local/s4/benchmark'
$baseline=Join-Path $project '.local/baseline/SpaceScan-1.4.1.exe'
$state=Get-VHD -Path $image
if($state.Attached -or $state.Size -ne 8GB -or $state.VhdType -ne 'Dynamic'){throw 'Expected the detached owned 8GiB image'}
$partition=$null
[IO.Directory]::CreateDirectory($output) | Out-Null
try {
    $disk=Mount-VHD -Path $image -ReadOnly -NoDriveLetter -PassThru | Get-Disk
    $bound=Get-VHD -Path $image
    if(@($disk).Count -ne 1 -or $disk.IsSystem -or $disk.IsBoot -or $bound.DiskNumber -ne $disk.Number -or -not $disk.IsReadOnly){throw 'Read-only owned image/disk binding mismatch'}
    $partitions=@(Get-Partition -DiskNumber $disk.Number | Where-Object Type -eq 'Basic')
    if($partitions.Count -ne 1){throw 'Expected one fixture data partition'}
    $partition=$partitions[0]
    $volume=$partition | Get-Volume
    if($volume.FileSystemLabel -ne 'DiskweftValidation' -or $volume.FileSystem -ne 'NTFS'){throw 'Fixture volume identity mismatch'}
    $partition | Add-PartitionAccessPath -AccessPath ($mount+'\')
    $fixture=Join-Path $mount 'million'
    if($Mode -eq 'Native') {
        & "$build/Diskweft.Tests.exe" native-host $fixture "$output/wpf"
        if($LASTEXITCODE -ne 0){throw 'Host native million-file acceptance failed'}
    } elseif($Mode -eq 'Qualify') {
        & "$PSScriptRoot/Invoke-PerformanceChecks.ps1" -Mode Classic -BuildRoot $build -Fixture $fixture -OutputRoot $output -Processor 0
    } else {
    foreach($run in 0..2) {
        foreach($kind in @('baseline','core','fork')) {
            & "$build/Diskweft.Tests.exe" $kind classic $fixture "$output/$kind-$run.json" $baseline
            if($LASTEXITCODE -ne 0){throw "$kind diagnostic failed"}
        }
    }
    }
} finally {
    $state=Get-VHD -Path $image
    if($state.Attached) {
        if($null -ne $partition) {
            $current=Get-Partition -DiskNumber $state.DiskNumber -PartitionNumber $partition.PartitionNumber
            if($current.AccessPaths -contains ($mount+'\')){$current | Remove-PartitionAccessPath -AccessPath ($mount+'\')}
        }
        Dismount-VHD -Path $image
    }
    $detached=-not (Get-VHD -Path $image).Attached
    @{detached=$detached;image=$image;readOnly=$true} | ConvertTo-Json | Set-Content "$output/lifecycle.json" -Encoding UTF8
    if(-not $detached){throw 'Owned image remains attached'}
}
if($Mode -eq 'Qualify') {
    & "$project/tests/Verify-MftQuery.ps1"
    & "$PSScriptRoot/Verify-Stage2Ntfs.ps1" -Volume 'E:\' -BuildRoot 'D:/Temp/Diskweft-s4-optimization/native-ntfs' -ScratchRoot 'E:/Temp/Diskweft-s4-optimization-20261104'
}
