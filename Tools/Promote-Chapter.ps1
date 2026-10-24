$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$vp=Join-Path $root 'Builds/ValidationProject'
$task=Join-Path $root 'Builds/Pacing/20261022-Map1'
$backup=Join-Path $root 'Builds/PacingBackups/20261022-Map1'
$owned=@('Assets/_Project/Prefabs/Networking/NetworkRoomState.prefab','Assets/_Project/Prefabs/UI/CanThoOperationsMenu.prefab','Assets/_Project/Settings/MvpNetworkPrefabs.asset','Assets/_Project/ScriptableObjects/Missions/CampusRescue.asset','Assets/_Project/Prefabs/Networking/ChapterTerminal.prefab')
[xml]$tests=Get-Content -Raw -LiteralPath (Join-Path $task 'EditMode.xml')
if($tests.'test-run'.result -ne 'Passed' -or [int]$tests.'test-run'.total -lt 185){throw 'EditMode not passed'}
foreach($stage in @('Install','Build','EditMode','Idempotence')){if([int](Get-Content -LiteralPath (Join-Path $task ($stage+'.exit'))) -ne 0){throw ('Stage failed: '+$stage)}}
foreach($report in @('PreservationValidation','Idempotence','MenuOnlyChangeValidation')){if(-not(Get-Content -Raw -LiteralPath (Join-Path $task ($report+'.json'))|ConvertFrom-Json).passed){throw ($report+' failed')}}
$suites=@{'Chapter2-Final'=2;'Chapter4-Final'=4;'AIRegression2'=2;'CheckpointRegression2'=2}
foreach($suite in $suites.Keys){
 $rows=@(Get-Content -Raw -LiteralPath (Join-Path $task ($suite+'/Summary.json'))|ConvertFrom-Json)
 if($rows.Count -ne $suites[$suite] -or ($rows|Where-Object{-not $_.Passed})){throw ('Suite failed: '+$suite)}
 if(Get-ChildItem -LiteralPath (Join-Path $task $suite) -Filter '*.log'|Select-String -Pattern 'NullReferenceException|MissingReferenceException|InvalidOperationException|IndexOutOfRangeException|KeyNotFoundException|Failed to create agent|\[Error\]'){throw ('Application error in '+$suite)}
}
foreach($name in @('Chapter-Stage-12','Chapter-Stage-15')){
 $check=Get-Content -Raw -LiteralPath (Join-Path $task ('Chapter2-Final/'+$name+'-Checks.json'))|ConvertFrom-Json
 if(-not $check.complete -or $check.failures.Count -ne 0 -or $check.passed.Count -lt 4){throw ('Graphical check failed: '+$name)}
}
$menu=Get-Content -Raw -LiteralPath (Join-Path $task 'MenuPreview/Checks.json')|ConvertFrom-Json
if(-not $menu.complete -or $menu.failures.Count -ne 0 -or $menu.passed.Count -lt 40){throw 'Menu graphical validation failed'}
if(Get-ChildItem -LiteralPath (Join-Path $task 'MenuPreview') -Filter '*.log'|Select-String -Pattern 'NullReferenceException|MissingReferenceException|InvalidOperationException|IndexOutOfRangeException|\[Error\]'){throw 'Menu application error'}
foreach($row in Get-Content -Raw -LiteralPath (Join-Path $backup 'BaselineHashes.json')|ConvertFrom-Json){
 $hash=(Get-FileHash -LiteralPath (Join-Path $root $row.Path)).Hash
 if($hash -ne $row.Hash -and -not($row.Path -in $owned -and $hash -eq (Get-FileHash -LiteralPath (Join-Path $vp $row.Path)).Hash)){throw ('Main asset changed: '+$row.Path)}
}
$code=@();$metadata=@()
foreach($folder in @('Scripts','Tests','Documentation')){
 $prefix='Assets/_Project/'+$folder
 $files=@(Get-ChildItem -LiteralPath (Join-Path $root $prefix) -Recurse -File|Where-Object{$_.Extension -ne '.meta'})
 $verified=@(Get-ChildItem -LiteralPath (Join-Path $vp $prefix) -Recurse -File|Where-Object{$_.Extension -ne '.meta'})
 if($files.Count -ne $verified.Count){throw ('Source inventory differs '+$folder)}
 foreach($file in $files){
  $path=$file.FullName.Substring($root.Length+1);$hash=(Get-FileHash -LiteralPath $file.FullName).Hash
  if($hash -ne (Get-FileHash -LiteralPath (Join-Path $vp $path)).Hash){throw ('Source not verified '+$path)}
  $code+=[pscustomobject]@{Path=$path;SHA256=$hash}
  if(-not(Test-Path -LiteralPath ($file.FullName+'.meta'))){$metadata+=($path+'.meta')}
  elseif((Select-String -LiteralPath ($file.FullName+'.meta') '^guid:').Line -ne (Select-String -LiteralPath (Join-Path $vp ($path+'.meta')) '^guid:').Line){throw ('Source GUID differs '+$path)}
 }
}
foreach($path in $owned){
 if(-not(Test-Path -LiteralPath (Join-Path $vp ($path+'.meta')))){throw ('Missing verified metadata '+$path)}
 if((Test-Path -LiteralPath (Join-Path $root ($path+'.meta'))) -and (Select-String -LiteralPath (Join-Path $root ($path+'.meta')) '^guid:').Line -ne (Select-String -LiteralPath (Join-Path $vp ($path+'.meta')) '^guid:').Line){throw ('Asset GUID differs '+$path)}
}
# Finish all validation before changing any main assets.
foreach($path in $metadata){Copy-Item -LiteralPath (Join-Path $vp $path) -Destination (Join-Path $root $path) -Force}
foreach($path in $owned){
 $source=Join-Path $vp $path;$target=Join-Path $root $path
 New-Item -ItemType Directory -Path (Split-Path $target) -Force|Out-Null
 if(Test-Path -LiteralPath $target){
  if((Get-FileHash -LiteralPath $source).Hash -ne (Get-FileHash -LiteralPath $target).Hash){
   $staged=Join-Path $task ([IO.Path]::GetFileName($path)+'.promotion-stage');$saved=Join-Path $task ([IO.Path]::GetFileName($path)+'.pre-atomic-replace')
   Copy-Item -LiteralPath $source -Destination $staged -Force;[IO.File]::Replace($staged,$target,$saved)
  }
 }else{Copy-Item -LiteralPath $source -Destination $target}
 Copy-Item -LiteralPath ($source+'.meta') -Destination ($target+'.meta') -Force
}
$code|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $task 'VerifiedCodeHashes.json')
$manifest=foreach($path in $owned){[pscustomobject]@{Path=$path;SHA256=(Get-FileHash -LiteralPath (Join-Path $root $path)).Hash}}
$manifest|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $task 'PromotedAssets.json')
$player=Join-Path $vp 'Builds/Chapter1Player'
[pscustomobject]@{Protocol=28;Executable=(Join-Path $player 'LAST_PROTOCOL.exe');ExecutableSHA256=(Get-FileHash -LiteralPath (Join-Path $player 'LAST_PROTOCOL.exe')).Hash;RuntimeSHA256=(Get-FileHash -LiteralPath (Join-Path $player 'LAST_PROTOCOL_Data/Managed/LastProtocol.Runtime.dll')).Hash}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $task 'VerifiedRuntime.json')
'CHAPTER_PROMOTION_PASS '+$manifest.Count
