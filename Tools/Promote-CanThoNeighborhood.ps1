param(
    [string]$TaskDirectory='Builds/CanThoMap/20261016-Neighborhood',
    [string]$BackupDirectory='Builds/CanThoBackups/20261016-Neighborhood',
    [ValidatePattern('^Candidate[0-9]{2}$')][string]$CandidateName='Candidate05',
    [int]$MinimumTests=117,
    [int]$MinimumGraphicalChecks=80
)
$ErrorActionPreference='Stop'
$workspace=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$taskRoot=[IO.Path]::GetFullPath((Join-Path $workspace $TaskDirectory))
$backupRoot=[IO.Path]::GetFullPath((Join-Path $workspace $BackupDirectory))
foreach($path in @($taskRoot,$backupRoot)) { if(-not $path.StartsWith($workspace+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Work folders must remain inside the project.'} }
$validation=Join-Path $workspace 'Builds/ValidationProject'
$candidate=Join-Path $taskRoot $CandidateName
[xml]$tests=Get-Content -LiteralPath (Join-Path $taskRoot 'EditModeFinal.xml')
if ($tests.'test-run'.result -ne 'Passed' -or [int]$tests.'test-run'.total -lt $MinimumTests) { throw 'EditMode results must pass before promotion.' }
foreach ($name in @('EditModeFinal','WindowsBuild')) {
    if ([int](Get-Content -LiteralPath (Join-Path $taskRoot ($name+'-ExitCode.txt'))) -ne 0) { throw "$name process failed." }
}
if (-not (Get-Content -Raw -LiteralPath (Join-Path $taskRoot 'PreservationFinal.json') | ConvertFrom-Json).passed) { throw 'Source preservation checks failed.' }
$smoke=Get-Content -Raw -LiteralPath (Join-Path $taskRoot 'RuntimeEvidence/MissionSmoke/Summary.json') | ConvertFrom-Json
if ($smoke.Count -ne 6 -or @($smoke | Where-Object { -not $_.Passed }).Count) { throw 'Four player smoke failed.' }
$graphical=Get-Content -Raw -LiteralPath (Join-Path $taskRoot 'RuntimeEvidence/GraphicalPreview/Checks.json') | ConvertFrom-Json
if (-not $graphical.complete -or $graphical.failures.Count -or $graphical.passed.Count -lt $MinimumGraphicalChecks) { throw 'Graphical checks missing/failed.' }
$protected=Get-Content -Raw -LiteralPath (Join-Path $backupRoot 'ProtectedHashes.json') | ConvertFrom-Json
foreach ($file in $protected) {
    if ((Get-FileHash -LiteralPath (Join-Path $workspace $file.Path)).Hash -ne $file.Hash) { throw "Protected file changed during work: $($file.Path)." }
}
$assets=@('Assets/_Project/Art/Environments/CanThoCampus/CanThoCampus.fbx',
    'Assets/_Project/Art/Environments/CanThoCampus/CanThoCampus.layout.json',
    'Assets/_Project/Scenes/CanThoCampus/CanThoCampus.unity',
    'Assets/_Project/Settings/CanThoCampusNavigation.asset',
    'Assets/_Project/Prefabs/UI/CanThoOperationsMenu.prefab')
foreach ($p in $assets) {
    $existing=Join-Path $workspace ($p+'.meta');$staged=Join-Path $validation ($p+'.meta')
    $a=(Select-String -LiteralPath $existing -Pattern '^guid:').Line
    $b=(Select-String -LiteralPath $staged -Pattern '^guid:').Line
    if ($a -ne $b) { throw "GUID mismatch: $p" }
}
foreach ($folder in @('Assets/_Project/Art/Environments/CanThoCampus/Textures','Assets/_Project/Materials/CanThoCampus')) {
    Get-ChildItem -LiteralPath (Join-Path $validation $folder) -File -Recurse | ForEach-Object {
        if ($_.Extension -ne '.meta') { $assets += $_.FullName.Substring($validation.Length+1).Replace('\','/') }
    }
}
# Refuse to overwrite any edits saved by the user while validation was running.
foreach ($p in $assets) {
    foreach ($q in @($p,$p+'.meta')) {
        $prior=Join-Path $backupRoot $q;$current=Join-Path $workspace $q
        if ((Test-Path -LiteralPath $prior) -and (Test-Path -LiteralPath $current) -and
            (Get-FileHash -LiteralPath $prior).Hash -ne (Get-FileHash -LiteralPath $current).Hash) {
            throw "Owned asset changed since backup; preserve for review: $q"
        }
    }
    $existing=Join-Path $workspace ($p+'.meta');$staged=Join-Path $validation ($p+'.meta')
    if ((Test-Path -LiteralPath $existing) -and (Test-Path -LiteralPath $staged) -and
        (Select-String -LiteralPath $existing -Pattern '^guid:').Line -ne (Select-String -LiteralPath $staged -Pattern '^guid:').Line) {
        throw "GUID mismatch: $p"
    }
}
foreach ($name in @('CanThoCampus.blend','CanThoCampus.layout.json')) {
    $p='ArtSource/Blender/CanThoCampus/'+$name
    if ((Get-FileHash -LiteralPath (Join-Path $backupRoot $p)).Hash -ne (Get-FileHash -LiteralPath (Join-Path $workspace $p)).Hash) {
        throw "Blender source changed since backup: $p"
    }
}
foreach ($p in $assets) {
    $sourcePath=Join-Path $validation $p;$targetPath=Join-Path $workspace $p
    Copy-Item -LiteralPath $sourcePath -Destination $targetPath -Force
    if (Test-Path -LiteralPath ($sourcePath+'.meta')) { Copy-Item -LiteralPath ($sourcePath+'.meta') -Destination ($targetPath+'.meta') -Force }
}
# Keep Editor/test source GUIDs when a new script has not yet been imported by the
# user's Editor. Never replace an existing script GUID just to match validation.
foreach ($file in (Get-ChildItem -LiteralPath (Join-Path $validation 'Assets/_Project/Tests/EditMode') -Filter '*.cs.meta' -File)) {
    $relative=$file.FullName.Substring($validation.Length+1)
    if (-not (Test-Path -LiteralPath (Join-Path $workspace $relative))) {
        Copy-Item -LiteralPath (Join-Path $validation $relative) -Destination (Join-Path $workspace $relative)
    }
}
$sourceRoot=Join-Path $workspace 'ArtSource/Blender/CanThoCampus'
foreach ($name in @('CanThoCampus.blend','CanThoCampus.layout.json')) { Copy-Item -LiteralPath (Join-Path $candidate $name) -Destination (Join-Path $sourceRoot $name) -Force }
$manifest=@()
foreach ($p in $assets) {
    foreach ($q in @($p,$p+'.meta')) {
        if (Test-Path -LiteralPath (Join-Path $workspace $q)) {
            $hash=(Get-FileHash -LiteralPath (Join-Path $workspace $q)).Hash
            if ($hash -ne (Get-FileHash -LiteralPath (Join-Path $validation $q)).Hash) { throw "Promoted hash mismatch: $q" }
            $manifest += [pscustomobject]@{Path=$q;SHA256=$hash}
        }
    }
}
$manifest | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $taskRoot 'PromotedAssets.json')
Write-Output ('PROMOTION_PASS assets='+$manifest.Count)
