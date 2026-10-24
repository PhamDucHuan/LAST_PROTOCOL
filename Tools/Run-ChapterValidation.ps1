param([ValidateSet('Install','EditMode','Build','Idempotence')][string]$Stage='Install',[switch]$InitialSync)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$vp=Join-Path $root 'Builds/ValidationProject'
$task=Join-Path $root 'Builds/Pacing/20261022-Map1'
$before=@{}
if($Stage -eq 'Idempotence'){
 foreach($file in Get-ChildItem (Join-Path $vp 'Assets/_Project') -Recurse -File | Where-Object{$_.Extension -in @('.asset','.prefab','.unity','.fbx')}){$before[$file.FullName]=(Get-FileHash -LiteralPath $file.FullName).Hash}
}
if($InitialSync){
 foreach($folder in @('Assets','Packages','ProjectSettings')){
  & robocopy (Join-Path $root $folder) (Join-Path $vp $folder) /E /R:1 /W:1 /NFL /NDL /NJH /NJS /NP | Out-Null
  if($LASTEXITCODE -ge 8){throw 'Initial sync failed'}
 }
}else{
 foreach($folder in @('Scripts','Tests','Documentation')){
  & robocopy (Join-Path $root ('Assets/_Project/'+$folder)) (Join-Path $vp ('Assets/_Project/'+$folder)) /E /R:1 /W:1 /NFL /NDL /NJH /NJS /NP | Out-Null
  if($LASTEXITCODE -ge 8){throw 'Source sync failed'}
 }
}
$arguments=@('-batchmode','-nographics','-projectPath',('"'+$vp+'"'),'-logFile',('"'+$task+'/'+$Stage+'.log"'))
switch($Stage){
 'Install' {$arguments+=@('-executeMethod','LastProtocol.Editor.ChapterSetupTool.BatchInstallAndExit')}
 'Idempotence' {$arguments+=@('-runTests','-testPlatform','EditMode','-testFilter','LastProtocol.Tests.ChapterTests.InstallerIsIdempotent','-testResults',('"'+$task+'/Idempotence.xml"'))}
 'Build' {$arguments+=@('-quit','-executeMethod','LastProtocol.Editor.MultiplayerSetupTool.BatchBuildChapter1')}
 'EditMode' {$arguments+=@('-runTests','-testPlatform','EditMode','-testFilter','LastProtocol.Tests','-testResults',('"'+$task+'/EditMode.xml"'))}
}
$p=Start-Process 'C:/Program Files/Unity/Hub/Editor/6000.5.4f1/Editor/Unity.exe' -ArgumentList $arguments -PassThru -WindowStyle Hidden
'AI '+$Stage+' PID='+$p.Id
$p.WaitForExit();$p.Refresh();$p.ExitCode | Set-Content -LiteralPath (Join-Path $task ($Stage+'.exit'))
if($Stage -eq 'Idempotence' -and $p.ExitCode -eq 0){
 $changed=@($before.Keys | Where-Object{(Get-FileHash -LiteralPath $_).Hash -ne $before[$_]})
 [pscustomobject]@{passed=($changed.Count -eq 0);checked=$before.Count;changed=$changed}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $task 'Idempotence.json')
 if($changed.Count){throw 'Idempotent installation changed existing assets'}
}
exit $p.ExitCode
