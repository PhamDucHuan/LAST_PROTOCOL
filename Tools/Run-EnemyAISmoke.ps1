param([Parameter(Mandatory=$true)][string]$Player,[ValidateSet(2,4)][int]$Players=2,[Parameter(Mandatory=$true)][string]$OutputDirectory,[switch]$Graphical)
$ErrorActionPreference='Stop'
if(Test-Path -LiteralPath $OutputDirectory){throw 'Preserve evidence; choose a new output directory.'}
New-Item -ItemType Directory -Path $OutputDirectory | Out-Null
$processes=@()
function Read-Result($name){try{Get-Content -Raw -LiteralPath (Join-Path $OutputDirectory ($name+'.json')) | ConvertFrom-Json}catch{$null}}
function Wait-Result($name,[scriptblock]$condition,$seconds){
 $deadline=[DateTime]::UtcNow.AddSeconds($seconds)
 do{$r=Read-Result $name;if($r.enemyAI.error){throw $r.enemyAI.error};if($null -ne $r -and (& $condition $r)){return};Start-Sleep -Milliseconds 500}while([DateTime]::UtcNow -lt $deadline)
 throw ('Timed out: '+$name)
}
try{
 $names=@('host')+@(1..($Players-1)|ForEach-Object {'client'+$_})
 foreach($name in $names){
  $role=if($name -eq 'host'){'host'}else{'client'}
  $duration=if($name -eq 'host'){115}else{120}
  $args=@('-batchmode','-nographics','-logFile',('"'+$OutputDirectory+'/'+$name+'.log"'),'--lp-smoke',$role,'--lp-result',('"'+$OutputDirectory+'/'+$name+'.json"'),'--lp-duration',$duration,'--lp-auto-start',$Players,'--lp-start-delay','12','--lp-level','2','--lp-ai-test')
  if($Graphical -and $name -eq 'host'){$args=@($args|Where-Object{$_ -notin @('-batchmode','-nographics')})+@('-force-d3d11','-screen-width','1600','-screen-height','900','-screen-fullscreen','0','--lp-ai-preview-dir',('"'+$OutputDirectory+'"'))}
  $p=Start-Process -FilePath $Player -ArgumentList $args -WindowStyle Hidden -PassThru;$processes+=$p
  $name+' PID='+$p.Id
  if($name -eq 'host'){Wait-Result 'host' {param($r)$r.state -eq 'Lobby'} 25}
 }
 foreach($name in $names){Wait-Result $name {param($r)$r.enemyAI.passed} 115}
 foreach($p in $processes){$p.WaitForExit();$p.Refresh();if($p.ExitCode -ne 0){throw ('Player exit '+$p.ExitCode)}}
 if(Get-ChildItem -LiteralPath $OutputDirectory -Filter '*.log'|Select-String -Pattern 'NullReferenceException|MissingReferenceException|InvalidOperationException|AI stage|\[Error\]|Failed to create agent'){throw 'Application error in Enemy AI log.'}
 $hostResult=Read-Result 'host'
 $rows=foreach($name in $names){$r=Read-Result $name;[pscustomobject]@{Name=$name;Passed=($r.enemyAI.passed -and $r.maximumPlayers -eq $Players -and $r.enemyAI.finalState -eq $hostResult.enemyAI.finalState);Result=$r}}
 $rows|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $OutputDirectory 'Summary.json')
 if($rows|Where-Object {-not $_.Passed}){throw 'Enemy AI state mismatch.'}
 'ENEMY_AI_NETWORK_SMOKE_PASS '+$Players
}finally{foreach($p in $processes){if(-not $p.HasExited){Stop-Process -Id $p.Id -Force}}}
