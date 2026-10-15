param(
    [string]$Blender = 'D:\Blender\blender.exe',
    [string]$UnityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.5.4f1\Editor\Unity.exe'
)
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$validation = Join-Path $projectRoot 'Builds\ValidationProject'
$results = Join-Path $projectRoot ('Builds\ValidationResults\BlenderConnection\' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff'))
foreach ($executable in @($Blender, $UnityEditor)) {
    if (-not (Test-Path -LiteralPath $executable)) { throw "Executable missing: $executable" }
}
if (-not (Test-Path -LiteralPath (Join-Path $validation 'ProjectSettings\ProjectVersion.txt'))) {
    throw 'Validation copy missing. Run Tools\Run-MultiplayerValidation.ps1 first.'
}
if (Test-Path -LiteralPath (Join-Path $validation 'Temp\UnityLockfile')) {
    throw 'Validation Editor may be open. This probe will not close an existing Editor.'
}
New-Item -ItemType Directory -Path $results -Force | Out-Null
$quotedScript = '"' + (Join-Path $PSScriptRoot 'ExportConnectionProbe.py') + '"'
$p = Start-Process -FilePath $Blender -ArgumentList @('--background','--factory-startup','--python-exit-code','1','--python',$quotedScript,'--',('"' + $results + '"')) -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $results 'blender.log') -RedirectStandardError (Join-Path $results 'blender-errors.log')
Write-Output "Blender PID=$($p.Id) Results=$results"
$p.WaitForExit()
if ($p.ExitCode -ne 0) { throw "Blender failed: $($p.ExitCode). See blender.log and blender-errors.log." }
$probeAssets = Join-Path $validation 'Assets\_BlenderBridgeProbe'
$editorAssets = Join-Path $validation 'Assets\Editor'
New-Item -ItemType Directory -Path $probeAssets,$editorAssets -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $results 'ConnectionProbe.fbx') -Destination $probeAssets -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'UnityConnectionProbe.cs') -Destination (Join-Path $editorAssets 'LastProtocolBlenderConnectionProbe.cs') -Force
$argsForUnity = @('-batchmode','-nographics','-projectPath',('"' + $validation + '"'),'-logFile',('"' + (Join-Path $results 'unity.log') + '"'),'-executeMethod','LastProtocol.EditorValidation.BlenderConnectionProbe.Run','--lp-blender-report',('"' + (Join-Path $results 'unity-result.json') + '"'))
$p = Start-Process -FilePath $UnityEditor -ArgumentList $argsForUnity -WindowStyle Hidden -PassThru
Write-Output "Unity validation PID=$($p.Id)"
$p.WaitForExit()
if ($p.ExitCode -ne 0) { throw "Unity import probe failed: $($p.ExitCode). See unity.log and unity-result.json." }
Get-Content -LiteralPath (Join-Path $results 'blender-result.json'),(Join-Path $results 'unity-result.json')
Write-Output "Blender -> FBX -> Unity probe complete. Results=$results"
