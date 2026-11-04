[CmdletBinding()]
param([ValidateSet('Red','Green')][string]$Mode='Green',[ValidateSet('all','recycle','clipboard','error','finalize')][string]$Only='all')
$ErrorActionPreference='Stop'
if($env:COMPUTERNAME -eq 'BEACON'){throw 'Disposable guest only'}
$root='C:\Coordinator\Work'
trap { [IO.File]::WriteAllText("$root/last-file-actions-error.txt",$_.Exception.ToString());exit 1 }
$env:DISKWEFT_SANDBOX='1'
$payload=Join-Path $root $Mode.ToLowerInvariant()
$results=Join-Path $root ('results\'+$Mode.ToLowerInvariant())
[IO.Directory]::CreateDirectory($results) | Out-Null
$env:TEMP=$results;$env:TMP=$results
$tests=if($Mode -eq 'Red'){@('recycle','clipboard')}else{@('recycle','permanent','close','clipboard','error')}
if($Only -eq 'finalize'){
 foreach($completed in $tests){if(-not (Get-Content "$results/$completed/result.json" -Raw | ConvertFrom-Json).passed){throw 'A required existing native workflow did not pass'}}
 $tests=@()
} elseif($Only -ne 'all'){$tests=@($Only)}
foreach($test in $tests) {
 $output=Join-Path $results $test
 $arguments=@($output,'--file-actions')
 if($test -eq 'clipboard'){$arguments+='--clipboard'}
 if($test -eq 'close'){$arguments+='--close'}
 if($test -eq 'permanent'){$arguments+='--permanent'}
 if($test -eq 'error'){$arguments+='--error'}
 if($Mode -eq 'Red'){$arguments+='--baseline'}
 $process=Start-Process -FilePath "$payload\Diskweft.Tests.exe" -ArgumentList $arguments -WindowStyle Hidden -Wait -PassThru -RedirectStandardOutput "$results/$test.log" -RedirectStandardError "$results/$test-error.log"
 $code=$process.ExitCode
 if($Mode -eq 'Red') {
  if($code -eq 0){throw 'Regression did not reproduce against original behavior'}
  $log=(Get-Content "$output/result.json" -Raw)+(Get-Content "$results/$test-error.log" -Raw)
  if($test -eq 'recycle' -and -not $log.Contains('Recycle shell work blocked the WPF dispatcher')){throw 'Unexpected recycle reproduction failure'}
  if($test -eq 'clipboard' -and -not $log.Contains('0x800401D0')){throw 'Unexpected clipboard reproduction failure'}
 } elseif($code -ne 0){throw "Native $test regression failed: $code"}
}
if($Mode -eq 'Green' -and $Only -in @('all','clipboard','finalize')) {
 Add-Type -AssemblyName PresentationCore
 $expected=[IO.File]::ReadAllText("$results/clipboard/clipboard-expected.txt")
 if([Windows.Clipboard]::GetText() -ne $expected){throw 'Unicode Copy path did not survive producer exit'}
}
if($Mode -eq 'Green' -and $Only -in @('all','finalize')) {
 foreach($regression in @('live-sort','release-notes','classic-ui')) {
  & "$payload\Diskweft.Tests.exe" (Join-Path $results $regression) ('--'+$regression) 2>&1 | Out-File "$results/$regression.log" -Encoding utf8
  if($LASTEXITCODE -ne 0){throw "Affected native regression failed: $regression"}
 }
}
@{passed=$true;mode=$Mode;scope=$Only;executed=$tests;clipboardPersistsAfterExit=($Mode -eq 'Green' -and $Only -in @('all','clipboard','finalize'))} | ConvertTo-Json | Set-Content "$results/summary-$Only.json" -Encoding utf8
