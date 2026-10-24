$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$task=Join-Path $root 'Builds/Missions/20261021-RestoreUplink'
$vp=Join-Path $root 'Builds/ValidationProject'
$backup=Join-Path $root 'Builds/MissionBackups/20261021-RestoreUplink'
[xml]$tests=Get-Content -Raw (Join-Path $task 'EditMode.xml')
if($tests.'test-run'.result -ne 'Passed' -or [int]$tests.'test-run'.total -lt 140){throw 'EditMode has not passed.'}
foreach($stage in @('Install','Build','EditMode','Idempotence')){
 if([int](Get-Content (Join-Path $task ($stage+'.exit'))) -ne 0){throw ('Stage failed: '+$stage)}
}
$counts=@{NetworkSmoke2=2;NetworkSmoke3=3;NetworkSmoke4=4;InteractionRegression=2;ReviveRegression=6;ReconRegression=2}
foreach($suite in $counts.Keys){
 $rows=@(Get-Content -Raw (Join-Path $task ($suite+'/Summary.json'))|ConvertFrom-Json)
 if($rows.Count -ne $counts[$suite] -or ($rows|Where-Object {-not $_.Passed})){throw ('Network suite failed: '+$suite)}
}
$preview=Get-Content -Raw (Join-Path $task 'GraphicalPreview/Checks.json')|ConvertFrom-Json
if(-not $preview.complete -or $preview.failures.Count -ne 0){throw 'Graphical checks failed.'}
if(-not (Get-Content -Raw (Join-Path $task 'PreservationValidation.json')|ConvertFrom-Json).passed){throw 'Scene preservation failed.'}
if(-not (Get-Content -Raw (Join-Path $task 'Idempotence.json')|ConvertFrom-Json).passed){throw 'Install is not idempotent.'}
foreach($old in Get-Content -Raw (Join-Path $backup 'BaselineHashes.json')|ConvertFrom-Json){
 if((Get-FileHash -LiteralPath (Join-Path $root $old.Path)).Hash -ne $old.Hash){throw ('Main asset changed during validation: '+$old.Path)}
}
$room='Assets/_Project/Prefabs/Networking/NetworkRoomState.prefab'
if((Get-FileHash (Join-Path $root $room)).Hash -ne (Get-FileHash (Join-Path $backup $room)).Hash){throw 'Main Room prefab changed during validation.'}
$owned=@($room,'Assets/_Project/Scenes/CanThoCampus/CanThoCampus.unity','Assets/_Project/Settings/CanThoCampusNavigation.asset','Assets/_Project/Prefabs/UI/CanThoOperationsMenu.prefab','Assets/_Project/ScriptableObjects/Missions/RestoreUplink.asset')
$metaCopies=@()
$codeHashes=@()
foreach($folder in @('Assets/_Project/Scripts','Assets/_Project/Tests')){
 foreach($file in Get-ChildItem -LiteralPath (Join-Path $root $folder) -Recurse -File -Filter '*.cs'){
  $p=$file.FullName.Substring($root.Length+1);$staged=Join-Path $vp $p
  if(-not(Test-Path -LiteralPath $staged) -or (Get-FileHash $file.FullName).Hash -ne (Get-FileHash $staged).Hash){throw ('Code differs from built copy: '+$p)}
  $codeHashes += [pscustomobject]@{Path=$p;SHA256=(Get-FileHash $file.FullName).Hash}
  if(-not(Test-Path -LiteralPath ($file.FullName+'.meta'))){$metaCopies+=($p+'.meta')}
  elseif((Get-Content -LiteralPath ($file.FullName+'.meta')|Select-String '^guid:').Line -ne (Get-Content -LiteralPath ($staged+'.meta')|Select-String '^guid:').Line){throw ('Code GUID mismatch: '+$p)}
 }
}
foreach($p in $owned){
 if(-not(Test-Path -LiteralPath (Join-Path $vp $p)) -or -not(Test-Path -LiteralPath (Join-Path $vp ($p+'.meta')))){throw ('Missing verified asset: '+$p)}
 if(Test-Path -LiteralPath (Join-Path $root ($p+'.meta'))){
  if((Select-String -LiteralPath (Join-Path $root ($p+'.meta')) -Pattern '^guid:').Line -ne (Select-String -LiteralPath (Join-Path $vp ($p+'.meta')) -Pattern '^guid:').Line){throw ('Asset GUID mismatch: '+$p)}
 }elseif(Test-Path -LiteralPath (Join-Path $root $p)){throw ('Existing asset has no metadata: '+$p)}
}
$doc='Assets/_Project/Documentation/RESTORE_UPLINK_SYSTEM.md.meta'
if(-not(Test-Path -LiteralPath (Join-Path $root $doc))){$metaCopies+=$doc}
foreach($p in $metaCopies){if(-not(Test-Path -LiteralPath (Join-Path $vp $p))){throw ('Missing generated metadata: '+$p)}}
# Mutate only after every verification and conflict guard has passed.
foreach($p in $metaCopies){Copy-Item -LiteralPath (Join-Path $vp $p) -Destination (Join-Path $root $p)}
foreach($p in $owned){
 Copy-Item -LiteralPath (Join-Path $vp $p) -Destination (Join-Path $root $p) -Force
 Copy-Item -LiteralPath (Join-Path $vp ($p+'.meta')) -Destination (Join-Path $root ($p+'.meta')) -Force
}
$codeHashes|ConvertTo-Json|Set-Content (Join-Path $task 'VerifiedCodeHashes.json')
$manifest=foreach($p in $owned){[pscustomobject]@{Path=$p;SHA256=(Get-FileHash -LiteralPath (Join-Path $root $p)).Hash}}
$manifest|ConvertTo-Json|Set-Content (Join-Path $task 'PromotedAssets.json')
'RESTORE_UPLINK_PROMOTION_PASS '+$manifest.Count
