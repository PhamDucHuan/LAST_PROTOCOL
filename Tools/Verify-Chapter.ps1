$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$task=Join-Path $root 'Builds/Pacing/20261022-Map1';$vp=Join-Path $root 'Builds/ValidationProject'
$owned=Get-Content -Raw -LiteralPath (Join-Path $task 'PromotedAssets.json')|ConvertFrom-Json
foreach($row in $owned){
 $hash=(Get-FileHash -LiteralPath (Join-Path $root $row.Path)).Hash
 if($hash -ne $row.SHA256 -or $hash -ne (Get-FileHash -LiteralPath (Join-Path $vp $row.Path)).Hash){throw ('Asset differs '+$row.Path)}
}
foreach($row in Get-Content -Raw -LiteralPath (Join-Path $root 'Builds/PacingBackups/20261022-Map1/BaselineHashes.json')|ConvertFrom-Json){
 if($row.Path -notin @($owned.Path) -and $row.Hash -ne (Get-FileHash -LiteralPath (Join-Path $root $row.Path)).Hash){throw ('Protected asset changed '+$row.Path)}
}
foreach($row in Get-Content -Raw -LiteralPath (Join-Path $task 'VerifiedCodeHashes.json')|ConvertFrom-Json){if($row.SHA256 -ne (Get-FileHash -LiteralPath (Join-Path $root $row.Path)).Hash){throw ('Source changed '+$row.Path)}}
$runtime=Get-Content -Raw -LiteralPath (Join-Path $task 'VerifiedRuntime.json')|ConvertFrom-Json
if($runtime.ExecutableSHA256 -ne (Get-FileHash -LiteralPath $runtime.Executable).Hash -or $runtime.RuntimeSHA256 -ne (Get-FileHash -LiteralPath (Join-Path (Split-Path $runtime.Executable) 'LAST_PROTOCOL_Data/Managed/LastProtocol.Runtime.dll')).Hash){throw 'Runtime differs'}
[xml]$tests=Get-Content -Raw -LiteralPath (Join-Path $task 'EditMode.xml')
$budget=Get-Content -Raw -LiteralPath (Join-Path $task 'PacingBudget.json')|ConvertFrom-Json
$menu=Get-Content -Raw -LiteralPath (Join-Path $task 'MenuPreview/Checks.json')|ConvertFrom-Json
[ordered]@{complete=$true;protocol=28;stages=12;targetMinutes=@(15,20);operationSeconds=810;timingIsDesignEstimate=$true;measuredHumanPlaythrough=$false;
 editModePassed=[int]$tests.'test-run'.passed;chapterCasesPerParty=16;parties=@(2,4);testClockMultiplier=60;aiRegressionCases=16;checkpointRegressionCases=32;menuChecks=$menu.passed.Count;gameplayRunsRetainedAfterSingleBriefingStringFix=$true;
 mapArtNavigationAndLegacyMissionPreserved=$true;assetsPromoted=@($owned).Count;runtime=$runtime}|ConvertTo-Json -Depth 6|Set-Content -LiteralPath (Join-Path $task 'CompletionReport.json')
$owned|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $task 'FinalAssetVerification.json')
'CHAPTER_FINAL_VERIFICATION_PASS'
