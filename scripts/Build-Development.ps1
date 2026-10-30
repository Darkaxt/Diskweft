[CmdletBinding()]
param(
    [string]$OutputDirectory,
    [switch]$Diagnostics,
    [switch]$Test
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $projectRoot '.local\development' }
$outputRoot = [IO.Path]::GetFullPath($OutputDirectory)
[IO.Directory]::CreateDirectory($outputRoot) | Out-Null
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { throw 'The existing .NET Framework compiler is unavailable.' }
$frameworkRoot = Split-Path -Parent $compiler
$target = if ($Test) { 'exe' } else { 'winexe' }
$name = if ($Test) { 'Diskweft.Tests.exe' } else { 'Diskweft.exe' }
$arguments = @('/nologo', "/target:$target", '/optimize', '/platform:anycpu', '/codepage:65001',
    "/lib:$frameworkRoot\WPF", '/r:PresentationFramework.dll', '/r:PresentationCore.dll', '/r:WindowsBase.dll',
    '/r:System.Xaml.dll', '/r:System.Windows.Forms.dll', '/r:Microsoft.VisualBasic.dll', '/r:System.Web.Extensions.dll',
    "/win32manifest:$projectRoot\app.manifest", "/win32icon:$projectRoot\Diskweft.ico",
    "/resource:$projectRoot\MainWindow.xaml,MainWindow.xaml", "/resource:$projectRoot\whatsnew.txt,whatsnew.txt",
    "/resource:$projectRoot\WhatsNew\1.4.jpg,WhatsNew.1.4.jpg", "/resource:$projectRoot\WhatsNew\m09.jpg,WhatsNew.m09.jpg",
    "/resource:$projectRoot\WhatsNew\m10.jpg,WhatsNew.m10.jpg", "/out:$outputRoot\$name",
    "$projectRoot\SpaceScan.cs", "$projectRoot\LiveScan.cs", "$projectRoot\ClassicUI.cs", "$projectRoot\Diagnostics.cs", "$projectRoot\App.cs", "$projectRoot\WhatsNew.cs")
if ($Diagnostics -or $Test) { $arguments += '/define:DISKWEFT_TEST' }
if ($Test) { $arguments += '/main:Stage1Tests'; $arguments += "$projectRoot\tests\Stage1Tests.cs" }
& $compiler @arguments
if ($LASTEXITCODE -ne 0) { throw 'Diskweft compilation failed.' }
Write-Output (Join-Path $outputRoot $name)
