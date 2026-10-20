param([string]$Player = '',[switch]$HealthTest,[switch]$CombatTest,[switch]$EnemyTest,[switch]$ReviveTest,[switch]$LevelTest,[ValidateRange(0,2)][int]$Level = 0,[switch]$NetworkStress,[switch]$MissionTest)
$ErrorActionPreference = 'Stop'
if ($EnemyTest -and $ReviveTest) { throw 'Run EnemyTest and ReviveTest separately to isolate their fixtures.' }
if ($MissionTest -and ($HealthTest -or $CombatTest -or $EnemyTest -or $ReviveTest -or $Level -ne 2 -or -not $LevelTest)) { throw 'MissionTest requires LevelTest Level 2 without combat/health fixtures.' }
$workspace = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if (-not $Player) {
    $buildFolder = if ($LevelTest) { 'MenuPlayer' } elseif ($ReviveTest) { 'RevivePlayer' } elseif ($EnemyTest) { 'EnemyPlayer' } elseif ($CombatTest) { 'CombatPlayer' } elseif ($HealthTest) { 'HealthPlayer' } else { 'ThirdPersonPlayer' }
    $Player = Join-Path $workspace ('Builds\ValidationProject\Builds\' + $buildFolder + '\LAST_PROTOCOL.exe')
}
if ($EnemyTest -or $ReviveTest) { $CombatTest = $true }
if ($CombatTest) { $HealthTest = $true }
if (-not (Test-Path -LiteralPath $Player)) { throw 'Build Windows development MVP first.' }
$results = Join-Path $workspace 'Builds\ValidationResults\Smoke'
if ($HealthTest) { $results = Join-Path $workspace 'Builds\ValidationResults\HealthSmoke' }
if ($CombatTest) { $results = Join-Path $workspace 'Builds\ValidationResults\CombatSmoke' }
if ($EnemyTest) { $results = Join-Path $workspace 'Builds\ValidationResults\EnemySmoke' }
if ($ReviveTest) { $results = Join-Path $workspace 'Builds\ValidationResults\ReviveSmoke' }
if ($LevelTest) { $results = Join-Path $workspace ('Builds\ValidationResults\LevelSmoke' + $Level) }
if ($NetworkStress) { $results = Join-Path $workspace ('Builds\ValidationResults\NetworkStressSmoke' + $Level) }
if ($MissionTest) { $results = Join-Path $workspace 'Builds\ValidationResults\MissionSmoke' }
New-Item -ItemType Directory -Path $results -Force | Out-Null
$previousFiles = @(Get-ChildItem -LiteralPath $results -File)
if ($previousFiles.Count -gt 0) {
    $history = Join-Path $workspace ('Builds\ValidationResults\History\' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff') + '-Smoke')
    New-Item -ItemType Directory -Path $history -Force | Out-Null
    $previousFiles | Copy-Item -Destination $history
}
[IO.File]::WriteAllText((Join-Path $results 'Summary.json'), '[]')
$launched = [Collections.Generic.List[Diagnostics.Process]]::new()
function Start-SmokePlayer([string]$Role,[string]$Name,[int]$Duration) {
    $resultPath = Join-Path $results ($Name + '.json')
    $logPath = Join-Path $results ($Name + '.log')
    # Never reuse stale JSON as evidence for this run.
    [IO.File]::WriteAllText($resultPath, '{}')
    $arguments = @('-batchmode','-nographics','-logFile',('"' + $logPath + '"'),
        '--lp-smoke',$Role,'--lp-result',('"' + $resultPath + '"'),'--lp-duration',$Duration,
        '--lp-auto-start','4','--lp-start-delay','25')
    if ($HealthTest) { $arguments += '--lp-health-test' }
    if ($CombatTest) { $arguments += '--lp-combat-test' }
    if ($EnemyTest) { $arguments += '--lp-enemy-test' }
    if ($ReviveTest) { $arguments += '--lp-revive-test' }
    if ($LevelTest) { $arguments += @('--lp-level-test','--lp-level',$Level) }
    if ($NetworkStress) { $arguments += '--lp-network-stress' }
    if ($MissionTest) { $arguments += '--lp-mission-test' }
    $process = Start-Process -FilePath $Player -ArgumentList $arguments -PassThru -WindowStyle Hidden
    $launched.Add($process)
    Write-Output ("Started {0}, PID {1}" -f $Name,$process.Id)
}
function Read-Smoke([string]$Name) {
    try { Get-Content -LiteralPath (Join-Path $results ($Name + '.json')) -Raw | ConvertFrom-Json } catch { $null }
}
function Wait-Smoke([string]$Name,[scriptblock]$Condition,[int]$Seconds = 25) {
    $deadline = [DateTime]::UtcNow.AddSeconds($Seconds)
    do {
        $snapshot = Read-Smoke $Name
        if ($MissionTest -and $null -ne $snapshot -and $snapshot.mission.error) { throw ('Mission assertion failed on ' + $Name + ': ' + $snapshot.mission.error) }
        if ($null -ne $snapshot -and (& $Condition $snapshot)) { return }
        Start-Sleep -Milliseconds 500
    } while ([DateTime]::UtcNow -lt $deadline)
    throw ('Smoke condition timed out: ' + $Name)
}
try {
    $hostDuration = if ($EnemyTest -or $ReviveTest) { 120 } else { 60 }
    $clientDuration = if ($EnemyTest -or $ReviveTest) { 125 } else { 65 }
    if ($MissionTest) { $hostDuration = 90; $clientDuration = 95 }
    # Abrupt Host process exit can discard a simulated delayed disconnect packet.
    # Keep Clients alive through UTP's 30s disconnect timeout instead of only 5s.
    if ($NetworkStress) { $clientDuration = [Math]::Max($clientDuration, $hostDuration + 40) }
    Start-SmokePlayer 'host' 'host' $hostDuration
    Wait-Smoke 'host' { param($r) $r.state -eq 'Lobby' }
    foreach ($number in 1..3) {
        Start-SmokePlayer 'client' ('client' + $number) $clientDuration
        $expected = $number + 1
        Wait-Smoke 'host' { param($r) $r.maximumMembers -ge $expected }
    }
    Start-SmokePlayer 'client' 'fifth' 8
    if ($NetworkStress) {
        # With loss/delay, transport closure may beat NGO's denial-reason message.
        Wait-Smoke 'fifth' { param($r) -not $r.confirmedConnection -and $r.state -eq 'Error' -and $r.error }
        Write-Output ('Fifth participant could not connect under stress: ' + (Read-Smoke 'fifth').error)
    } else {
        Wait-Smoke 'fifth' { param($r) $r.sawRoomFull }
        Write-Output 'Fifth participant rejected by NGO with Room Full.'
    }
    foreach ($name in @('host','client1','client2','client3')) {
        Wait-Smoke $name { param($r) $r.enteredGame -and $r.maximumPlayers -eq 4 -and $r.movedPlayers -eq 4 -and $r.rotatedPlayers -eq 4 -and $r.jumpedPlayers -eq 4 -and $r.sprintedPlayers -eq 4 } 40
        Write-Output ($name + ': four players moving, rotating, sprinting and jumping observed.')
        if ($LevelTest) {
            Wait-Smoke $name { param($r) $r.levelSelectionSynced -and $r.readyResetObserved -and $r.loadedSelectedScene -and $r.selectedLevel -eq $Level } 15
            Write-Output ($name + ': selected scene loaded; Host choice and Ready reset synchronized.')
        }
        if ($HealthTest) {
            Wait-Smoke $name { param($r) $r.initializedHealthPlayers -eq 4 -and $r.damagedHealthPlayers -eq 4 `
                -and $r.downedHealthPlayers -eq 4 -and $r.resetHealthPlayers -eq 4 -and $r.recoveredMovingPlayers -eq 4 } 25
            Write-Output ($name + ': HP initialized/damaged/downed/reset, movement resumed on four players.')
        }
        if ($CombatTest) {
            Wait-Smoke $name { param($r) $r.maximumTargets -eq 4 -and $r.shootingPlayers -eq 4 -and $r.damagingPlayers -eq 4 `
                -and $r.targetDamageObserved -and $r.targetDownObserved -and $r.targetResetObserved `
                -and $r.downedFireRejected -and $r.invalidAimRejected -and $r.friendlyFireBlocked -and $r.shotSpamLimited } 25
            Write-Output ($name + ': combat, target HP/reset, fire validation, friendly fire and cadence verified.')
        }
        if ($ReviveTest) {
            Wait-Smoke $name { param($r) $r.revive.passed } 65
            Write-Output ($name + ': revive cancellation, completion and team wipe verified.')
        }
        if ($EnemyTest) {
            Wait-Smoke $name { param($r) $r.enemy.passed } 50
            Write-Output ($name + ': robot patrol/chase/attack/retarget/dodge/death/respawn and client gun damage verified.')
        }
        if ($MissionTest) {
            Wait-Smoke $name { param($r) $r.mission.passed } 65
            Write-Output ($name + ': shared objectives, completion, wipe and mission restarts verified.')
        }
    }
    Start-SmokePlayer 'client' 'closed' 8
    if ($NetworkStress) {
        Wait-Smoke 'closed' { param($r) -not $r.confirmedConnection -and $r.state -eq 'Error' -and $r.error }
        Write-Output ('Late participant could not connect under stress: ' + (Read-Smoke 'closed').error)
    } else {
        Wait-Smoke 'closed' { param($r) $r.error -eq 'Room Closed' }
        Write-Output 'Joining after Start Game rejected with Room Closed.'
    }
    Wait-Smoke 'client1' { param($r) $r.sawHostDisconnect -and $r.state -eq 'Error' } 65
    foreach ($process in $launched) { $process.WaitForExit() }
    if ($MissionTest -and (Get-ChildItem -LiteralPath $results -Filter '*.log' | Select-String -Pattern 'NullReferenceException|MissingReferenceException|InvalidOperationException')) {
        throw 'Mission smoke produced application exceptions. Inspect participant logs.'
    }
    $summary = @()
    foreach ($name in @('host','client1','client2','client3')) {
        $r = Read-Smoke $name
        $pass = $r.confirmedConnection -and $r.enteredGame -and $r.maximumMembers -eq 4 -and $r.maximumPlayers -eq 4 `
            -and $r.movedPlayers -eq 4 -and $r.rotatedPlayers -eq 4 -and $r.localOwnedPlayer -and $r.serverAuthority -and $r.cameraBound `
            -and $r.jumpedPlayers -eq 4 -and $r.sprintedPlayers -eq 4 -and -not $r.excessiveJumpHeight
        if ($name -ne 'host') { $pass = $pass -and $r.sawHostDisconnect }
        if ($NetworkStress) { $pass = $pass -and $r.networkStressConfigured -and $r.maximumRttMilliseconds -ge 50 }

        if ($HealthTest) {
            $pass = $pass -and $r.initializedHealthPlayers -eq 4 -and $r.damagedHealthPlayers -eq 4 `
                -and $r.downedHealthPlayers -eq 4 -and $r.resetHealthPlayers -eq 4 -and $r.recoveredMovingPlayers -eq 4 `
                -and -not $r.movedWhileDowned
            if ($name -ne 'host') { $pass = $pass -and $r.clientHealthWritesRejected }
        }
        if ($CombatTest) {
            $pass = $pass -and $r.maximumTargets -eq 4 -and $r.shootingPlayers -eq 4 -and $r.damagingPlayers -eq 4 `
                -and $r.targetDamageObserved -and $r.targetDownObserved -and $r.targetResetObserved `
                -and $r.downedFireRejected -and $r.invalidAimRejected -and $r.friendlyFireBlocked -and $r.shotSpamLimited
        }
        if ($LevelTest) {
            $pass = $pass -and $r.levelSelectionSynced -and $r.readyResetObserved -and $r.loadedSelectedScene -and $r.selectedLevel -eq $Level
            if ($name -ne 'host') { $pass = $pass -and $r.clientLevelChangeRejected }
        }
        if ($EnemyTest) { $pass = $pass -and $r.enemy.passed }
        if ($ReviveTest) { $pass = $pass -and $r.revive.passed }
        if ($MissionTest) { $pass = $pass -and $r.mission.passed }
        $summary += [PSCustomObject]@{ Name=$name; Passed=[bool]$pass; Result=$r }
    }
    $fifthResult = Read-Smoke 'fifth'; $closedResult = Read-Smoke 'closed'
    $fifthPassed = $fifthResult.sawRoomFull; $closedPassed = $closedResult.error -eq 'Room Closed'
    if ($NetworkStress) {
        $fifthPassed = -not $fifthResult.confirmedConnection -and $fifthResult.state -eq 'Error' -and [bool]$fifthResult.error
        $closedPassed = -not $closedResult.confirmedConnection -and $closedResult.state -eq 'Error' -and [bool]$closedResult.error
    }
    $summary += [PSCustomObject]@{Name='fifth';Passed=[bool]$fifthPassed;Result=$fifthResult}
    $summary += [PSCustomObject]@{Name='closed';Passed=[bool]$closedPassed;Result=$closedResult}
    $summary | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $results 'Summary.json') -Encoding UTF8
    $summary | Select-Object Name,Passed | Format-Table -AutoSize
    if ($summary | Where-Object { -not $_.Passed }) { throw 'Some local smoke checks failed. Inspect JSON and logs.' }
} finally {
    # Stop only processes created by this exact test run, never the user's Editor.
    foreach ($process in $launched) { if (-not $process.HasExited) { Stop-Process -Id $process.Id -Force } }
}
