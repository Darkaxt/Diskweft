[CmdletBinding()]
param([Parameter(Mandatory = $true)][string]$AssemblyPath)
$ErrorActionPreference = 'Stop'
$assembly = [Reflection.Assembly]::LoadFile((Resolve-Path -LiteralPath $AssemblyPath).Path)
$title = $assembly.GetCustomAttributes([Reflection.AssemblyTitleAttribute], $false)[0].Title
$failures = @()
if ($title -ne 'Diskweft') { $failures += "Product identity is '$title', expected Diskweft." }
if ($null -eq $assembly.GetType('ScanSession')) { $failures += 'The classic progressive scan-session contract is absent.' }
if ($failures.Count -gt 0) { $failures | Write-Output; exit 1 }
Write-Output 'Diskweft identity and progressive-session contract present.'
