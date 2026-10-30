[CmdletBinding()]
param([switch]$SkipInstaller, [string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $PSScriptRoot '.local\development' }
$buildRoot = [IO.Path]::GetFullPath($OutputDirectory)
& (Join-Path $PSScriptRoot 'scripts\Build-Development.ps1') -OutputDirectory $buildRoot
$exe = Join-Path $buildRoot 'Diskweft.exe'
$version = [Diagnostics.FileVersionInfo]::GetVersionInfo($exe).FileVersion
if (-not $version) { throw 'No Diskweft file version.' }
if ($SkipInstaller) { return }
$command = Get-Command ISCC.exe -ErrorAction SilentlyContinue
$candidates = @("$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe", "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe", "$env:ProgramFiles\Inno Setup 6\ISCC.exe")
$iscc = if ($command) { $command.Source } else { $candidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1 }
if (-not $iscc) { throw 'Inno Setup 6 compiler is unavailable; no installer was built.' }
& $iscc /Q "/DAppVersion=$version" "/DBuildRoot=$buildRoot" (Join-Path $PSScriptRoot 'installer\Diskweft.iss')
if ($LASTEXITCODE -ne 0) { throw 'Diskweft installer compilation failed.' }
