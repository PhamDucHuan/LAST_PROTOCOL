$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$task=Join-Path $root 'Builds/CanThoMap/20261021-RoadClearance'
$backup=Join-Path $root 'Builds/CanThoBackups/20261021-RoadClearance'
$validation=Join-Path $root 'Builds/ValidationProject'
$candidate=Join-Path $task 'Candidate02'
foreach($p in @('Candidate02/RoadTreeRepair.json','SourcePreservationValidation.json','LayoutPreservationValidation.json','ScenePreservationValidation.json','VisualReview.json')){
 if(-not (Get-Content -Raw -LiteralPath (Join-Path $task $p)|ConvertFrom-Json).passed){throw ('Validation failed: '+$p)}
}
[xml]$tests=Get-Content -Raw -LiteralPath (Join-Path $task 'EditModeFinal.xml')
if($tests.'test-run'.result -ne 'Passed' -or [int]$tests.'test-run'.total -lt 133){throw 'Unity tests have not passed.'}
foreach($stage in @('RefreshFinal','EditModeFinal','CaptureFinal')){
 if([int](Get-Content (Join-Path $task ($stage+'-ExitCode.txt'))) -ne 0){throw ($stage+' failed.')}
}
foreach($name in @('CanThoCampus.fbx','CanThoCampus.layout.json')){
 if((Get-FileHash (Join-Path $candidate $name)).Hash -ne (Get-FileHash (Join-Path $validation ('Assets/_Project/Art/Environments/CanThoCampus/'+$name))).Hash){throw ('Candidate differs from tested art: '+$name)}
}
foreach($old in (Get-Content -Raw -LiteralPath (Join-Path $backup 'OwnedHashes.json')|ConvertFrom-Json)){
 if((Get-FileHash -LiteralPath (Join-Path $root $old.Path)).Hash -ne $old.Hash){throw ('User asset changed since backup: '+$old.Path)}
}
$owned=@('Assets/_Project/Art/Environments/CanThoCampus/CanThoCampus.fbx','Assets/_Project/Art/Environments/CanThoCampus/CanThoCampus.layout.json','Assets/_Project/Scenes/CanThoCampus/CanThoCampus.unity','Assets/_Project/Settings/CanThoCampusNavigation.asset','Assets/_Project/Prefabs/UI/CanThoOperationsMenu.prefab')
foreach($p in $owned){
 $dst=Join-Path $root $p;$src=Join-Path $validation $p
 if((Select-String -LiteralPath ($dst+'.meta') -Pattern '^guid:').Line -ne (Select-String -LiteralPath ($src+'.meta') -Pattern '^guid:').Line){throw ('GUID mismatch: '+$p)}
}
foreach($folder in @('Assets/_Project/Art/Environments/CanThoCampus/Textures','Assets/_Project/Materials/CanThoCampus')){
 foreach($file in Get-ChildItem -LiteralPath (Join-Path $root $folder) -File -Recurse){
  $p=$file.FullName.Substring($root.Length+1)
  if((Get-FileHash -LiteralPath $file.FullName).Hash -ne (Get-FileHash -LiteralPath (Join-Path $validation $p)).Hash){throw ('Unexpected surface change: '+$p)}
 }
}
foreach($p in $owned){
 Copy-Item -LiteralPath (Join-Path $validation $p) -Destination (Join-Path $root $p) -Force
 Copy-Item -LiteralPath (Join-Path $validation ($p+'.meta')) -Destination (Join-Path $root ($p+'.meta')) -Force
}
foreach($name in @('CanThoCampus.blend','CanThoCampus.layout.json')){
 $p='ArtSource/Blender/CanThoCampus/'+$name
 Copy-Item -LiteralPath (Join-Path $candidate $name) -Destination (Join-Path $root $p) -Force
 $owned+=$p
}
Copy-Item -LiteralPath (Join-Path $candidate 'RoadTreeRepair.json') -Destination (Join-Path $root 'ArtSource/Blender/CanThoCampus/RoadTreeRepair.json') -Force
$tool='Assets/_Project/Scripts/Editor/CanThoRoadClearanceTool.cs'
if(-not(Test-Path -LiteralPath (Join-Path $root ($tool+'.meta')))){
 Copy-Item -LiteralPath (Join-Path $validation ($tool+'.meta')) -Destination (Join-Path $root ($tool+'.meta'))
}
$manifest=foreach($p in $owned){[pscustomobject]@{Path=$p;SHA256=(Get-FileHash -LiteralPath (Join-Path $root $p)).Hash}}
$manifest|ConvertTo-Json -Depth 4|Set-Content -LiteralPath (Join-Path $task 'PromotedAssets.json')
'CANTHO_ROAD_CLEARANCE_PROMOTION_PASS '+$manifest.Count
