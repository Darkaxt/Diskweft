[CmdletBinding()]
param([string]$DownloadRoot='E:/Temp/Diskweft-release-verification-20261104/0.1.1',[Parameter(Mandatory=$true)][ValidatePattern('^[0-9a-f]{40}$')][string]$ExpectedCommit)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$project=Split-Path -Parent $PSScriptRoot
$root=[IO.Path]::GetFullPath($DownloadRoot)
if($root -ne 'E:\Temp\Diskweft-release-verification-20261104\0.1.1'){throw 'Use the exact registered delivered-asset scratch'}
$expected=@{
 'Diskweft-0.1.1-setup.exe'='c149acb540523b24656226f7f28c5d88492fe0c6915b5121e300839776b8880e'
 'Diskweft-0.1.1-portable.zip'='4cf754a4f34879ad18a51c137ef505294ca448b848a988b77f3774f5b1bf105a'
}
$release=& gh release view v0.1.1 --repo Darkaxt/Diskweft --json tagName,targetCommitish,isDraft,isPrerelease
if($LASTEXITCODE -ne 0){throw 'Release metadata could not be retrieved'}
$metadata=$release | ConvertFrom-Json
if($metadata.isDraft -or $metadata.isPrerelease -or $metadata.targetCommitish -ne $ExpectedCommit -or $metadata.tagName -ne 'v0.1.1'){throw 'Published release/source mismatch'}
foreach($line in Get-Content "$root/SHA256SUMS.txt") {
 $parts=$line -split '\s+',2
 if(-not $expected.ContainsKey($parts[1]) -or $parts[0] -ne $expected[$parts[1]]){throw 'Delivered checksum manifest mismatch'}
 if((Get-FileHash (Join-Path $root $parts[1])).Hash.ToLowerInvariant() -ne $parts[0]){throw 'Delivered asset hash mismatch'}
}
if(@(Get-Content "$root/SHA256SUMS.txt").Count -ne 2){throw 'Expected both package checksums'}
$portable=Join-Path $root 'portable'
if(Test-Path $portable){throw 'Inspect an existing extraction rather than replacing it'}
Expand-Archive -LiteralPath "$root/Diskweft-0.1.1-portable.zip" -DestinationPath $portable
if((Get-FileHash "$portable/Diskweft.exe").Hash -ne '9903E7E15EC2F8A2931582187F69BF2903B7C891327057E6A2E0B60B6F6F1046'){throw 'Delivered executable mismatch'}
if((Get-FileHash "$portable/Diskweft.exe.config").Hash -ne '28472E5DA425ECCD7D67A8D830D94524E8BF08EF321991731DBDDA2D07A1BC75'){throw 'Delivered runtime configuration mismatch'}
& "$project/tests/Verify-Package.ps1" -PackageRoot $portable -OutputRoot "$root/runtime"
$result=Get-Content "$root/runtime/result.json" -Raw | ConvertFrom-Json
if(-not $result.passed -or $result.version -ne '0.1.1' -or $result.signature -ne 'NotSigned'){throw 'Delivered normal package runtime failed'}
$result | ConvertTo-Json | Set-Content "$project/.local/s4/delivered-runtime-0.1.1.json" -Encoding UTF8
@{passed=$true;sourceCommit=$ExpectedCommit;tag='v0.1.1';installerHash=$expected['Diskweft-0.1.1-setup.exe'];portableHash=$expected['Diskweft-0.1.1-portable.zip'];manifestMatched=$true;independentlyDownloaded=$true;nativeAndCli=$true;signature='NotSigned';installerGuestEvidence='Same exact installer SHA-256 installed/uninstalled and normal WPF/CLI verified in owned guest6d2c71af-8721-4315-8a62-acbe6915e8b2'} | ConvertTo-Json | Set-Content "$project/.local/s4/delivery-verification-0.1.1.json" -Encoding UTF8
