[CmdletBinding()]
param(
    [string]$Root='D:/Temp/Diskweft-s4-optimization/classic',
    [string]$BuildRoot='D:/Projects/Diskweft/.local/s4/benchmark',
    [string]$OutputRoot='D:/Projects/Diskweft/.local/s4/performance/classic-current'
)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$rootPath=[IO.Path]::GetFullPath($Root)
if($rootPath -ne 'D:\Temp\Diskweft-s4-optimization\classic'){throw 'Use the exact task-owned classic fixture root'}
$image=Join-Path $rootPath 'fixture.vhdx'
$mount=Join-Path $rootPath 'mount'
if(Test-Path -LiteralPath $image){throw 'An existing image must be inspected, never recreated automatically'}
[IO.Directory]::CreateDirectory($rootPath) | Out-Null
[IO.Directory]::CreateDirectory($mount) | Out-Null
[IO.Directory]::CreateDirectory($OutputRoot) | Out-Null
$created=$false
$partition=$null
$disk=$null
$failure=$null
try {
    New-VHD -Path $image -Dynamic -SizeBytes 8GB | Out-Null
    $created=$true
    $owned=Get-VHD -Path $image
    if([IO.Path]::GetFullPath($owned.Path) -ne $image -or $owned.Size -ne 8GB){throw 'New VHD identity mismatch'}
    $disk=Mount-VHD -Path $image -NoDriveLetter -PassThru | Get-Disk
    if(@($disk).Count -ne 1 -or $disk.IsSystem -or $disk.IsBoot -or $disk.Size -ne 8GB -or $disk.PartitionStyle -ne 'RAW') {
        throw 'Refusing to initialize a disk that is not the newly created empty VHD'
    }
    $bound=Get-VHD -Path $image
    if(-not $bound.Attached -or $bound.DiskNumber -ne $disk.Number){throw 'Mounted image/disk binding mismatch'}
    $disk | Initialize-Disk -PartitionStyle GPT | Out-Null
    $partition=New-Partition -DiskNumber $disk.Number -UseMaximumSize
    $partition | Format-Volume -FileSystem NTFS -NewFileSystemLabel 'DiskweftValidation' -Confirm:$false | Out-Null
    $partition | Add-PartitionAccessPath -AccessPath ($mount+'\')
    $fixture=Join-Path $mount 'million'
    & "$BuildRoot/Diskweft.Tests.exe" prepare $fixture "$OutputRoot/fixture.json"
    if($LASTEXITCODE -ne 0){throw 'Million-file VHD fixture generation failed'}
    & "$PSScriptRoot/Invoke-PerformanceChecks.ps1" -Mode Classic -BuildRoot $BuildRoot -Fixture $fixture -OutputRoot $OutputRoot
    @{passed=$true;image=$image;diskNumber=$disk.Number;diskUniqueId=$disk.UniqueId;filesystem='NTFS';driveLetterAssigned=$false;fixture='Real 1,000,000 files in task-owned dynamic VHDX backed by D:'} | ConvertTo-Json | Set-Content "$OutputRoot/volume.json" -Encoding UTF8
} catch { $failure=$_; throw }
finally {
    if($created) {
        $state=Get-VHD -Path $image
        if($state.Attached) {
            if($null -ne $partition){
                $current=Get-Partition -DiskNumber $state.DiskNumber -PartitionNumber $partition.PartitionNumber
                if($current.AccessPaths -contains ($mount+'\')){$current | Remove-PartitionAccessPath -AccessPath ($mount+'\')}
            }
            Dismount-VHD -Path $image
        }
        $detached=-not (Get-VHD -Path $image).Attached
        @{detached=$detached;image=$image;failure=if($failure){$failure.Exception.Message}else{$null}} | ConvertTo-Json | Set-Content "$OutputRoot/lifecycle.json" -Encoding UTF8
        if(-not $detached){throw 'Owned VHD is still attached; preserve it and diagnose before cleanup'}
    }
}
