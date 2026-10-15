param([string]$Player = '',[switch]$HealthTest,[switch]$CombatTest)
$ErrorActionPreference = 'Stop'
$workspace = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if (-not $Player) {
    $buildFolder = if ($CombatTest) { 'CombatPlayer' } elseif ($HealthTest) { 'HealthPlayer' } else { 'ThirdPersonPlayer' }
    $Player = Join-Path $workspace ('Builds\ValidationProject\Builds\' + $buildFolder + '\LAST_PROTOCOL.exe')
}
if ($CombatTest) { $HealthTest = $true }
if (-not (Test-Path -LiteralPath $Player)) { throw 'Build Windows development MVP first.' }
$results = Join-Path $workspace 'Builds\ValidationResults\Smoke'
if ($HealthTest) { $results = Join-Path $workspace 'Builds\ValidationResults\HealthSmoke' }
if ($CombatTest) { $results = Join-Path $workspace 'Builds\ValidationResults\CombatSmoke' }
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
        if ($null -ne $snapshot -and (& $Condition $snapshot)) { return }
        Start-Sleep -Milliseconds 500
    } while ([DateTime]::UtcNow -lt $deadline)
    throw ('Smoke condition timed out: ' + $Name)
}
try {
    Start-SmokePlayer 'host' 'host' 60
    Wait-Smoke 'host' { param($r) $r.state -eq 'Lobby' }
    foreach ($number in 1..3) {
        Start-SmokePlayer 'client' ('client' + $number) 65
        $expected = $number + 1
        Wait-Smoke 'host' { param($r) $r.maximumMembers -ge $expected }
    }
    Start-SmokePlayer 'client' 'fifth' 8
    Wait-Smoke 'fifth' { param($r) $r.sawRoomFull }
    Write-Output 'Fifth participant rejected by NGO with Room Full.'
    foreach ($name in @('host','client1','client2','client3')) {
        Wait-Smoke $name { param($r) $r.enteredGame -and $r.maximumPlayers -eq 4 -and $r.movedPlayers -eq 4 -and $r.rotatedPlayers -eq 4 -and $r.jumpedPlayers -eq 4 -and $r.sprintedPlayers -eq 4 } 40
        Write-Output ($name + ': four players moving, rotating, sprinting and jumping observed.')
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
    }
    Start-SmokePlayer 'client' 'closed' 8
    Wait-Smoke 'closed' { param($r) $r.error -eq 'Room Closed' }
    Write-Output 'Joining after Start Game rejected with Room Closed.'
    Wait-Smoke 'client1' { param($r) $r.sawHostDisconnect -and $r.state -eq 'Error' } 45
    foreach ($process in $launched) { $process.WaitForExit() }
    $summary = @()
    foreach ($name in @('host','client1','client2','client3')) {
        $r = Read-Smoke $name
        $pass = $r.confirmedConnection -and $r.enteredGame -and $r.maximumMembers -eq 4 -and $r.maximumPlayers -eq 4 `
            -and $r.movedPlayers -eq 4 -and $r.rotatedPlayers -eq 4 -and $r.localOwnedPlayer -and $r.serverAuthority -and $r.cameraBound `
            -and $r.jumpedPlayers -eq 4 -and $r.sprintedPlayers -eq 4 -and -not $r.excessiveJumpHeight
        if ($name -ne 'host') { $pass = $pass -and $r.sawHostDisconnect }
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
        $summary += [PSCustomObject]@{ Name=$name; Passed=[bool]$pass; Result=$r }
    }
    $summary += [PSCustomObject]@{Name='fifth';Passed=[bool](Read-Smoke 'fifth').sawRoomFull;Result=(Read-Smoke 'fifth')}
    $summary += [PSCustomObject]@{Name='closed';Passed=[bool]((Read-Smoke 'closed').error -eq 'Room Closed');Result=(Read-Smoke 'closed')}
    $summary | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $results 'Summary.json') -Encoding UTF8
    $summary | Select-Object Name,Passed | Format-Table -AutoSize
    if ($summary | Where-Object { -not $_.Passed }) { throw 'Some local smoke checks failed. Inspect JSON and logs.' }
} finally {
    # Stop only processes created by this exact test run, never the user's Editor.
    foreach ($process in $launched) { if (-not $process.HasExited) { Stop-Process -Id $process.Id -Force } }
}
