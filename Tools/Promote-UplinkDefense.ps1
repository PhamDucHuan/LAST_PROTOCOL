$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$task=Join-Path $root 'Builds/Encounters/20261021-UplinkDefense'
$vp=Join-Path $root 'Builds/ValidationProject'
$backup=Join-Path $root 'Builds/EncounterBackups/20261021-UplinkDefense'
[xml]$tests=Get-Content -Raw (Join-Path $task 'EditMode.xml')
if($tests.'test-run'.result -ne 'Passed' -or [int]$tests.'test-run'.total -lt 145){throw 'EditMode has not passed.'}
foreach($stage in @('Install','Build','EditMode','Idempotence')){if([int](Get-Content (Join-Path $task ($stage+'.exit'))) -ne 0){throw ('Stage failed: '+$stage)}}
$counts=@{'NetworkSmoke2-Verified'=2;NetworkSmoke4=4;MissionRegression=2;EnemyRegression=6}
foreach($suite in $counts.Keys){
 $rows=@(Get-Content -Raw (Join-Path $task ($suite+'/Summary.json'))|ConvertFrom-Json)
 if($rows.Count -ne $counts[$suite] -or ($rows|Where-Object {-not $_.Passed})){throw ('Network suite failed: '+$suite)}
}
$preview=Get-Content -Raw (Join-Path $task 'GraphicalPreview/Checks.json')|ConvertFrom-Json
if(-not $preview.complete -or $preview.failures.Count -ne 0){throw 'Graphical checks failed.'}
foreach($check in @('PreservationValidation','Idempotence')){if(-not (Get-Content -Raw (Join-Path $task ($check+'.json'))|ConvertFrom-Json).passed){throw ($check+' failed.')}}
foreach($old in Get-Content -Raw (Join-Path $backup 'BaselineHashes.json')|ConvertFrom-Json){
 if((Get-FileHash -LiteralPath (Join-Path $root $old.Path)).Hash -ne $old.Hash){throw ('Main asset changed during validation: '+$old.Path)}
}
$list='Assets/_Project/Settings/MvpNetworkPrefabs.asset'
if((Get-FileHash (Join-Path $root $list)).Hash -ne (Get-FileHash (Join-Path $backup $list)).Hash){throw 'Main prefab registration changed.'}
$owned=@('Assets/_Project/Prefabs/Networking/NetworkRoomState.prefab',$list,'Assets/_Project/Prefabs/UI/CanThoOperationsMenu.prefab','Assets/_Project/Prefabs/Networking/UplinkPatrol.prefab','Assets/_Project/ScriptableObjects/Missions/UplinkDefense.asset')
$metaCopies=@();$codeHashes=@();$newGuidAlignments=@()
foreach($folder in @('Assets/_Project/Scripts','Assets/_Project/Tests')){
 foreach($file in Get-ChildItem -LiteralPath (Join-Path $root $folder) -Recurse -File -Filter '*.cs'){
  $path=$file.FullName.Substring($root.Length+1);$source=Join-Path $vp $path
  if(-not(Test-Path -LiteralPath $source) -or (Get-FileHash $file.FullName).Hash -ne (Get-FileHash $source).Hash){throw ('Code differs from built copy: '+$path)}
  $codeHashes += [pscustomobject]@{Path=$path;SHA256=(Get-FileHash $source).Hash}
  if(-not(Test-Path -LiteralPath ($file.FullName+'.meta'))){$metaCopies+=($path+'.meta')}
  elseif((Select-String -LiteralPath ($file.FullName+'.meta') '^guid:').Line -ne (Select-String -LiteralPath ($source+'.meta') '^guid:').Line){
   if(Test-Path -LiteralPath (Join-Path $backup $path)){throw ('Existing script GUID mismatch: '+$path)}
   $metaCopies+=($path+'.meta')
   $newGuidAlignments += [pscustomobject]@{Path=$path;GeneratedMainGuid=(Select-String -LiteralPath ($file.FullName+'.meta') '^guid:').Line;VerifiedGuid=(Select-String -LiteralPath ($source+'.meta') '^guid:').Line}
  }
 }
}
foreach($path in $owned){
 if(-not(Test-Path -LiteralPath (Join-Path $vp $path)) -or -not(Test-Path -LiteralPath (Join-Path $vp ($path+'.meta')))){throw ('Verified asset missing: '+$path)}
 if(Test-Path -LiteralPath (Join-Path $root ($path+'.meta'))){
  if((Select-String -LiteralPath (Join-Path $root ($path+'.meta')) '^guid:').Line -ne (Select-String -LiteralPath (Join-Path $vp ($path+'.meta')) '^guid:').Line){throw ('Asset GUID mismatch: '+$path)}
 }elseif(Test-Path -LiteralPath (Join-Path $root $path)){throw ('Existing asset without metadata: '+$path)}
}
$doc='Assets/_Project/Documentation/UPLINK_DEFENSE.md.meta'
if(-not(Test-Path -LiteralPath (Join-Path $root $doc))){$metaCopies+=$doc}
foreach($path in $metaCopies){if(-not(Test-Path -LiteralPath (Join-Path $vp $path))){throw ('Generated metadata missing: '+$path)}}
# Every preflight completes before any main asset is replaced.
foreach($alignment in $newGuidAlignments){
 $metaPath=$alignment.Path+'.meta';$save=Join-Path $backup ('GeneratedScriptMetas/'+$metaPath)
 New-Item -ItemType Directory -Path (Split-Path $save) -Force|Out-Null
 Copy-Item -LiteralPath (Join-Path $root $metaPath) -Destination $save
}
foreach($path in $metaCopies){Copy-Item -LiteralPath (Join-Path $vp $path) -Destination (Join-Path $root $path) -Force}
foreach($path in $owned){
 Copy-Item -LiteralPath (Join-Path $vp $path) -Destination (Join-Path $root $path) -Force
 Copy-Item -LiteralPath (Join-Path $vp ($path+'.meta')) -Destination (Join-Path $root ($path+'.meta')) -Force
}
$codeHashes|ConvertTo-Json|Set-Content (Join-Path $task 'VerifiedCodeHashes.json')
$manifest=foreach($path in $owned){[pscustomobject]@{Path=$path;SHA256=(Get-FileHash -LiteralPath (Join-Path $root $path)).Hash}}
$manifest|ConvertTo-Json|Set-Content (Join-Path $task 'PromotedAssets.json')
$newGuidAlignments|ConvertTo-Json|Set-Content (Join-Path $task 'NewScriptGuidAlignments.json')
'UPLINK_DEFENCE_PROMOTION_PASS '+$manifest.Count
