param([string]$Player = '',[switch]$FreshCapture,[ValidateRange(0,2)][int]$Level = 1,[switch]$CampusRoute,[switch]$Mission)
$ErrorActionPreference = 'Stop'
$workspace = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if (-not $Player) { $Player = Join-Path $workspace 'Builds\ValidationProject\Builds\MenuPlayer\LAST_PROTOCOL.exe' }
if (-not (Test-Path -LiteralPath $Player)) { throw 'Build MenuPlayer first.' }
$results = Join-Path $workspace 'Builds\ValidationResults\MenuPreview'
if ($Level -eq 2) { $results = Join-Path $workspace 'Builds\ValidationResults\CanThoMenuPreview' }
if (Test-Path -LiteralPath $results) {
    $history = Join-Path $workspace ('Builds\ValidationResults\History\' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff') + '-MenuPreview')
    New-Item -ItemType Directory -Path $history -Force | Out-Null
    Get-ChildItem -LiteralPath $results -File | Copy-Item -Destination $history
}
New-Item -ItemType Directory -Path $results -Force | Out-Null
[IO.File]::WriteAllText((Join-Path $results 'Checks.json'), '{}')
$arguments = @('-force-d3d11','-screen-width','1600','-screen-height','900','-screen-fullscreen','0',
    '-logFile',('"' + (Join-Path $results 'Player.log') + '"'),'--lp-menu-preview',('"' + $results + '"'),'--lp-preview-level',$Level)
if ($CampusRoute) {
    if ($Level -ne 2 -or $FreshCapture) { throw 'CampusRoute requires Level 2 without FreshCapture.' }
    $arguments += '--lp-campus-route-preview'
}
if ($Mission) {
    if ($Level -ne 2 -or $FreshCapture) { throw 'Mission requires Level 2 without FreshCapture.' }
    $arguments += '--lp-mission-preview'
}
$process = Start-Process -FilePath $Player -ArgumentList $arguments -PassThru -WindowStyle Hidden
Write-Output ('Menu graphical preview PID=' + $process.Id)
try {
    if (-not $process.WaitForExit(120000)) { throw 'Menu graphical preview timed out.' }
    $report = Get-Content -Raw -LiteralPath (Join-Path $results 'Checks.json') | ConvertFrom-Json
    $report | ConvertTo-Json -Depth 4
    if ($process.ExitCode -ne 0 -or -not $report.complete -or $report.failures.Count -ne 0) { throw 'Menu preview checks failed. Inspect screenshots/log.' }
    if ($FreshCapture) {
        $captures = @()
        $selectionPage = if ($Level -eq 2) { '03-cantho-selection' } else { '03-yard-selection' }
        foreach ($page in @('01-home','02-training-selection',$selectionPage,'04-lobby','05-lobby-ready-reset','06-join','07-error','08-controls','09-home-4x3')) {
            $pageDirectory = Join-Path $results ('Fresh\' + $page)
            New-Item -ItemType Directory -Path $pageDirectory -Force | Out-Null
            $pageArguments = @('-force-d3d11','-screen-width','1600','-screen-height','900','-screen-fullscreen','0',
                '-logFile',('"' + (Join-Path $pageDirectory 'Player.log') + '"'),'--lp-menu-preview',('"' + $pageDirectory + '"'),'--lp-preview-level',$Level,'--lp-preview-page',$page)
            $pageProcess = Start-Process -FilePath $Player -ArgumentList $pageArguments -PassThru -WindowStyle Hidden
            try {
                if (-not $pageProcess.WaitForExit(60000)) { throw ('Fresh page capture timed out: ' + $page) }
                $pageReport = Get-Content -Raw -LiteralPath (Join-Path $pageDirectory 'Checks.json') | ConvertFrom-Json
                if ($pageProcess.ExitCode -ne 0 -or -not $pageReport.complete -or $pageReport.failures.Count -gt 0) { throw ('Fresh page capture failed: ' + $page) }
                Copy-Item -LiteralPath (Join-Path $pageDirectory ($page + '.png')) -Destination $results -Force
                $captures += [PSCustomObject]@{Page=$page;Passed=$true;Checks=$pageReport.passed.Count}
                Write-Output ('Fresh capture PASS: ' + $page)
            } finally { if (-not $pageProcess.HasExited) { Stop-Process -Id $pageProcess.Id -Force } }
        }
        $captures | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $results 'FreshCaptureChecks.json') -Encoding UTF8
    }
} finally { if (-not $process.HasExited) { Stop-Process -Id $process.Id -Force } }
