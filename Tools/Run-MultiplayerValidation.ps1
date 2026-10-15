param(
    [ValidateSet('Setup','Repair','Tests','Build','ControllerBuild','HealthSetup','HealthBuild','CombatSetup','CombatBuild')][string]$Stage = 'Setup',
    [string]$UnityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.5.4f1\Editor\Unity.exe'
)
$ErrorActionPreference = 'Stop'
$workspace = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$validation = [IO.Path]::GetFullPath((Join-Path $workspace 'Builds\ValidationProject'))
if (-not $validation.StartsWith($workspace + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Validation directory must be inside this workspace.'
}
if (-not (Test-Path -LiteralPath $UnityEditor)) { throw "Unity Editor missing: $UnityEditor" }
New-Item -ItemType Directory -Path $validation -Force | Out-Null
foreach ($folder in @('Assets','Packages','ProjectSettings')) {
    $destination = Join-Path $validation $folder
    New-Item -ItemType Directory -Path $destination -Force | Out-Null
    Get-ChildItem -LiteralPath (Join-Path $workspace $folder) -Force | Copy-Item -Destination $destination -Recurse -Force
}
$results = Join-Path $workspace 'Builds\ValidationResults'
New-Item -ItemType Directory -Path $results -Force | Out-Null
# Archive earlier evidence before new logs/results are produced.
$history = Join-Path $results ('History\' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff') + '-' + $Stage)
New-Item -ItemType Directory -Path $history -Force | Out-Null
foreach ($name in @(($Stage + '.log'), 'EditMode.xml')) {
    $previous = Join-Path $results $name
    if (Test-Path -LiteralPath $previous) { Copy-Item -LiteralPath $previous -Destination (Join-Path $history $name) }
}
if ($Stage -eq 'Tests') { [IO.File]::WriteAllText((Join-Path $results 'EditMode.xml'), '') }
$log = Join-Path $results ($Stage + '.log')
$arguments = @('-batchmode','-nographics','-projectPath',('"' + $validation + '"'),'-logFile',('"' + $log + '"'))
switch ($Stage) {
    'Setup' { $arguments += @('-quit','-executeMethod','LastProtocol.Editor.MultiplayerSetupTool.BatchSetup') }
    'Repair' { $arguments += @('-quit','-executeMethod','LastProtocol.Editor.MultiplayerSetupTool.BatchRepair') }
    'Tests' { $arguments += @('-runTests','-testPlatform','EditMode','-testFilter','LastProtocol.Tests','-testResults',('"' + (Join-Path $results 'EditMode.xml') + '"')) }
    'Build' { $arguments += @('-quit','-executeMethod','LastProtocol.Editor.MultiplayerSetupTool.BatchBuild') }
    'ControllerBuild' { $arguments += @('-quit','-executeMethod','LastProtocol.Editor.MultiplayerSetupTool.BatchBuildController') }
    'HealthSetup' { $arguments += @('-quit','-executeMethod','LastProtocol.Editor.PlayerHealthSetupTool.BatchInstall') }
    'HealthBuild' { $arguments += @('-quit','-executeMethod','LastProtocol.Editor.MultiplayerSetupTool.BatchBuildHealth') }
    'CombatSetup' { $arguments += @('-quit','-executeMethod','LastProtocol.Editor.CombatSetupTool.BatchInstall') }
    'CombatBuild' { $arguments += @('-quit','-executeMethod','LastProtocol.Editor.MultiplayerSetupTool.BatchBuildCombat') }
}
# Start only a hidden batch Editor, never touch/close the user's open Editor.
$process = Start-Process -FilePath $UnityEditor -ArgumentList $arguments -PassThru -WindowStyle Hidden
Write-Output ("Stage={0} PID={1} Log={2}" -f $Stage,$process.Id,$log)
$process.WaitForExit()
Write-Output ("Unity exit code: " + $process.ExitCode)
exit $process.ExitCode
