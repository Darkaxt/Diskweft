[CmdletBinding()]
param([Parameter(Mandatory=$true)][ValidatePattern('^[0-9a-f]{40}$')][string]$ExpectedCommit)
$ErrorActionPreference='Stop'
$project=Split-Path -Parent $PSScriptRoot
$root='E:\Temp\Diskweft-file-actions-resume-20261104\download'
$evidence=Join-Path $project '.local/maintenance-file-actions'
$metadata=(& gh release view v0.1.2 --repo Darkaxt/Diskweft --json tagName,targetCommitish,isDraft,isPrerelease) | ConvertFrom-Json
if($LASTEXITCODE -ne 0 -or $metadata.isDraft -or $metadata.isPrerelease -or $metadata.targetCommitish -ne $ExpectedCommit -or $metadata.tagName -ne 'v0.1.2'){throw 'Published source/state mismatch'}
$expected=@{'Diskweft-0.1.2-setup.exe'='7567776e9c33b634ed26a0c3e3e7ae50afdf6cc1c924f5dde9fb92896d28e336';'Diskweft-0.1.2-portable.zip'='03c8e4feb08978f3c4ed28d6a47165c1fb14280c86611b6fe9a18ec6b6c374cc'}
$lines=Get-Content "$root/SHA256SUMS.txt"
if($lines.Count -ne 2){throw 'Both delivered package checksums are required'}
foreach($line in $lines){$parts=$line -split '\s+',2;if(-not $expected.ContainsKey($parts[1]) -or $expected[$parts[1]] -ne $parts[0] -or (Get-FileHash "$root/$($parts[1])").Hash.ToLowerInvariant() -ne $parts[0]){throw 'Delivered checksum/hash mismatch'}}
$portable=Join-Path $root 'portable'
if(Test-Path $portable){throw 'Preserve the existing independent extraction'}
Expand-Archive -LiteralPath "$root/Diskweft-0.1.2-portable.zip" -DestinationPath $portable
if((Get-FileHash "$portable/Diskweft.exe").Hash -ne '599BE7AD3338D119AE89C7340CC9A876F79DF2657F472416F18F08BC378EA2F5'){throw 'Delivered executable differs from the qualified package'}
& "$project/tests/Verify-Package.ps1" -PackageRoot $portable -OutputRoot "$root/runtime" -ExpectedVersion '0.1.2'
$runtime=Get-Content "$root/runtime/result.json" -Raw | ConvertFrom-Json
if(-not $runtime.passed -or $runtime.signature -ne 'NotSigned'){throw 'Actual independently delivered package runtime failed'}
$runtime | ConvertTo-Json | Set-Content "$evidence/delivered-runtime-0.1.2.json" -Encoding UTF8
@{passed=$true;version='0.1.2';source=$ExpectedCommit;normalPublished=$true;independentlyDownloaded=$true;hashes=$expected;nativeAndCli=$true;signature='NotSigned'} | ConvertTo-Json -Depth 4 | Set-Content "$evidence/delivery-0.1.2.json" -Encoding UTF8
