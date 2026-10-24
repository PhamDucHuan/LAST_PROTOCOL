$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$task=Join-Path $root 'Builds/Interaction/20261021-AdminDataStation'
$vp=Join-Path $root 'Builds/ValidationProject'
$backup=Join-Path $root 'Builds/InteractionBackups/20261021-AdminDataStation'
[xml]$tests=Get-Content -Raw (Join-Path $task 'EditModeFinal.xml')
if($tests.'test-run'.result -ne 'Passed' -or [int]$tests.'test-run'.total -lt 136){throw 'EditMode has not passed.'}
foreach($stage in @('Install','EditModeFinal','BuildFinal','CaptureFinal')){
 if([int](Get-Content (Join-Path $task ($stage+'-ExitCode.txt'))) -ne 0){throw ('Stage failed: '+$stage)}
}
foreach($r in Get-Content -Raw (Join-Path $task 'NetworkSmoke/Summary.json')|ConvertFrom-Json){if(-not $r.Passed){throw 'Interaction smoke failed.'}}
foreach($r in Get-Content -Raw (Join-Path $task 'ReviveSmoke/Summary.json')|ConvertFrom-Json){if(-not $r.Passed){throw 'Revive regression failed.'}}
$preview=Get-Content -Raw (Join-Path $task 'GraphicalPreview/Checks.json')|ConvertFrom-Json
if(-not $preview.complete -or $preview.failures.Count -ne 0){throw 'Graphical interaction checks failed.'}
if(-not (Get-Content -Raw (Join-Path $task 'PreservationValidation.json')|ConvertFrom-Json).passed){throw 'Scene preservation failed.'}
foreach($old in Get-Content -Raw (Join-Path $backup 'OwnedHashes.json')|ConvertFrom-Json){
 if($old.Path -match '/Scripts/'){continue}
 if((Get-FileHash -LiteralPath (Join-Path $root $old.Path)).Hash -ne $old.Hash){throw ('User asset changed during validation: '+$old.Path)}
}
$owned=@('Assets/_Project/Prefabs/Player/NetworkPlayer.prefab','Assets/_Project/Scenes/CanThoCampus/CanThoCampus.unity','Assets/_Project/Settings/CanThoCampusNavigation.asset','Assets/_Project/Prefabs/UI/CanThoOperationsMenu.prefab')
foreach($p in $owned){
 if((Select-String -LiteralPath (Join-Path $root ($p+'.meta')) -Pattern '^guid:').Line -ne (Select-String -LiteralPath (Join-Path $vp ($p+'.meta')) -Pattern '^guid:').Line){throw ('GUID mismatch: '+$p)}
}
foreach($p in $owned){Copy-Item -LiteralPath (Join-Path $vp $p) -Destination (Join-Path $root $p) -Force;Copy-Item -LiteralPath (Join-Path $vp ($p+'.meta')) -Destination (Join-Path $root ($p+'.meta')) -Force}
$materials='Assets/_Project/Materials/Interactions'
New-Item -ItemType Directory -Path (Join-Path $root $materials) -Force|Out-Null
Copy-Item -LiteralPath (Join-Path $vp ($materials+'.meta')) -Destination (Join-Path $root ($materials+'.meta')) -Force
foreach($file in Get-ChildItem -LiteralPath (Join-Path $vp $materials) -File){Copy-Item -LiteralPath $file.FullName -Destination (Join-Path $root $materials) -Force;if($file.Extension -ne '.meta'){$owned+=($materials+'/'+$file.Name)}}
foreach($folder in @('Assets/_Project/Scripts','Assets/_Project/Tests')){
 foreach($file in Get-ChildItem -LiteralPath (Join-Path $root $folder) -Recurse -File -Filter '*.cs'){
  $p=$file.FullName.Substring($root.Length+1);$staged=Join-Path $vp $p
  if(Test-Path -LiteralPath $staged){
   if((Get-FileHash -LiteralPath $file.FullName).Hash -ne (Get-FileHash -LiteralPath $staged).Hash){throw ('Working code differs from tested code: '+$p)}
   if(-not(Test-Path -LiteralPath ($file.FullName+'.meta'))){Copy-Item -LiteralPath ($staged+'.meta') -Destination ($file.FullName+'.meta')}
  }
 }
}
$doc='Assets/_Project/Documentation/DATA_STATION_INTERACTION.md'
if(-not(Test-Path -LiteralPath (Join-Path $root ($doc+'.meta')))){Copy-Item -LiteralPath (Join-Path $vp ($doc+'.meta')) -Destination (Join-Path $root ($doc+'.meta'))}
$manifest=foreach($p in $owned){[pscustomobject]@{Path=$p;SHA256=(Get-FileHash -LiteralPath (Join-Path $root $p)).Hash}}
$manifest|ConvertTo-Json|Set-Content (Join-Path $task 'PromotedAssets.json')
'DATA_STATION_PROMOTION_PASS '+$manifest.Count
