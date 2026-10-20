param(
    [string]$TaskDirectory='Builds/CanThoMap/20261016-Neighborhood',
    [string]$BackupDirectory='Builds/CanThoBackups/20261016-Neighborhood',
    [ValidatePattern('^Candidate[0-9]{2}$')][string]$CandidateName='Candidate05',
    [int]$ExpectedTests=117,
    [int]$ExpectedGraphicalChecks=80,
    [int]$ArtRevision=6,
    [int]$Protocol=16,
    [ValidatePattern('^[A-Za-z0-9_-]+$')][string]$DistributionName='LAST_PROTOCOL-Campus-Protocol16-Neighborhood-20261016'
)
$ErrorActionPreference='Stop'
$workspace=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$taskRoot=[IO.Path]::GetFullPath((Join-Path $workspace $TaskDirectory))
$validation=Join-Path $workspace 'Builds/ValidationProject'
$backup=[IO.Path]::GetFullPath((Join-Path $workspace $BackupDirectory))
foreach($path in @($taskRoot,$backup)) { if(-not $path.StartsWith($workspace+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Work folders must remain inside the project.'} }
$candidate=Join-Path $taskRoot $CandidateName
$checks=[ordered]@{}
[xml]$tests=Get-Content -LiteralPath (Join-Path $taskRoot 'EditModeFinal.xml')
$checks.EditMode=($tests.'test-run'.result -eq 'Passed' -and [int]$tests.'test-run'.passed -eq $ExpectedTests)
foreach ($stage in @('EditModeFinal','WindowsBuild','Refresh')) {
    $checks[$stage+'Process']=[int](Get-Content -LiteralPath (Join-Path $taskRoot ($stage+'-ExitCode.txt'))) -eq 0
}
$source=Get-Content -Raw -LiteralPath (Join-Path $taskRoot 'PreservationFinal.json') | ConvertFrom-Json
$checks.SourcePreservation=$source.passed
$smoke=Get-Content -Raw -LiteralPath (Join-Path $taskRoot 'RuntimeEvidence/MissionSmoke/Summary.json') | ConvertFrom-Json
$checks.SixSmokeProcesses=($smoke.Count -eq 6 -and @($smoke | Where-Object {-not $_.Passed}).Count -eq 0)
$runtime=Get-Content -Raw -LiteralPath (Join-Path $taskRoot 'RuntimeEvidence/GraphicalPreview/Checks.json') | ConvertFrom-Json
$checks.GraphicalRuntime=($runtime.complete -and $runtime.passed.Count -eq $ExpectedGraphicalChecks -and $runtime.failures.Count -eq 0)
$protected=Get-Content -Raw -LiteralPath (Join-Path $backup 'ProtectedHashes.json') | ConvertFrom-Json
$changed=@($protected | Where-Object { (Get-FileHash -LiteralPath (Join-Path $workspace $_.Path)).Hash -ne $_.Hash })
$checks.ProtectedFilesUnchanged=$changed.Count -eq 0
$manifest=Get-Content -Raw -LiteralPath (Join-Path $taskRoot 'PromotedAssets.json') | ConvertFrom-Json
$mismatches=@($manifest | Where-Object { (Get-FileHash -LiteralPath (Join-Path $workspace $_.Path)).Hash -ne $_.SHA256 })
$checks.PromotedAssetsMatch=$mismatches.Count -eq 0
$codeFiles=@(Get-ChildItem -LiteralPath (Join-Path $workspace 'Assets/_Project/Scripts'),(Join-Path $workspace 'Assets/_Project/Tests') -Filter '*.cs' -Recurse -File)
$codeMismatch=@($codeFiles | Where-Object {
    $relative=$_.FullName.Substring($workspace.Length+1)
    (Get-FileHash -LiteralPath $_.FullName).Hash -ne (Get-FileHash -LiteralPath (Join-Path $validation $relative)).Hash
})
$checks.TestedCodeMatches=$codeMismatch.Count -eq 0
foreach ($name in @('CanThoCampus.blend','CanThoCampus.layout.json')) {
    $checks['EditableSource_'+$name]=(Get-FileHash -LiteralPath (Join-Path $candidate $name)).Hash -eq (Get-FileHash -LiteralPath (Join-Path $workspace ('ArtSource/Blender/CanThoCampus/'+$name))).Hash
}
$guidChecks=@()
foreach($path in @('Assets/_Project/Art/Environments/CanThoCampus/CanThoCampus.fbx.meta',
    'Assets/_Project/Scenes/CanThoCampus/CanThoCampus.unity.meta',
    'Assets/_Project/Settings/CanThoCampusNavigation.asset.meta',
    'Assets/_Project/Prefabs/UI/CanThoOperationsMenu.prefab.meta')) {
    $guidChecks+=((Select-String -LiteralPath (Join-Path $workspace $path) -Pattern '^guid:').Line -eq
        (Select-String -LiteralPath (Join-Path $backup $path) -Pattern '^guid:').Line)
}
$checks.StableSceneModelNavMenuGUIDs=@($guidChecks | Where-Object {-not $_}).Count -eq 0
foreach($path in @('Assets/_Project/Settings/CanThoFuturePostProcessing.asset','Assets/_Project/Settings/CanThoSkyReflection.cubemap')) {
    $checks[([IO.Path]::GetFileNameWithoutExtension($path))+'MatchesValidation']=(Get-FileHash -LiteralPath (Join-Path $workspace $path)).Hash -eq (Get-FileHash -LiteralPath (Join-Path $validation $path)).Hash
}
$zipPath=Join-Path $workspace ('Builds/Distributions/'+$DistributionName+'.zip')
$hash=(Get-FileHash -LiteralPath $zipPath).Hash
$checks.DistributionHash=$hash -eq ((Get-Content -Raw -LiteralPath ($zipPath+'.sha256')).Split(' ')[0])
$checks.PackageEntriesVerified=[bool](Select-String -LiteralPath (Join-Path $taskRoot 'Package.log') -Pattern 'Verified\s+: True')
$requiredPassed=@($checks.Values | Where-Object {-not $_}).Count -eq 0
$captures=@(Get-ChildItem -LiteralPath (Join-Path $taskRoot 'UnityCaptureFinal') -Filter '*.png' -File)
$captureExit=[int](Get-Content -LiteralPath (Join-Path $taskRoot 'CaptureFinal-ExitCode.txt'))
$report=[ordered]@{
    ArtRevision=$ArtRevision;Protocol=$Protocol;RequiredChecksPassed=$requiredPassed
    Result=$(if(-not $requiredPassed){'FAIL'}elseif($captureExit -ne 0){'REQUIRED_CHECKS_PASS_WITH_CAPTURE_UTILITY_FAILURE'}else{'REQUIRED_CHECKS_PASS'})
    Checks=$checks;ProtectedFiles=$protected.Count;PromotedAssets=$manifest.Count;CodeFiles=$codeFiles.Count
    CaptureUtilityExit=$captureExit
    CaptureImages=$captures.Count;InternetRetest='CHUA_KIEM_THU';TargetHardwareFPS='CHUA_KIEM_THU'
    Distribution=@{Path=$zipPath;Bytes=(Get-Item -LiteralPath $zipPath).Length;SHA256=$hash}
}
$report | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $taskRoot 'FinalVerification.json')
$report | ConvertTo-Json -Depth 8
if(-not $requiredPassed){throw 'Final verification failed.'}
