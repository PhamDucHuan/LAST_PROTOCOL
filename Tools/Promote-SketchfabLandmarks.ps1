$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$task=Join-Path $root 'Builds/CanThoMap/20261024-SketchfabLandmarks'
$clone=Join-Path $root 'Builds/ValidationProject'
$candidate=Join-Path $task 'Candidate04'
$backup=Join-Path $root 'Builds/CanThoBackups/20261024-SketchfabLandmarks'
foreach($report in @('SourcePreservation.json','SceneImport.json','FinalCapture.json','VisualQA.json','DownloadValidation.json','ProtectedAssetsVerification.json')){if(-not(Get-Content -Raw (Join-Path $task $report)|ConvertFrom-Json).passed){throw ('Failed '+$report)}}
[xml]$tests=Get-Content -Raw (Join-Path $task 'EditMode.xml')
if($tests.'test-run'.result -ne 'Passed' -or [int]$tests.'test-run'.total -lt 187){throw 'Unity checks have not passed'}
foreach($stage in @('Import','EditMode','Build')){if([int](Get-Content (Join-Path $task ($stage+'.exit'))) -ne 0){throw ('Failed '+$stage)}}
$network=Get-Content -Raw (Join-Path $task 'Multiplayer2-Final/Summary.json')|ConvertFrom-Json
if(@($network).Count -ne 2 -or @($network|Where-Object {-not $_.Passed}).Count){throw 'Two-player validation failed'}
foreach($tested in (Get-Content -Raw (Join-Path $task 'TestedAssetHashes.json')|ConvertFrom-Json)){
 if((Get-FileHash -LiteralPath (Join-Path $clone $tested.Path)).Hash -ne $tested.SHA256){throw ('Tested asset changed: '+$tested.Path)}
}
foreach($name in @('HoiTruongRua','NhaHocA3','NhaThiDauTDTT')){if((Get-Item (Join-Path $task ($name+'.png'))).Length -lt 20000){throw ('Missing preview '+$name)}}
foreach($old in (Get-Content -Raw (Join-Path $backup 'OwnedHashes.json')|ConvertFrom-Json)){
 if((Get-FileHash -LiteralPath (Join-Path $root $old.Path)).Hash -ne $old.SHA256){throw ('Current user asset changed: '+$old.Path)}
}
$owned=@('Assets/_Project/Art/Environments/CanThoCampus/CanThoCampus.fbx','Assets/_Project/Art/Environments/CanThoCampus/CanThoCampus.layout.json','Assets/_Project/Scenes/CanThoCampus/CanThoCampus.unity','Assets/_Project/Settings/CanThoCampusNavigation.asset')
foreach($folder in @('Assets/_Project/Art/Environments/CanThoCampus/Textures','Assets/_Project/Materials/CanThoCampus')){
 foreach($file in Get-ChildItem -LiteralPath (Join-Path $root $folder) -File){
  if($file.Name.StartsWith('CT12_')){continue}
  $relative=$file.FullName.Substring($root.Length+1)
  if((Get-FileHash $file.FullName).Hash -ne (Get-FileHash (Join-Path $clone $relative)).Hash){throw ('Existing surface changed: '+$relative)}
 }
 $owned+=Get-ChildItem (Join-Path $clone $folder) -File -Filter 'CT12_*'|Where-Object Extension -ne '.meta'|ForEach-Object{$_.FullName.Substring($clone.Length+1)}
}
foreach($p in $owned){
 $src=Join-Path $clone $p;$dst=Join-Path $root $p
 if(Test-Path -LiteralPath ($dst+'.meta')){
  if((Select-String -LiteralPath ($src+'.meta') -Pattern '^guid:').Line -ne (Select-String -LiteralPath ($dst+'.meta') -Pattern '^guid:').Line){throw ('GUID mismatch: '+$p)}
 }
}
$owned=@($owned|Sort-Object {if($_ -match '/Textures/'){0}elseif($_ -match '/Materials/'){1}elseif($_ -match '\.fbx$'){3}elseif($_ -match '\.fbx\.meta$'){4}else{2}})
foreach($name in @('CanThoCampus.fbx','CanThoCampus.layout.json')){
 if((Get-FileHash (Join-Path $candidate $name)).Hash -ne (Get-FileHash (Join-Path $clone ('Assets/_Project/Art/Environments/CanThoCampus/'+$name))).Hash){throw 'Candidate differs from tested clone'}
}
function Replace-TestedFile([string]$src,[string]$dst,[string]$relative){
 if(Test-Path -LiteralPath $dst){
  $stage=$dst+'.landmark-staged';Copy-Item -LiteralPath $src -Destination $stage -Force
  $saved=Join-Path $backup ('PromotionReplaced/'+$relative);New-Item -ItemType Directory -Path (Split-Path $saved) -Force|Out-Null
  [IO.File]::Replace($stage,$dst,$saved)
 }else{Copy-Item -LiteralPath $src -Destination $dst}
}
foreach($p in $owned){
 Replace-TestedFile (Join-Path $clone $p) (Join-Path $root $p) $p
 if(Test-Path -LiteralPath (Join-Path $clone ($p+'.meta'))){Replace-TestedFile (Join-Path $clone ($p+'.meta')) (Join-Path $root ($p+'.meta')) ($p+'.meta')}
}
foreach($name in @('CanThoCampus.blend','CanThoCampus.layout.json','SketchfabIntegration.json')){
 $p='ArtSource/Blender/CanThoCampus/'+$name;Replace-TestedFile (Join-Path $candidate $name) (Join-Path $root $p) $p;$owned+=$p
}
Get-ChildItem (Join-Path $candidate 'Textures') -Filter 'CT12_*' -File|ForEach-Object{Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $root ('ArtSource/Blender/CanThoCampus/Textures/'+$_.Name))}
$manifest=foreach($p in $owned){[pscustomobject]@{Path=$p;SHA256=(Get-FileHash -LiteralPath (Join-Path $root $p)).Hash;MetaSHA256=if(Test-Path -LiteralPath (Join-Path $root ($p+'.meta'))){(Get-FileHash -LiteralPath (Join-Path $root ($p+'.meta'))).Hash}else{$null}}}
$manifest|ConvertTo-Json -Depth 4|Set-Content -LiteralPath (Join-Path $task 'PromotedAssets.json') -Encoding utf8
'SKETCHFAB_LANDMARK_PROMOTION_PASS '+$manifest.Count
