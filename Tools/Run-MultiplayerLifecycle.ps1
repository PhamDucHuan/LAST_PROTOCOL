param([string]$Player = '')
$ErrorActionPreference = 'Stop'
$workspace = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if (-not $Player) { $Player = Join-Path $workspace 'Builds\ValidationProject\Builds\SmokePlayer\LAST_PROTOCOL.exe' }
if (-not (Test-Path -LiteralPath $Player)) { throw 'Build Windows development MVP first.' }
$results = Join-Path $workspace 'Builds\ValidationResults\Lifecycle'
New-Item -ItemType Directory -Path $results -Force | Out-Null
$launched = [Collections.Generic.List[Diagnostics.Process]]::new()
function Start-Lifecycle([string]$Role,[string[]]$ExtraArgs) {
    $json = Join-Path $results ($Role + '.json')
    [IO.File]::WriteAllText($json,'{}')
    $arguments = @('-batchmode','-nographics','-logFile',('"' + (Join-Path $results ($Role + '.log')) + '"'),
        '--lp-smoke',$Role,'--lp-result',('"' + $json + '"'),'--lp-duration','30','--lp-auto-start','0') + $ExtraArgs
    $process = Start-Process -FilePath $Player -ArgumentList $arguments -PassThru -WindowStyle Hidden
    $launched.Add($process)
    Write-Output ("Started lifecycle {0}, PID {1}" -f $Role,$process.Id)
}
function Read-Lifecycle([string]$Role) {
    try { Get-Content -LiteralPath (Join-Path $results ($Role + '.json')) -Raw | ConvertFrom-Json } catch { $null }
}
try {
    Start-Lifecycle 'host' @('--lp-leave-after','18','--lp-recreate-after','22')
    $deadline = [DateTime]::UtcNow.AddSeconds(20)
    do {
        $hostResult = Read-Lifecycle 'host'
        if ($hostResult.state -eq 'Lobby') { break }
        Start-Sleep -Milliseconds 500
    } while ([DateTime]::UtcNow -lt $deadline)
    if ($hostResult.state -ne 'Lobby') { throw 'Host did not enter lobby.' }
    Start-Lifecycle 'client' @('--lp-leave-after','4','--lp-rejoin-after','7')
    foreach ($process in $launched) { $process.WaitForExit() }
    $hostResult = Read-Lifecycle 'host'
    $clientResult = Read-Lifecycle 'client'
    $hostPass = $hostResult.maximumMembers -eq 2 -and $hostResult.departuresObserved -ge 1 `
        -and $hostResult.leaveCompleted -and $hostResult.recreatedRoom -and $hostResult.currentMembers -eq 1 -and $hostResult.managerCountAfterLeave -eq 1
    $clientPass = $clientResult.maximumMembers -eq 2 -and $clientResult.leaveCompleted `
        -and $clientResult.reconnected -and $clientResult.sawHostDisconnect -and $clientResult.managerCountAfterLeave -eq 1
    $summary = @([PSCustomObject]@{Name='host';Passed=[bool]$hostPass;Result=$hostResult},
        [PSCustomObject]@{Name='client';Passed=[bool]$clientPass;Result=$clientResult})
    $summary | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $results 'Summary.json') -Encoding UTF8
    $summary | Select-Object Name,Passed | Format-Table -AutoSize
    if (-not ($hostPass -and $clientPass)) { throw 'Lifecycle checks failed; inspect JSON/logs.' }
} finally {
    foreach ($process in $launched) { if (-not $process.HasExited) { Stop-Process -Id $process.Id -Force } }
}
