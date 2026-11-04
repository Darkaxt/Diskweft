[CmdletBinding()]
param([string]$Root='D:/Temp/Diskweft-file-actions-resume-20261104/sandbox')
$ErrorActionPreference='Stop'
$project=Split-Path -Parent $PSScriptRoot
[IO.Directory]::CreateDirectory($Root) | Out-Null
# Retain shipped behavior; add only the test observer and shell-confirmation seam.
$baseline=(& git -C $project show 76dd613b514ef9ab32744609de32972407581560:App.cs) -join "`n"
if($LASTEXITCODE -ne 0){throw 'Cannot read the actual shipped baseline'}
$marker='        var uiOption = ask ? UIOption.OnlyErrorDialogs : UIOption.AllDialogs;'
if(-not $baseline.Contains($marker)){throw 'Baseline delete entry changed'}
$baseline=$baseline.Replace($marker,$marker+"`n        if (DeleteDecision != null) uiOption=UIOption.OnlyErrorDialogs;")
$marker='                if (t is Node) FileSystem.DeleteDirectory(PathOf(t), uiOption, recycle, UICancelOption.DoNothing);'
if(-not $baseline.Contains($marker)){throw 'Baseline shell call changed'}
$baseline=$baseline.Replace($marker,"                if (BeforeShellDelete != null) BeforeShellDelete();`n"+$marker)
[IO.File]::WriteAllText("$Root/BaselineApp.cs",$baseline,(New-Object Text.UTF8Encoding($false)))
& "$PSScriptRoot/Build-Development.ps1" -OutputDirectory "$Root/red" -Test -TestEntry Stage3Tests -AppSource "$Root/BaselineApp.cs"
& "$PSScriptRoot/Build-Development.ps1" -OutputDirectory "$Root/green" -Test -TestEntry Stage3Tests
Copy-Item "$PSScriptRoot/Verify-FileActionsGuest.ps1" "$Root/Verify-FileActionsGuest.ps1"
Get-FileHash "$Root/Verify-FileActionsGuest.ps1"
