param([switch]$AfterPromotion)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$clone=Join-Path $root 'Builds/ValidationProject'
$task=Join-Path $root 'Builds/CanThoMap/20261024-SketchfabLandmarks'
$protected=@()
foreach($folder in @('Prefabs','ScriptableObjects','Settings','Scenes')){
 foreach($file in Get-ChildItem (Join-Path $root ('Assets/_Project/'+$folder)) -File -Recurse){
  $path=$file.FullName.Substring($root.Length+1).Replace('\','/')
  if($path -in @('Assets/_Project/Scenes/CanThoCampus/CanThoCampus.unity','Assets/_Project/Settings/CanThoCampusNavigation.asset')){continue}
  if((Get-FileHash -LiteralPath $file.FullName).Hash -ne (Get-FileHash -LiteralPath (Join-Path $clone $path)).Hash){throw ('Protected asset differs: '+$path)}
  $protected+=$path
 }
}
foreach($path in @('Assets/DefaultNetworkPrefabs.asset','ProjectSettings/EditorBuildSettings.asset')){
 if((Get-FileHash (Join-Path $root $path)).Hash -ne (Get-FileHash (Join-Path $clone $path)).Hash){throw ('Protected settings differ: '+$path)};$protected+=$path
}
$runtime=Get-Content -Raw (Join-Path $task 'RuntimeCodeHashes.json')|ConvertFrom-Json
foreach($file in $runtime){
 foreach($base in @($root,$clone)){if((Get-FileHash -LiteralPath (Join-Path $base $file.Path)).Hash -ne $file.SHA256){throw ('Runtime source changed: '+$file.Path)}}
}
$owned=0
if($AfterPromotion){
 foreach($file in (Get-Content -Raw (Join-Path $task 'PromotedAssets.json')|ConvertFrom-Json)){
  if((Get-FileHash (Join-Path $root $file.Path)).Hash -ne $file.SHA256){throw ('Promoted asset changed: '+$file.Path)}
  if($file.MetaSHA256 -and (Get-FileHash (Join-Path $root ($file.Path+'.meta'))).Hash -ne $file.MetaSHA256){throw ('Promoted metadata changed: '+$file.Path)}
  $owned++
 }
}
$report=[pscustomobject]@{passed=$true;afterPromotion=[bool]$AfterPromotion;protectedAssets=$protected.Count;unchangedRuntimeSources=@($runtime).Count;promotedAssets=$owned;protocolVersion=28}
$name=if($AfterPromotion){'FinalVerification.json'}else{'ProtectedAssetsVerification.json'}
$report|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $task $name) -Encoding utf8
$report
