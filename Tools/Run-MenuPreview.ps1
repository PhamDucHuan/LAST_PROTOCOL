param([string]$Player = '')
$ErrorActionPreference = 'Stop'
$workspace = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if (-not $Player) { $Player = Join-Path $workspace 'Builds\ValidationProject\Builds\MenuPlayer\LAST_PROTOCOL.exe' }
if (-not (Test-Path -LiteralPath $Player)) { throw 'Build MenuPlayer first.' }
$results = Join-Path $workspace 'Builds\ValidationResults\MenuPreview'
if (Test-Path -LiteralPath $results) {
    $history = Join-Path $workspace ('Builds\ValidationResults\History\' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff') + '-MenuPreview')
    New-Item -ItemType Directory -Path $history -Force | Out-Null
    Get-ChildItem -LiteralPath $results -File | Copy-Item -Destination $history
}
New-Item -ItemType Directory -Path $results -Force | Out-Null
[IO.File]::WriteAllText((Join-Path $results 'Checks.json'), '{}')
$arguments = @('-force-d3d11','-screen-width','1600','-screen-height','900','-screen-fullscreen','0',
    '-logFile',('"' + (Join-Path $results 'Player.log') + '"'),'--lp-menu-preview',('"' + $results + '"'))
$process = Start-Process -FilePath $Player -ArgumentList $arguments -PassThru -WindowStyle Hidden
Write-Output ('Menu graphical preview PID=' + $process.Id)
try {
    if (-not $process.WaitForExit(120000)) { throw 'Menu graphical preview timed out.' }
    $report = Get-Content -Raw -LiteralPath (Join-Path $results 'Checks.json') | ConvertFrom-Json
    $report | ConvertTo-Json -Depth 4
    if ($process.ExitCode -ne 0 -or -not $report.complete -or $report.failures.Count -ne 0) { throw 'Menu preview checks failed. Inspect screenshots/log.' }
} finally { if (-not $process.HasExited) { Stop-Process -Id $process.Id -Force } }
