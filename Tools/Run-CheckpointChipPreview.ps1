param([Parameter(Mandatory=$true)][string]$Player,[Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference='Stop'
if(Test-Path -LiteralPath $OutputDirectory){throw 'Preserve previous preview evidence.'}
New-Item -ItemType Directory -Path $OutputDirectory|Out-Null
$hostProcess=$null;$clientProcess=$null
try{
 $hostArgs=@('-force-d3d11','-screen-width','1600','-screen-height','900','-screen-fullscreen','0','-logFile',('"'+$OutputDirectory+'/Host.log"'),'--lp-menu-preview',('"'+$OutputDirectory+'"'),'--lp-preview-level','2','--lp-checkpoint-chip-preview')
 $hostProcess=Start-Process -FilePath $Player -ArgumentList $hostArgs -WindowStyle Hidden -PassThru
 $deadline=[DateTime]::UtcNow.AddSeconds(35)
 while(-not(Test-Path -LiteralPath (Join-Path $OutputDirectory 'ReadyForClient.txt'))){if($hostProcess.HasExited -or [DateTime]::UtcNow -gt $deadline){throw 'Preview Host did not become ready'};Start-Sleep -Milliseconds 300}
 $clientArgs=@('-batchmode','-nographics','-logFile',('"'+$OutputDirectory+'/Client.log"'),'--lp-smoke','client','--lp-result',('"'+$OutputDirectory+'/Client.json"'),'--lp-duration','100','--lp-auto-start','0','--lp-level','2','--lp-chip-preview-client')
 $clientProcess=Start-Process -FilePath $Player -ArgumentList $clientArgs -WindowStyle Hidden -PassThru
 'Graphical Host PID='+$hostProcess.Id+' Remote client PID='+$clientProcess.Id
 if(-not $hostProcess.WaitForExit(120000)){throw 'Checkpoint/chip preview timed out'}
 $report=Get-Content -Raw -LiteralPath (Join-Path $OutputDirectory 'Checks.json')|ConvertFrom-Json
 [pscustomobject]@{complete=$report.complete;passed=$report.passed.Count;failures=$report.failures}|ConvertTo-Json -Depth 4
 if($hostProcess.ExitCode -ne 0 -or -not $report.complete -or $report.failures.Count -ne 0){throw 'Checkpoint/chip graphical checks failed'}
 if(Get-ChildItem -LiteralPath $OutputDirectory -Filter '*.log' | Select-String -Pattern 'NullReferenceException|MissingReferenceException|InvalidOperationException|IndexOutOfRangeException|\[Error\]'){throw 'Application exception in preview participant logs'}
 'CHECKPOINT_CHIP_GRAPHICAL_PASS '+$report.passed.Count
}finally{foreach($p in @($hostProcess,$clientProcess)){if($null -ne $p -and -not $p.HasExited){Stop-Process -Id $p.Id -Force}}}
