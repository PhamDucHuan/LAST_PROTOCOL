param([string]$Player='',[string]$OutputDirectory='')
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if(-not $Player){$Player=Join-Path $root 'Builds/ValidationProject/Builds/InteractionPlayer/LAST_PROTOCOL.exe'}
$out=if($OutputDirectory){$OutputDirectory}else{Join-Path $root 'Builds/Interaction/20261021-AdminDataStation/NetworkSmoke'}
if(Test-Path -LiteralPath $out){throw 'Keep existing evidence; use a new output directory for a rerun.'}
New-Item -ItemType Directory -Path $out|Out-Null
$processes=@()
function Read-Result($name){try{Get-Content -Raw -LiteralPath (Join-Path $out ($name+'.json'))|ConvertFrom-Json}catch{$null}}
function Wait-Result($name,[scriptblock]$condition,$seconds){
 $deadline=[DateTime]::UtcNow.AddSeconds($seconds)
 do{$r=Read-Result $name;if($r.interaction.error){throw $r.interaction.error};if($null -ne $r -and (& $condition $r)){return};Start-Sleep -Milliseconds 500}while([DateTime]::UtcNow -lt $deadline)
 throw ('Timed out: '+$name)
}
try{
 foreach($name in @('host','client')){
  $role=if($name -eq 'host'){'host'}else{'client'}
  $duration=if($name -eq 'host'){75}else{80}
  $arguments=@('-batchmode','-nographics','-logFile',('"'+$out+'\'+$name+'.log"'),'--lp-smoke',$role,'--lp-result',('"'+$out+'\'+$name+'.json"'),'--lp-duration',$duration,'--lp-auto-start','2','--lp-start-delay','12','--lp-level','2','--lp-interaction-test')
  $p=Start-Process -FilePath $Player -ArgumentList $arguments -WindowStyle Hidden -PassThru;$processes+=$p
  $name+' PID='+$p.Id
  if($name -eq 'host'){Wait-Result 'host' {param($r)$r.state -eq 'Lobby'} 25}
 }
 foreach($name in @('host','client')){Wait-Result $name {param($r)$r.interaction.passed} 85}
 foreach($p in $processes){$p.WaitForExit();$p.Refresh();if($p.ExitCode -ne 0){throw ('Player exit '+$p.ExitCode)}}
 if(Get-ChildItem -LiteralPath $out -Filter '*.log'|Select-String -Pattern 'NullReferenceException|MissingReferenceException|InvalidOperationException|Interaction stage failed|\[Error\]'){throw 'Application errors in smoke log.'}
 $results=foreach($name in @('host','client')){$r=Read-Result $name;[pscustomobject]@{Name=$name;Passed=($r.interaction.passed -and $r.enteredGame -and $r.maximumPlayers -eq 2 -and $r.loadedSelectedScene);Result=$r}}
 $results|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $out 'Summary.json')
 if($results|Where-Object {-not $_.Passed}){throw 'Interaction smoke failed.'}
 'INTERACTION_NETWORK_SMOKE_PASS'
}finally{foreach($p in $processes){if(-not $p.HasExited){Stop-Process -Id $p.Id -Force}}}
