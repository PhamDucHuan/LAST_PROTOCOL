param([string]$CandidateName='Candidate05')
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$task=Join-Path $root 'Builds/CanThoMap/20261020-VietnamAssets'
$backup=Join-Path $root 'Builds/CanThoBackups/20261020-VietnamAssets'
$validation=Join-Path $root 'Builds/ValidationProject'
$candidate=[IO.Path]::GetFullPath((Join-Path $task $CandidateName))
if(-not $candidate.StartsWith($task+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Candidate must remain in the task directory.'}
if(-not (Get-Content -Raw -LiteralPath (Join-Path $candidate 'IntegrationValidation.json') | ConvertFrom-Json).passed){throw 'Art validation failed.'}
[xml]$tests=Get-Content -Raw -LiteralPath (Join-Path $task 'EditModeFinal.xml')
if($tests.'test-run'.result -ne 'Passed' -or [int]$tests.'test-run'.total -lt 128){throw 'Unity tests have not passed.'}
foreach($old in (Get-Content -Raw -LiteralPath (Join-Path $backup 'OwnedHashes.json') | ConvertFrom-Json)){
 if((Get-FileHash -LiteralPath (Join-Path $root $old.Path)).Hash -ne $old.Hash){throw ('Current user asset changed since backup: '+$old.Path)}
}
$owned=@('Assets/_Project/Art/Environments/CanThoCampus/CanThoCampus.fbx','Assets/_Project/Art/Environments/CanThoCampus/CanThoCampus.layout.json','Assets/_Project/Scenes/CanThoCampus/CanThoCampus.unity','Assets/_Project/Settings/CanThoCampusNavigation.asset','Assets/_Project/Prefabs/UI/CanThoOperationsMenu.prefab')
foreach($folder in @('Assets/_Project/Art/Environments/CanThoCampus/Textures','Assets/_Project/Materials/CanThoCampus')){
 $owned+=Get-ChildItem -LiteralPath (Join-Path $validation $folder) -File -Recurse | Where-Object Extension -ne '.meta' | ForEach-Object {$_.FullName.Substring($validation.Length+1).Replace('\','/')}
}
foreach($p in $owned){
 $dst=Join-Path $root $p;$src=Join-Path $validation $p
 if((Test-Path -LiteralPath ($dst+'.meta')) -and (Select-String -LiteralPath ($dst+'.meta') -Pattern '^guid:').Line -ne (Select-String -LiteralPath ($src+'.meta') -Pattern '^guid:').Line){throw ('GUID mismatch: '+$p)}
}
foreach($p in $owned){
 $dst=Join-Path $root $p;$src=Join-Path $validation $p
 Copy-Item -LiteralPath $src -Destination $dst -Force
 if(Test-Path -LiteralPath ($src+'.meta')){Copy-Item -LiteralPath ($src+'.meta') -Destination ($dst+'.meta') -Force}
}
foreach($name in @('CanThoCampus.blend','CanThoCampus.layout.json','VietnamIntegration.json')){Copy-Item -LiteralPath (Join-Path $candidate $name) -Destination (Join-Path $root ('ArtSource/Blender/CanThoCampus/'+$name)) -Force}
Get-ChildItem -LiteralPath (Join-Path $candidate 'Textures') -File | Copy-Item -Destination (Join-Path $root 'ArtSource/Blender/CanThoCampus/Textures') -Force
foreach($folder in @('Assets/_Project/Tests/EditMode','Assets/_Project/Documentation')){
 Get-ChildItem -LiteralPath (Join-Path $validation $folder) -Filter '*.meta' -File | ForEach-Object {
  $targetMeta=Join-Path $root ($folder+'/'+$_.Name)
  if(-not (Test-Path -LiteralPath $targetMeta)){Copy-Item -LiteralPath $_.FullName -Destination $targetMeta}
 }
}
$manifest=foreach($p in $owned){[pscustomobject]@{Path=$p;SHA256=(Get-FileHash -LiteralPath (Join-Path $root $p)).Hash}}
$manifest | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $task 'PromotedAssets.json')
Write-Output ('VIETNAM_ASSETS_PROMOTION_PASS '+$manifest.Count)
