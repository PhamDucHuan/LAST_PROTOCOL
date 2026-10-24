$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$task=Join-Path $root 'Builds/Pacing/20261022-Map1'
$player=Join-Path $root 'Builds/ValidationProject/Builds/Chapter1Player/LAST_PROTOCOL.exe'
if(Test-Path -LiteralPath (Join-Path $task 'Chapter2-Final')){throw 'Preserve final evidence'}
$two=@(Get-Content -Raw -LiteralPath (Join-Path $task 'Chapter2-Run2/Summary.json')|ConvertFrom-Json)
if($two.Count -ne 2 -or @($two|Where-Object{-not $_.Passed}).Count){throw 'Two-player chapter test missing'}
Copy-Item -LiteralPath (Join-Path $task 'Chapter2-Run2') -Destination (Join-Path $task 'Chapter2-Final') -Recurse
& (Join-Path $PSScriptRoot 'Run-ChapterSmoke.ps1') -Player $player -Players 4 -OutputDirectory (Join-Path $task 'Chapter4-Final')
& (Join-Path $PSScriptRoot 'Run-EnemyAISmoke.ps1') -Player $player -Players 2 -OutputDirectory (Join-Path $task 'AIRegression2')
& (Join-Path $PSScriptRoot 'Run-CheckpointChipSmoke.ps1') -Player $player -Players 2 -OutputDirectory (Join-Path $task 'CheckpointRegression2')
$menuStarted=[DateTime]::UtcNow
& (Join-Path $PSScriptRoot 'Run-MenuPreview.ps1') -Player $player -Level 2 | Out-Null
$menu=Join-Path $root 'Builds/ValidationResults/CanThoMenuPreview'
New-Item -ItemType Directory -Path (Join-Path $task 'MenuPreview')|Out-Null
Get-ChildItem -LiteralPath $menu -File|Where-Object{$_.LastWriteTimeUtc -ge $menuStarted}|Copy-Item -Destination (Join-Path $task 'MenuPreview')
$report=Get-Content -Raw -LiteralPath (Join-Path $task 'MenuPreview/Checks.json')|ConvertFrom-Json
'CHAPTER_MENU_CHECKS '+$report.passed.Count
'CHAPTER_ALL_SUITES_PASS'
