$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$task=Join-Path $root 'Builds/CanThoMap/20261024-LandmarkRedraw'
$clone=Join-Path $root 'Builds/ValidationProject'
$backup=Join-Path $root 'Builds/CanThoBackups/20261024-LandmarkRedraw/PromotionReplaced'
foreach($name in @('SourcePreservation.json','SceneImport.json','ProtectedAssetsVerification.json','FinalCapture.json','VisualQA.json')){
 if(-not(Get-Content -Raw (Join-Path $task $name)|ConvertFrom-Json).passed){throw ('Failed '+$name)}
}
[xml]$tests=Get-Content -Raw (Join-Path $task 'EditMode.xml')
if($tests.'test-run'.result -ne 'Passed' -or [int]$tests.'test-run'.total -lt 187){throw 'Unity tests have not passed'}
foreach($stage in @('Import','EditMode','Build')){if([int](Get-Content (Join-Path $task ($stage+'.exit'))) -ne 0){throw ('Failed '+$stage)}}
foreach($row in (Get-Content -Raw (Join-Path $task 'BaselineHashes.json')|ConvertFrom-Json)){
 if((Get-FileHash -LiteralPath (Join-Path $root $row.Path)).Hash -ne $row.SHA256){throw ('User asset changed: '+$row.Path)}
}
function Replace-Tested([string]$src,[string]$relative){
 $dst=Join-Path $root $relative
 $saved=Join-Path $backup $relative
 New-Item -ItemType Directory -Path (Split-Path $saved) -Force|Out-Null
 if(Test-Path -LiteralPath $dst){
  $staged=$dst+'.redraw-staged';Copy-Item -LiteralPath $src -Destination $staged -Force
  [IO.File]::Replace($staged,$dst,$saved)
 }else{Copy-Item -LiteralPath $src -Destination $dst}
}
$owned=@('Assets/_Project/Art/Environments/CanThoCampus/CanThoCampus.fbx','Assets/_Project/Art/Environments/CanThoCampus/CanThoCampus.layout.json','Assets/_Project/Scenes/CanThoCampus/CanThoCampus.unity')
foreach($p in $owned){
 if((Get-FileHash (Join-Path $root ($p+'.meta'))).Hash -ne (Get-FileHash (Join-Path $clone ($p+'.meta'))).Hash){throw ('Metadata differs: '+$p)}
}
foreach($row in (Get-Content -Raw (Join-Path $task 'TestedAssetHashes.json')|ConvertFrom-Json)){
 if((Get-FileHash -LiteralPath (Join-Path $clone $row.Path)).Hash -ne $row.SHA256){throw ('Tested file changed: '+$row.Path)}
}
foreach($p in $owned){Replace-Tested (Join-Path $clone $p) $p}
foreach($name in @('CanThoCampus.blend','CanThoCampus.layout.json','LandmarkRedraw.json')){
 $p='ArtSource/Blender/CanThoCampus/'+$name
 Replace-Tested (Join-Path $task ('Candidate01/'+$name)) $p;$owned+=$p
}
$manifest=foreach($p in $owned){[pscustomobject]@{Path=$p;SHA256=(Get-FileHash -LiteralPath (Join-Path $root $p)).Hash;MetaSHA256=if(Test-Path -LiteralPath (Join-Path $root ($p+'.meta'))){(Get-FileHash -LiteralPath (Join-Path $root ($p+'.meta'))).Hash}else{$null}}}
$manifest|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $task 'PromotedAssets.json') -Encoding utf8
'LANDMARK_REDRAW_PROMOTION_PASS '+$manifest.Count
