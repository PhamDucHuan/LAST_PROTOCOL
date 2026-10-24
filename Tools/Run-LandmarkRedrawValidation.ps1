param([ValidateSet('Import','EditMode','Capture','Build')][string]$Stage='Import',[string]$Candidate='Candidate01')
$ErrorActionPreference='Stop'
function Asset-SHA256([string]$path){
 $stream=[IO.File]::OpenRead($path);$algorithm=[Security.Cryptography.SHA256]::Create()
 try{return [BitConverter]::ToString($algorithm.ComputeHash($stream)).Replace('-','')}finally{$stream.Dispose();$algorithm.Dispose()}
}
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$task=Join-Path $root 'Builds/CanThoMap/20261024-LandmarkRedraw'
$clone=Join-Path $root 'Builds/ValidationProject'
if(Test-Path -LiteralPath (Join-Path $clone 'Temp/UnityLockfile')){throw 'Validation Editor is busy'}
foreach($folder in @('Scripts','Tests','Documentation')){
 & robocopy (Join-Path $root ('Assets/_Project/'+$folder)) (Join-Path $clone ('Assets/_Project/'+$folder)) /E /R:1 /W:1 /NFL /NDL /NJH /NJS /NP | Out-Null
 if($LASTEXITCODE -ge 8){throw 'Source sync failed'}
}
if($Stage -eq 'Import'){
 $candidatePath=[IO.Path]::GetFullPath((Join-Path $task $Candidate))
 if(-not $candidatePath.StartsWith($task+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Candidate outside task'}
 $art=Join-Path $clone 'Assets/_Project/Art/Environments/CanThoCampus'
 foreach($name in @('CanThoCampus.fbx','CanThoCampus.layout.json')){Copy-Item -LiteralPath (Join-Path $candidatePath $name) -Destination (Join-Path $art $name) -Force}
 & robocopy (Join-Path $candidatePath 'Textures') (Join-Path $art 'Textures') /E /R:1 /W:1 /NFL /NDL /NJH /NJS /NP | Out-Null
 if($LASTEXITCODE -ge 8){throw 'Texture sync failed'}
}
$arguments=@('-batchmode','-projectPath',('"'+$clone+'"'),'-logFile',('"'+$task+'/'+$Stage+'.log"'),'--lp-landmark-output',('"'+$task+'"'),'--lp-landmark-player','Builds/LandmarkRedrawPlayer')
switch($Stage){
 'Import' {$arguments+=@('-nographics','-quit','-executeMethod','LastProtocol.Editor.CanThoLandmarkSetupTool.BatchApplyRedraw')}
 'EditMode' {$arguments+=@('-nographics','-runTests','-testPlatform','EditMode','-testFilter','LastProtocol.Tests','-testResults',('"'+$task+'/EditMode.xml"'))}
 'Capture' {$arguments+=@('-force-d3d11','-executeMethod','LastProtocol.Editor.CanThoLandmarkSetupTool.BatchCaptureAndExit')}
 'Build' {$arguments+=@('-force-d3d11','-executeMethod','LastProtocol.Editor.CanThoLandmarkSetupTool.BatchBuildAndCaptureAndExit')}
}
$p=Start-Process 'C:/Program Files/Unity/Hub/Editor/6000.5.4f1/Editor/Unity.exe' -ArgumentList $arguments -PassThru -WindowStyle Hidden
'LANDMARK '+$Stage+' PID='+$p.Id
$p.WaitForExit();$p.Refresh();$p.ExitCode | Set-Content -LiteralPath (Join-Path $task ($Stage+'.exit'))
if($Stage -eq 'Build' -and $p.ExitCode -eq 0){
 $log=Get-Content -Raw -LiteralPath (Join-Path $task 'Build.log')
 if($log -notmatch 'SKETCHFAB_LANDMARK_CAPTURE_PASS' -or $log -notmatch 'SKETCHFAB_LANDMARK_BUILD_PASS'){throw 'Combined build/capture did not complete'}
 $images=foreach($name in @('HoiTruongRua','NhaHocA3','NhaThiDauTDTT','HoiTruongRua-Plot','NhaHocA3-Plot','NhaThiDauTDTT-Plot')){
  $file=Get-Item -LiteralPath (Join-Path $task ($name+'.png'));if($file.Length -lt 20000){throw ('Empty capture '+$name)}
  [pscustomobject]@{Name=$file.Name;SHA256=(Asset-SHA256 $file.FullName);Bytes=$file.Length}
 }
 [pscustomobject]@{passed=$true;sourceStage='Build';exitCode=$p.ExitCode;log='Build.log';images=$images}|ConvertTo-Json -Depth 5|Set-Content -LiteralPath (Join-Path $task 'FinalCapture.json') -Encoding utf8
}
exit $p.ExitCode
