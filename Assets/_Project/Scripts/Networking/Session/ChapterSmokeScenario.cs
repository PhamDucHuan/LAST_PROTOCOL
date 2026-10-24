#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Linq;
using Unity.Netcode.Components;
using UnityEngine;
namespace LastProtocol
{
    [Serializable]public sealed class ChapterSmokeReport
    {
        public uint checks;
        public int clockMultiplier=60,maximumAlive;
        public bool passed,metadataUnique,authorityCorrect=true,clientWritesDenied,sawSplit,sawRetry,sawWaves;
        public string finalState,error;
    }
    public sealed class ChapterSmokeScenario
    {
        public const uint Expected=(1u<<16)-1;
        public readonly ChapterSmokeReport Report=new();
        private int previous=-1,localStage=-1,screenshotStage=-1;
        private string previousDevice,localDevice;
        private float at,nextSend,heldAt;
        private bool restarted;
        public void Tick(MultiplayerSession session,NetworkPlayerController[] players,float time)
        {
            if(session?.Room==null||players.Length<2||time<5)return;
            var mission=session.Room.GetComponent<NetworkMissionState>();var op=session.Room.GetComponent<ChapterOperation>();var chip=session.Room.GetComponent<NetworkChip>();
            if(!mission.IsLongChapter||op==null){Fail("Production long chapter not selected");return;}
            var ordered=players.OrderBy(x=>session.Room.GetSlot(x.OwnerClientId)).ToArray();var carrier=ordered[0];var technician=ordered[^1];
            var nodes=UnityEngine.Object.FindObjectsByType<DataStation>().Where(x=>x.IsSpawned).ToArray();
            var robots=UnityEngine.Object.FindObjectsByType<RobotEnemy>().Where(x=>x.IsSpawned).ToArray();
            var state=mission.Snapshot;int stage=op.TestStage.Value;
            Report.checks=op.TestChecks.Value;Report.maximumAlive=Math.Max(Report.maximumAlive,robots.Count(x=>!x.Snapshot.Health.IsDowned));
            Report.sawWaves|=op.Snapshot.Waves>0;Report.sawSplit|=state.ObjectiveIndex==8&&state.Inside==2;
            Report.sawRetry|=state.Attempt==2;
            Report.metadataUnique|=nodes.Length==11&&nodes.Select(x=>x.stationId).Distinct().Count()==11&&ChapterPlan.DeviceIds.All(id=>nodes.Any(x=>x.stationId==id));
            Report.authorityCorrect&=robots.All(x=>session.IsHost?x.Snapshot.Health.IsDowned||x.Agent.enabled&&x.Agent.isOnNavMesh:!x.Agent.enabled);
            if(!session.IsHost)Report.clientWritesDenied=!op.TestTryClientWrite()&&!mission.TestTryClientWrite()&&nodes.All(x=>!x.TestTryClientWrite());
            if(stage==17)
            {
                Report.finalState=$"{state.Attempt}:{state.Phase}:{state.ObjectiveIndex}:{state.CompletedStations}:{chip.Snapshot.Location}:"+string.Join(";",nodes.OrderBy(x=>x.stationId).Select(x=>$"{x.stationId}:{x.Snapshot.Completed}:{x.Snapshot.Completions}"));
                Report.passed=Report.checks==Expected&&state.Phase==MissionPhase.Completed&&Report.metadataUnique&&Report.maximumAlive<=3&&Report.authorityCorrect&&Report.sawWaves&&Report.sawSplit&&Report.sawRetry&&(session.IsHost||Report.clientWritesDenied);return;
            }
            if(Report.error!=null||!Report.metadataUnique)return;
            string desired=stage==3?DataStation.UplinkId:stage==6||stage==8?ChapterPlan.DeviceIds[0]:stage==9?ChapterPlan.DeviceIds[1]:
                stage==11?ChapterPlan.DeviceIds.Skip(2).Take(2).FirstOrDefault(id=>nodes.Single(x=>x.stationId==id).Snapshot.Completed==false):
                stage==14?ChapterPlan.DeviceIds.Skip(4).Take(2).FirstOrDefault(id=>nodes.Single(x=>x.stationId==id).Snapshot.Completed==false):null;
            var terminal=desired!=null?nodes.Single(x=>x.stationId==desired):null;
            if(session.IsHost)
            {
                mission.TestClockSteps=60;foreach(var enemy in robots)enemy.DebugPaused=true;
                if(stage==0){op.TestStage.Value=1;return;}
                if(stage!=previous)
                {
                    previous=stage;at=time;heldAt=state.HeldSeconds;previousDevice=null;restarted=false;
                    foreach(var p in ordered){p.GetComponent<PlayerInteraction>().ResetRoundServer();if(stage!=7)p.GetComponent<PlayerHealth>().DebugResetHealthServer();Warp(p,new Vector3(40+session.Room.GetSlot(p.OwnerClientId),1.1f,-35));}
                    if(stage==2)mission.TestPrepareAIEncounter();
                    if(stage==3)Warp(carrier,nodes.Single(x=>x.stationId==DataStation.UplinkId).transform.position+new Vector3(0,1.1f,-2));
                    if(stage==5||stage==10||stage==15)Warp(carrier,mission.Definition.objectives[state.ObjectiveIndex].center+new Vector3(0,1.1f,1));
                    if(stage==7)foreach(var p in ordered)p.GetComponent<PlayerHealth>().TryApplyDamageServer(int.MaxValue);
                    if(stage==12||stage==13)
                    {Warp(carrier,mission.Definition.objectives[8].center+new Vector3(0,1.1f,1));if(stage==13)Warp(technician,mission.Definition.objectives[8].secondaryCenter+new Vector3(0,1.1f,1));}
                    if(stage==16)for(int i=0;i<ordered.Length;i++)Warp(ordered[i],new Vector3((i-1.5f)*.8f,1.1f,-40));
                }
                if(terminal!=null&&desired!=previousDevice)
                {previousDevice=desired;var actor=stage==3?carrier:technician;actor.GetComponent<PlayerInteraction>().ResetRoundServer();Warp(actor,terminal.transform.position+new Vector3(0,1.1f,-1.8f));}
                Physics.SyncTransforms();float elapsed=time-at;
                if(stage==7&&elapsed>1&&!restarted&&mission.Snapshot.Phase==MissionPhase.Failed)restarted=session.Room.RestartRoundServer();
                if((stage==12&&elapsed>1||stage==15&&elapsed>2)&&screenshotStage!=stage&&SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null)
                {
                    var args=Environment.GetCommandLineArgs();int flag=Array.IndexOf(args,"--lp-chapter-preview-dir");
                    if(flag>=0&&flag+1<args.Length)session.StartCoroutine(EnemyAIPreview.Capture(args[flag+1],"Chapter-Stage-"+stage,mission.Definition.objectives[state.ObjectiveIndex].center));screenshotStage=stage;
                }
                float wait=stage switch{1 or 2=>.8f,3=>6,4=>3.5f,5 or 10=>4,6 or 9=>5.2f,7=>2.5f,8=>1.5f,11 or 14=>11,12=>2,13=>4.8f,15=>5,16=>11,_=>1};
                if(elapsed>wait)
                {
                    bool ok=stage switch{
                        1=>mission.Definition.objectives.Length==12&&nodes.Length==11,
                        2=>state.ObjectiveIndex==3&&state.CompletedStations==15&&chip.Snapshot.TutorialComplete,
                        3=>state.ObjectiveIndex==4&&state.CompletedStations==31&&mission.Checkpoint.ObjectiveIndex==4,
                        4=>state.ObjectiveIndex==4&&Mathf.Abs(state.HeldSeconds-heldAt)<.01f&&op.Snapshot.Waves>0&&op.Snapshot.Alive==3,
                        5=>state.ObjectiveIndex==5&&mission.Checkpoint.HeldSeconds==0,
                        6=>state.ObjectiveIndex==5&&nodes.Single(x=>x.stationId==ChapterPlan.DeviceIds[0]).Snapshot.Completions==1,
                        7=>restarted&&state.Attempt==2&&state.ObjectiveIndex==5&&state.CompletedStations==63,
                        8=>state.CompletedStations==63&&nodes.Single(x=>x.stationId==ChapterPlan.DeviceIds[0]).Snapshot.Completions==1,
                        9=>state.ObjectiveIndex==6&&state.CompletedStations==127,
                        10=>state.ObjectiveIndex==7,
                        11=>state.ObjectiveIndex==8&&state.CompletedStations==511,
                        12=>state.ObjectiveIndex==8&&state.HeldSeconds==0,
                        13=>state.ObjectiveIndex==9&&Report.sawSplit,
                        14=>state.ObjectiveIndex==10&&state.CompletedStations==2047,
                        15=>state.ObjectiveIndex==11,
                        16=>state.Phase==MissionPhase.Completed&&nodes.All(x=>x.Snapshot.Completed&&x.Snapshot.Completions==1)&&robots.Length==0,
                        _=>false};
                    if(!ok){Fail($"Chapter stage {stage} failed objective={state.ObjectiveIndex} progress={state.HeldSeconds} mask={state.CompletedStations} waves={op.Snapshot.Waves} alive={op.Snapshot.Alive}");return;}
                    op.TestChecks.Value|=1u<<(stage-1);op.TestStage.Value++;
                }
            }
            if(localStage!=stage||localDevice!=desired)
            {localStage=stage;localDevice=desired;nextSend=time+.4f;foreach(var p in ordered)if(p.IsOwner)p.GetComponent<PlayerInteraction>().TestRelease();}
            if(time<nextSend)return;nextSend=time+.2f;
            var who=stage==3?carrier:technician;
            if(who.IsOwner&&terminal!=null&&(stage==3||stage==6||stage==8||stage==9||stage==11||stage==14))who.GetComponent<PlayerInteraction>().TestHold(InteractionKind.DataStation,terminal.NetworkObjectId);
        }
        private void Fail(string error){Report.error=error;Debug.LogError(error);}
        private static void Warp(NetworkPlayerController p,Vector3 point)
        {var c=p.GetComponent<CharacterController>();c.enabled=false;p.GetComponent<NetworkTransform>().Teleport(point,Quaternion.identity,p.transform.localScale);p.GetComponent<PlayerMovement>().ResetMotion();c.enabled=true;}
    }
}
#endif
