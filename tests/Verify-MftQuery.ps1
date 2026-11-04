[CmdletBinding()]
param(
    [string]$AssemblyPath='D:/Projects/Diskweft/.local/s4/benchmark/Diskweft.Tests.exe',
    [string]$ScratchRoot='E:/Temp/Diskweft-s4-optimization-20261104',
    [string]$Output='D:/Projects/Diskweft/.local/s4/mft-query.json'
)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$scratch=[IO.Path]::GetFullPath($ScratchRoot)
if($scratch -ne 'E:\Temp\Diskweft-s4-optimization-20261104'){throw 'Use the exact owned query fixture root'}
[IO.Directory]::CreateDirectory($scratch) | Out-Null
$marker=Join-Path $scratch ('mft-query-'+[Guid]::NewGuid().ToString('N')+'.txt')
$stream=New-Object IO.FileStream($marker,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::Read)
try { $data=New-Object byte[] 137; $stream.Write($data,0,$data.Length); $stream.Flush($true) }
finally { $stream.Dispose() }
$assembly=[Reflection.Assembly]::LoadFrom([IO.Path]::GetFullPath($AssemblyPath))
$flags=[Reflection.BindingFlags]'Public,NonPublic,Static,Instance'
$empty=New-Object object[] 1
$empty[0]=[string[]]@()
$assembly.GetType('Exclude').GetMethod('Set',$flags).Invoke($null,$empty) | Out-Null
$assembly.GetType('Scanner').GetField('Cancel',$flags).SetValue($null,$false)
$query=[Activator]::CreateInstance($assembly.GetType('Query'),$true)
$query.Name=[IO.Path]::GetFileName($marker);$query.MinSize=137;$query.Max=10
$hits=$assembly.GetType('Mft').GetMethod('Collect',$flags).Invoke($null,@('E:\',$query))
$match=@($hits | Where-Object { $_.FullPath.Equals($marker,[StringComparison]::OrdinalIgnoreCase) -and $_.Size -eq 137 })
if($match.Count -ne 1){throw 'Real MFT query did not resolve the flushed owned marker exactly once'}
@{passed=$true;realMftQuery=$true;exactPath=$true;markerBytes=137;matches=$match.Count;scope='Actual query path through the bounded raw reader; owned flushed file only'} | ConvertTo-Json | Set-Content $Output -Encoding UTF8
