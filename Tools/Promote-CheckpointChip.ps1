$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$task=Join-Path $root 'Builds/Checkpoints/20261022-CheckpointChip'
$backup=Join-Path $root 'Builds/CheckpointBackups/20261022-CheckpointChip'
$vp=Join-Path $root 'Builds/ValidationProject'
$owned=@('Assets/_Project/Prefabs/Networking/NetworkRoomState.prefab','Assets/_Project/Prefabs/UI/CanThoOperationsMenu.prefab')
[xml]$tests=Get-Content -Raw -LiteralPath (Join-Path $task 'EditMode.xml')
if($tests.'test-run'.result -ne 'Passed' -or [int]$tests.'test-run'.total -lt 157){throw 'EditMode has not passed.'}
foreach($stage in @('Install','Build','EditMode','Idempotence')){
 if([int](Get-Content -LiteralPath (Join-Path $task ($stage+'.exit'))) -ne 0){throw ('Stage failed: '+$stage)}
}
$suites=@{'NetworkSmoke2-Final'=2;'NetworkSmoke4-Final'=4;DefenseRegression=2;ReviveRegression=6}
foreach($suite in $suites.Keys){
 $rows=@(Get-Content -Raw -LiteralPath (Join-Path $task ($suite+'/Summary.json'))|ConvertFrom-Json)
 if($rows.Count -ne $suites[$suite] -or ($rows|Where-Object {-not $_.Passed})){throw ('Network suite failed: '+$suite)}
}
foreach($suite in @('GraphicalPreview-FinalClean','LegacyGraphicalPreview')){
 $report=Get-Content -Raw -LiteralPath (Join-Path $task ($suite+'/Checks.json'))|ConvertFrom-Json
 $minimum=if($suite -eq 'GraphicalPreview-FinalClean'){83}else{107}
 if(-not $report.complete -or $report.failures.Count -ne 0 -or $report.passed.Count -lt $minimum){throw ('Graphical suite failed: '+$suite)}
}
foreach($suite in @($suites.Keys)+@('GraphicalPreview-FinalClean','LegacyGraphicalPreview')){
 if(Get-ChildItem -LiteralPath (Join-Path $task $suite) -Filter '*.log' -File | Select-String -Pattern 'NullReferenceException|MissingReferenceException|InvalidOperationException|IndexOutOfRangeException|KeyNotFoundException|Failed to create agent|\[Error\]'){
  throw ('Application errors in suite: '+$suite)
 }
}
foreach($name in @('PreservationValidation','Idempotence')){
 if(-not (Get-Content -Raw -LiteralPath (Join-Path $task ($name+'.json'))|ConvertFrom-Json).passed){throw ($name+' failed')}
}
foreach($old in Get-Content -Raw -LiteralPath (Join-Path $backup 'BaselineHashes.json')|ConvertFrom-Json){
 $current=(Get-FileHash -LiteralPath (Join-Path $root $old.Path)).Hash
 if($current -ne $old.Hash -and -not ($owned -contains $old.Path -and $current -eq (Get-FileHash -LiteralPath (Join-Path $vp $old.Path)).Hash)){
  throw ('Main asset changed during validation: '+$old.Path)
 }
}
$codeHashes=@();$metadata=@()
foreach($folder in @('Assets/_Project/Scripts','Assets/_Project/Tests')){
 $sourceFiles=@(Get-ChildItem -LiteralPath (Join-Path $root $folder) -Filter '*.cs' -Recurse -File)
 if($sourceFiles.Count -ne @(Get-ChildItem -LiteralPath (Join-Path $vp $folder) -Filter '*.cs' -Recurse -File).Count){throw 'Validation source file inventory differs.'}
 foreach($file in $sourceFiles){
  $path=$file.FullName.Substring($root.Length+1);$verified=Join-Path $vp $path
  $hash=(Get-FileHash -LiteralPath $file.FullName).Hash
  if($hash -ne (Get-FileHash -LiteralPath $verified).Hash){throw ('Source differs from tested build: '+$path)}
  $codeHashes+=[pscustomobject]@{Path=$path;SHA256=$hash}
  if(-not(Test-Path -LiteralPath ($file.FullName+'.meta'))){$metadata+=($path+'.meta')}
  elseif((Select-String -LiteralPath ($file.FullName+'.meta') '^guid:').Line -ne (Select-String -LiteralPath ($verified+'.meta') '^guid:').Line){throw ('Script GUID mismatch: '+$path)}
 }
}
foreach($path in $owned){
 if(-not(Test-Path -LiteralPath (Join-Path $vp $path))){throw ('Verified asset missing: '+$path)}
 if((Select-String -LiteralPath (Join-Path $root ($path+'.meta')) '^guid:').Line -ne (Select-String -LiteralPath (Join-Path $vp ($path+'.meta')) '^guid:').Line){throw ('Asset GUID mismatch: '+$path)}
}
$doc='Assets/_Project/Documentation/CHECKPOINT_CHIP.md.meta'
if(-not(Test-Path -LiteralPath (Join-Path $root $doc))){$metadata+=$doc}
foreach($path in $metadata){if(-not(Test-Path -LiteralPath (Join-Path $vp $path))){throw ('Verified metadata missing: '+$path)}}
# All checks above finish before replacing any asset in the user's project.
foreach($path in $metadata){Copy-Item -LiteralPath (Join-Path $vp $path) -Destination (Join-Path $root $path) -Force}
foreach($path in $owned){
 $source=Join-Path $vp $path;$target=Join-Path $root $path
 if((Get-FileHash -LiteralPath $source).Hash -ne (Get-FileHash -LiteralPath $target).Hash){
  # Replacing the directory entry also works when an importer holds a read mapping.
  # The original bytes are already protected by the task backup and preflight hashes.
  $staged=[IO.Path]::GetFullPath((Join-Path $task ([IO.Path]::GetFileName($path)+'.promotion-stage')))
  if(-not $staged.StartsWith($task+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Invalid staging path'}
  Copy-Item -LiteralPath $source -Destination $staged -Force
  $replaced=Join-Path $task ([IO.Path]::GetFileName($path)+'.pre-atomic-replace')
  [IO.File]::Replace($staged,$target,$replaced)
 }
 Copy-Item -LiteralPath (Join-Path $vp ($path+'.meta')) -Destination (Join-Path $root ($path+'.meta')) -Force
}
$codeHashes|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $task 'VerifiedCodeHashes.json')
$manifest=foreach($path in $owned){[pscustomobject]@{Path=$path;SHA256=(Get-FileHash -LiteralPath (Join-Path $root $path)).Hash}}
$manifest|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $task 'PromotedAssets.json')
$player=Join-Path $vp 'Builds/CheckpointChipPlayer'
[pscustomobject]@{Protocol=26;Executable=(Join-Path $player 'LAST_PROTOCOL.exe');ExecutableSHA256=(Get-FileHash -LiteralPath (Join-Path $player 'LAST_PROTOCOL.exe')).Hash;RuntimeSHA256=(Get-FileHash -LiteralPath (Join-Path $player 'LAST_PROTOCOL_Data/Managed/LastProtocol.Runtime.dll')).Hash}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $task 'VerifiedRuntime.json')
'CHECKPOINT_CHIP_PROMOTION_PASS '+$manifest.Count
