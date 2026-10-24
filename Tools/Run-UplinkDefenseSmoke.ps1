param([Parameter(Mandatory=$true)][string]$Player,[ValidateSet(2,4)][int]$Players=2,[string]$OutputDirectory='')
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if(-not $OutputDirectory){$OutputDirectory=Join-Path $root ('Builds/Encounters/20261021-UplinkDefense/NetworkSmoke'+$Players)}
if(Test-Path -LiteralPath $OutputDirectory){throw 'Keep existing evidence; choose a new output directory.'}
New-Item -ItemType Directory -Path $OutputDirectory|Out-Null
$processes=@()
function Read-Result($name){try{Get-Content -Raw -LiteralPath (Join-Path $OutputDirectory ($name+'.json'))|ConvertFrom-Json}catch{$null}}
function Wait-Result($name,[scriptblock]$condition,$seconds){
 $deadline=[DateTime]::UtcNow.AddSeconds($seconds)
 do{$r=Read-Result $name;if($r.defense.error){throw $r.defense.error};if($null -ne $r -and (& $condition $r)){return};Start-Sleep -Milliseconds 500}while([DateTime]::UtcNow -lt $deadline)
 throw ('Timed out: '+$name)
}
try{
 $names=@('host') + @(1..($Players-1)|ForEach-Object {'client'+$_})
 foreach($name in $names){
  $role=if($name -eq 'host'){'host'}else{'client'}
  $duration=if($name -eq 'host'){130}else{135}
  $arguments=@('-batchmode','-nographics','-logFile',('"'+$OutputDirectory+'\'+$name+'.log"'),'--lp-smoke',$role,'--lp-result',('"'+$OutputDirectory+'\'+$name+'.json"'),'--lp-duration',$duration,'--lp-auto-start',$Players,'--lp-start-delay','12','--lp-level','2','--lp-uplink-defense-test')
  $p=Start-Process -FilePath $Player -ArgumentList $arguments -WindowStyle Hidden -PassThru;$processes+=$p
  $name+' PID='+$p.Id
  if($name -eq 'host'){Wait-Result 'host' {param($r)$r.state -eq 'Lobby'} 25}
 }
 foreach($name in $names){Wait-Result $name {param($r)$r.defense.passed} 125}
 foreach($p in $processes){$p.WaitForExit();$p.Refresh();if($p.ExitCode -ne 0){throw ('Player exit '+$p.ExitCode)}}
 if(Get-ChildItem -LiteralPath $OutputDirectory -Filter '*.log'|Select-String -Pattern 'NullReferenceException|MissingReferenceException|InvalidOperationException|Uplink defence stage failed|\[Error\]|Failed to create agent'){throw 'Application errors in defence smoke log.'}
 $hostResult=Read-Result 'host'
 $results=foreach($name in $names){
  $r=Read-Result $name
  $sameWave=$r.defense.firstWave -eq $hostResult.defense.firstWave
  $sameCompletion=$r.defense.completedState -eq $hostResult.defense.completedState
  $sameReset=$r.defense.resetState -eq $hostResult.defense.resetState
  [pscustomobject]@{Name=$name;Passed=($r.defense.passed -and $r.enteredGame -and $r.maximumPlayers -eq $Players -and $r.loadedSelectedScene -and $sameWave -and $sameCompletion -and $sameReset);Result=$r}
 }
 $results|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $OutputDirectory 'Summary.json')
 if($results|Where-Object {-not $_.Passed}){throw 'Defence smoke or participant state comparison failed.'}
 'UPLINK_DEFENCE_NETWORK_SMOKE_PASS '+$Players
}finally{foreach($p in $processes){if(-not $p.HasExited){Stop-Process -Id $p.Id -Force}}}
