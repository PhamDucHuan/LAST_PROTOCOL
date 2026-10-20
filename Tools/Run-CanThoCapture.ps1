param(
    [string]$UnityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.5.4f1\Editor\Unity.exe',
    [ValidateSet('d3d11','d3d12')][string]$GraphicsApi = 'd3d12'
)
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$validation = Join-Path $projectRoot 'Builds\ValidationProject'
if (Test-Path -LiteralPath (Join-Path $validation 'Temp\UnityLockfile')) { throw 'The validation Editor is busy; finish its current run first.' }
$output = Join-Path $projectRoot ('Builds\ValidationResults\CanThoMapPreview\' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $output -Force | Out-Null
$arguments = @('-batchmode','-quit',('-force-' + $GraphicsApi),'-projectPath',('"' + $validation + '"'),'-logFile',('"' + (Join-Path $output 'Unity.log') + '"'),'-executeMethod','LastProtocol.Editor.CanThoMapSetupTool.BatchCapture','--lp-map-captures',('"' + $output + '"'))
$process = Start-Process -FilePath $UnityEditor -ArgumentList $arguments -WindowStyle Hidden -PassThru
Write-Output "Unity map preview PID=$($process.Id) Output=$output"
try {
    # A changed runtime assembly can trigger a full graphics Editor recompile.
    $deadline = [DateTime]::UtcNow.AddMinutes(5)
    while (-not $process.HasExited) {
        if ([DateTime]::UtcNow -gt $deadline) { throw 'Unity capture timed out.' }
        $process.WaitForExit(1000) | Out-Null
    }
    if ($process.ExitCode -ne 0) { throw "Capture failed ($($process.ExitCode)); inspect Unity.log." }
    foreach ($name in @('Unity-CanTho-Overview.png','Unity-CanTho-GateB.png','Unity-CanTho-TurtleHall.png','Unity-CanTho-FrontFence.png','Unity-CanTho-SchematicPlan.png')) {
        $file = Get-Item -LiteralPath (Join-Path $output $name)
        if ($file.Length -lt 20000) { throw "Capture has too little image data: $name. Inspect visually." }
    }
    Write-Output 'Captures saved; visual inspection is still required.'
} finally { if (-not $process.HasExited) { Stop-Process -Id $process.Id -Force } }
