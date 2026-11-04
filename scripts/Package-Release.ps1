[CmdletBinding()]
param([string]$BuildRoot='D:/Temp/Diskweft-file-actions-resume-20261104/release',[string]$OutputDirectory='D:/Projects/Diskweft/dist/0.1.2')
$ErrorActionPreference='Stop'
$projectRoot=Split-Path -Parent $PSScriptRoot
$output=[IO.Path]::GetFullPath($OutputDirectory)
[IO.Directory]::CreateDirectory($output) | Out-Null
& "$projectRoot/build.ps1" -OutputDirectory $BuildRoot -InstallerOutputDirectory $output
$version=[Diagnostics.FileVersionInfo]::GetVersionInfo("$BuildRoot/Diskweft.exe").FileVersion
if($version -ne '0.1.2'){throw 'Release metadata must be 0.1.2'}
Copy-Item "$projectRoot/LICENSE" $BuildRoot
Copy-Item "$projectRoot/Diskweft.ico" $BuildRoot
Copy-Item "$projectRoot/docs/PORTABLE.md" "$BuildRoot/README.txt"
$zip=Join-Path $output "Diskweft-$version-portable.zip"
if(Test-Path $zip){throw 'Versioned package already exists; inspect it before replacing'}
Compress-Archive -LiteralPath @("$BuildRoot/Diskweft.exe","$BuildRoot/Diskweft.exe.config","$BuildRoot/LICENSE","$BuildRoot/Diskweft.ico","$BuildRoot/README.txt") -DestinationPath $zip -CompressionLevel Optimal
$assets=@("Diskweft-$version-setup.exe","Diskweft-$version-portable.zip")
$lines=foreach($asset in $assets){(Get-FileHash "$output/$asset").Hash.ToLowerInvariant()+'  '+$asset}
[IO.File]::WriteAllLines("$output/SHA256SUMS.txt",$lines,(New-Object Text.UTF8Encoding($false)))
[ordered]@{version=$version;exeSha256=(Get-FileHash "$BuildRoot/Diskweft.exe").Hash;signature=(Get-AuthenticodeSignature "$BuildRoot/Diskweft.exe").Status.ToString();compiler='D:/Tools/InnoSetup/6.7.4/ISCC.exe';assets=$lines} | ConvertTo-Json | Set-Content "$projectRoot/.local/maintenance-file-actions/package-build.json" -Encoding UTF8
Get-Content "$output/SHA256SUMS.txt"
