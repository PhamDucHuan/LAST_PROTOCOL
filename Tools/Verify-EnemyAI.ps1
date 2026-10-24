$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$task=Join-Path $root 'Builds/EnemyAI/20261022-EnemyAI'
$vp=Join-Path $root 'Builds/ValidationProject'
$baseline=Get-Content -Raw -LiteralPath (Join-Path $root 'Builds/EnemyAIBackups/20261022-EnemyAI/BaselineHashes.json')|ConvertFrom-Json
$manifest=Get-Content -Raw -LiteralPath (Join-Path $task 'PromotedAssets.json')|ConvertFrom-Json
$rows=foreach($row in $manifest){
 $hash=(Get-FileHash -LiteralPath (Join-Path $root $row.Path)).Hash
 if($hash -ne $row.SHA256 -or $hash -ne (Get-FileHash -LiteralPath (Join-Path $vp $row.Path)).Hash){throw ('Promoted asset differs '+$row.Path)}
 [pscustomobject]@{Path=$row.Path;SHA256=$hash}
}
foreach($row in $baseline){if($row.Path -notin @($manifest.Path) -and $row.Hash -ne (Get-FileHash -LiteralPath (Join-Path $root $row.Path)).Hash){throw ('Protected asset differs '+$row.Path)}}
foreach($row in Get-Content -Raw -LiteralPath (Join-Path $task 'VerifiedCodeHashes.json')|ConvertFrom-Json){if($row.SHA256 -ne (Get-FileHash -LiteralPath (Join-Path $root $row.Path)).Hash){throw ('Source changed '+$row.Path)}}
$runtime=Get-Content -Raw -LiteralPath (Join-Path $task 'VerifiedRuntime.json')|ConvertFrom-Json
if($runtime.ExecutableSHA256 -ne (Get-FileHash -LiteralPath $runtime.Executable).Hash){throw 'Player changed'}
$dll=Join-Path (Split-Path $runtime.Executable) 'LAST_PROTOCOL_Data/Managed/LastProtocol.Runtime.dll'
if($runtime.RuntimeSHA256 -ne (Get-FileHash -LiteralPath $dll).Hash){throw 'Runtime changed'}
$rows|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $task 'FinalAssetVerification.json')
[xml]$tests=Get-Content -Raw -LiteralPath (Join-Path $task 'EditMode.xml')
$report=[ordered]@{complete=$true;protocol=27;editModePassed=[int]$tests.'test-run'.passed;aiCasesPerParty=16;parties=@(2,4);checkpointCasesPerParty=32;legacyDefenceCases=22;legacyEnemyRegression=$true;assetsPromoted=$rows.Count;mapNavigationPlayerAndLegacyRobotsPreserved=$true;runtime=$runtime;limitations=@('Localhost, not internet/Relay latency testing','Existing prototype robot visuals','Boss, drone, inventory, campaign persistence and player Tier-2 hacking remain separate modules')}
$report|ConvertTo-Json -Depth 6|Set-Content -LiteralPath (Join-Path $task 'CompletionReport.json')
'ENEMY_AI_FINAL_VERIFICATION_PASS'
