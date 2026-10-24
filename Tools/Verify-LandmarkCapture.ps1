$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$task=Join-Path $root 'Builds/CanThoMap/20261024-SketchfabLandmarks'
$code=[int](Get-Content (Join-Path $task 'Build.exit'))
$log=Get-Content -Raw -LiteralPath (Join-Path $task 'Build.log')
if($code -ne 0 -or $log -notmatch 'SKETCHFAB_LANDMARK_CAPTURE_PASS' -or $log -notmatch 'SKETCHFAB_LANDMARK_BUILD_PASS'){throw 'Final combined capture/build did not complete cleanly'}
$images=foreach($name in @('HoiTruongRua','NhaHocA3','NhaThiDauTDTT','HoiTruongRua-Plot','NhaHocA3-Plot','NhaThiDauTDTT-Plot')){
 $file=Get-Item -LiteralPath (Join-Path $task ($name+'.png'));if($file.Length -lt 20000){throw ('Empty capture '+$name)}
 [pscustomobject]@{Name=$file.Name;SHA256=(Get-FileHash $file.FullName).Hash;Bytes=$file.Length}
}
[pscustomobject]@{passed=$true;sourceStage='Build';exitCode=$code;log='Build.log';images=$images}|ConvertTo-Json -Depth 5|Set-Content -LiteralPath (Join-Path $task 'FinalCapture.json') -Encoding utf8
'LANDMARK_FINAL_CAPTURE_PASS'
