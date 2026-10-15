param([string]$Blender = 'D:\Blender\blender.exe', [switch]$Render)
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$output = Join-Path $projectRoot ('Builds\CanThoMap\' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff'))
if (-not (Test-Path -LiteralPath $Blender)) { throw "Blender missing: $Blender" }
New-Item -ItemType Directory -Path $output -Force | Out-Null
$arguments = @('--background','--factory-startup','--python-exit-code','1','--python',('"' + (Join-Path $PSScriptRoot 'BuildCanThoCampus.py') + '"'),'--',('"' + $output + '"'))
if ($Render) { $arguments += '--render' }
$process = Start-Process -FilePath $Blender -ArgumentList $arguments -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $output 'Blender.log') -RedirectStandardError (Join-Path $output 'Blender-errors.log')
Write-Output "Blender PID=$($process.Id) Output=$output"
$process.WaitForExit()
if ($process.ExitCode -ne 0) { throw "Blender failed ($($process.ExitCode)); inspect logs in $output" }
Get-Content -LiteralPath (Join-Path $output 'BuildStats.json')
Write-Output 'New Blender source, FBX and layout are ready. Existing Unity art was not overwritten.'
