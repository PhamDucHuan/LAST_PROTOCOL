$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$task=Join-Path $root 'Builds/EnemyAI/20261022-EnemyAI'
$player=Join-Path $root 'Builds/ValidationProject/Builds/EnemyAIPlayer/LAST_PROTOCOL.exe'
if(Test-Path -LiteralPath (Join-Path $task 'AI2-Final')){throw 'Preserve existing final evidence'}
$ai2=Get-Content -Raw -LiteralPath (Join-Path $task 'AI2-Run3/Summary.json')|ConvertFrom-Json
if(@($ai2).Count -ne 2 -or @($ai2|Where-Object{-not $_.Passed}).Count){throw 'AI two-player test missing'}
Copy-Item -LiteralPath (Join-Path $task 'AI2-Run3') -Destination (Join-Path $task 'AI2-Final') -Recurse
& (Join-Path $PSScriptRoot 'Run-EnemyAISmoke.ps1') -Player $player -Players 4 -OutputDirectory (Join-Path $task 'AI4-Final')
& (Join-Path $PSScriptRoot 'Run-CheckpointChipSmoke.ps1') -Player $player -Players 2 -OutputDirectory (Join-Path $task 'CheckpointRegression2')
& (Join-Path $PSScriptRoot 'Run-CheckpointChipSmoke.ps1') -Player $player -Players 4 -OutputDirectory (Join-Path $task 'CheckpointRegression4')
& (Join-Path $PSScriptRoot 'Run-UplinkDefenseSmoke.ps1') -Player $player -Players 2 -OutputDirectory (Join-Path $task 'DefenseRegression')
& (Join-Path $PSScriptRoot 'Run-MultiplayerSmoke.ps1') -Player $player -EnemyTest
Copy-Item -LiteralPath (Join-Path $root 'Builds/ValidationResults/EnemySmoke') -Destination (Join-Path $task 'EnemyRegression') -Recurse
'ENEMY_AI_ALL_SUITES_PASS'
