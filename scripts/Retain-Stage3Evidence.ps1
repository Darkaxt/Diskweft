[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$destination = Join-Path $root '.local/s3'
[IO.Directory]::CreateDirectory($destination) | Out-Null
$native = 'D:/Temp/Diskweft-s3-build/sandbox/results-f6b4d9388d5a4452a200ea4a6f1eee9e'
$normal = 'E:/Temp/Diskweft-s3-20261104/normal-7730674505774c8f907f996c00126ca2'
foreach ($name in @('result.json','normal-build.json')) {
    if ($name -eq 'result.json') { Copy-Item "$native/result.json" "$destination/native-result.json" }
    else { Copy-Item "$normal/result.json" "$destination/$name" }
}
foreach ($stem in @('dark-1280-100','light-1280-100','dark-1600-200','light-1280-150','dark-960-200','keyboard-focus','dark-settings','light-settings','high-contrast-live','high-contrast-settings','system-change-live','dark-stopped','dark-complete','empty-complete','denied-incomplete','settings-minimum-bottom')) {
    Copy-Item "$native/$stem.json" $destination
    Copy-Item "$native/$stem.png" $destination
}
Copy-Item 'D:/Temp/Diskweft-s3-build/sandbox/classic-ui-1e00a303a74d476296d82925376abc25/result.json' "$destination/classic-ui-result.json"
Copy-Item 'D:/Temp/Diskweft-s3-build/sandbox/dpi-206028334c58471ea57e18c952af3129/result.json' "$destination/dpi-result.json"
Copy-Item 'E:/Temp/Diskweft-s3-20261104/actual-system-source/result.json' "$destination/actual-system-source.json"
Copy-Item 'D:/Temp/Diskweft-s3-build/normal/Diskweft.exe' "$root/.local/development/Diskweft.exe"
Copy-Item 'D:/Temp/Diskweft-s3-build/normal/Diskweft.exe.config' "$root/.local/development/Diskweft.exe.config"
$expected = (Get-Content "$normal/result.json" -Raw | ConvertFrom-Json).normalSha256
if ((Get-FileHash "$root/.local/development/Diskweft.exe").Hash -ne $expected) { throw 'Retained binary mismatch' }
Get-ChildItem $root -File | Where-Object { $_.Extension -in '.cs','.xaml','.manifest','.config' } | Get-FileHash | Select-Object Path,Hash | ConvertTo-Json | Set-Content "$destination/source-hashes.json" -Encoding UTF8
foreach ($name in @('native-result','normal-build','classic-ui-result','dpi-result','actual-system-source')) {
    if (-not (Get-Content "$destination/$name.json" -Raw | ConvertFrom-Json).passed) { throw "Failed evidence: $name" }
}
Write-Output "Retained verified S3 evidence and development binary $expected"
