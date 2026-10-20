param(
    [Parameter(Mandatory=$true)][string]$Player,
    [ValidateSet(2,3)][int]$Players = 2
)
$ErrorActionPreference = 'Stop'
$workspace = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if (-not (Test-Path -LiteralPath $Player -PathType Leaf)) { throw 'Mission Development player is missing.' }
$results = Join-Path $workspace ('Builds\ValidationResults\MissionPartySmoke' + $Players)
New-Item -ItemType Directory -Path $results -Force | Out-Null
$previous = @(Get-ChildItem -LiteralPath $results -File)
if ($previous.Count -gt 0) {
    $archive = Join-Path $workspace ('Builds\ValidationResults\History\' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff') + '-MissionParty')
    New-Item -ItemType Directory -Path $archive -Force | Out-Null
    $previous | Copy-Item -Destination $archive
}
[IO.File]::WriteAllText((Join-Path $results 'Summary.json'), '[]')
$launched = [Collections.Generic.List[Diagnostics.Process]]::new()
function Read-PartyResult([string]$Name) {
    try { Get-Content -LiteralPath (Join-Path $results ($Name + '.json')) -Raw | ConvertFrom-Json } catch { $null }
}
function Wait-PartyResult([string]$Name,[scriptblock]$Condition,[int]$Seconds = 20) {
    $deadline = [DateTime]::UtcNow.AddSeconds($Seconds)
    do {
        $snapshot = Read-PartyResult $Name
        if ($null -ne $snapshot -and $snapshot.mission.error) { throw ('Mission assertion: ' + $snapshot.mission.error) }
        if ($null -ne $snapshot -and (& $Condition $snapshot)) { return }
        Start-Sleep -Milliseconds 500
    } while ([DateTime]::UtcNow -lt $deadline)
    throw ('Party smoke timeout: ' + $Name)
}
function Start-PartyPlayer([string]$Role,[string]$Name,[int]$Duration) {
    $resultPath = Join-Path $results ($Name + '.json')
    [IO.File]::WriteAllText($resultPath, '{}')
    $arguments = @('-batchmode','-nographics','-logFile',('"' + (Join-Path $results ($Name + '.log')) + '"'),
        '--lp-smoke',$Role,'--lp-result',('"' + $resultPath + '"'),'--lp-duration',$Duration,
        '--lp-auto-start',$Players,'--lp-start-delay','12','--lp-level','2','--lp-mission-test')
    $process = Start-Process -FilePath $Player -ArgumentList $arguments -PassThru -WindowStyle Hidden
    $launched.Add($process)
    Write-Output ('Party ' + $Players + ': ' + $Name + ' PID=' + $process.Id)
}
try {
    Start-PartyPlayer 'host' 'host' 65
    Wait-PartyResult 'host' { param($r) $r.state -eq 'Lobby' }
    $names = @('host')
    for ($index = 1; $index -lt $Players; $index++) {
        $name = 'client' + $index; $names += $name
        Start-PartyPlayer 'client' $name 70
        $expected = $index + 1
        Wait-PartyResult 'host' { param($r) $r.maximumMembers -ge $expected }
    }
    foreach ($name in $names) { Wait-PartyResult $name { param($r) $r.mission.passed } 50 }
    foreach ($process in $launched) { $process.WaitForExit() }
    if (Get-ChildItem -LiteralPath $results -Filter '*.log' | Select-String -Pattern 'NullReferenceException|MissingReferenceException|InvalidOperationException') {
        throw 'Mission party smoke produced application exceptions. Inspect participant logs.'
    }
    $summary = foreach ($name in $names) {
        $result = Read-PartyResult $name
        $passed = $result.confirmedConnection -and $result.enteredGame -and $result.maximumMembers -eq $Players `
            -and $result.maximumPlayers -eq $Players -and $result.mission.passed -and $result.loadedSelectedScene
        if ($name -ne 'host') { $passed = $passed -and $result.sawHostDisconnect }
        [PSCustomObject]@{ Name=$name; Passed=[bool]$passed; Result=$result }
    }
    $summary | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $results 'Summary.json') -Encoding UTF8
    $summary | Select-Object Name,Passed | Format-Table -AutoSize
    if ($summary | Where-Object { -not $_.Passed }) { throw 'Mission party smoke failed.' }
} finally {
    foreach ($process in $launched) { if (-not $process.HasExited) { Stop-Process -Id $process.Id -Force } }
}
