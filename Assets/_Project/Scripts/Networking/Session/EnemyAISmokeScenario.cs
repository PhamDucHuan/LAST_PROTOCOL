#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Linq;
using Unity.Netcode.Components;
using UnityEngine;
namespace LastProtocol
{
    [Serializable] public sealed class EnemyAISmokeReport
    {
        public uint checks;
        public bool passed, authorityCorrect=true, clientWritesDenied, sawWindup, sawCapture, sawTrace, sawModifier, remoteShot, sawRetry, sawHacked;
        public string error, finalState;
    }
    // Skips quest setup only. Decisions, movement, attacks, chip and retry use production rules;
    // shooting and recovery use actual owner RPCs from the remote process.
    public sealed class EnemyAISmokeScenario
    {
        public const uint Expected=(1u<<16)-1;
        public readonly EnemyAISmokeReport Report=new();
        private int previous=-1, localStage=-1;
        private float at,nextSend;
        private Vector3 origin,memoryPosition;
        private string waveIds;
        private int healthBefore, allyBefore;
        private GameObject wall;
        private bool dodged, localFired;
        private int screenshotStage=-1;
        public void Tick(MultiplayerSession session,NetworkPlayerController[] players,float time)
        {
            if(session?.Room==null||time<5||players.Length<2)return;
            var mission=session.Room.GetComponent<NetworkMissionState>();var director=session.Room.GetComponent<EnemyDirector>();
            var chip=session.Room.GetComponent<NetworkChip>();var encounter=session.Room.GetComponent<UplinkEncounter>();
            if(director==null){Fail("Director missing");return;}
            int stage=director.TestStage.Value;
            var ordered=players.OrderBy(x=>session.Room.GetSlot(x.OwnerClientId)).ToArray();
            var carrier=ordered[0];var defender=ordered[^1];
            var robots=UnityEngine.Object.FindObjectsByType<RobotEnemy>().OrderBy(x=>x.NetworkObjectId).ToArray();
            var enemy=robots.FirstOrDefault();
            Report.checks=director.TestChecks.Value;
            Report.authorityCorrect &= robots.All(x=>session.IsHost?x.Snapshot.Health.IsDowned||x.Agent.enabled&&x.Agent.isOnNavMesh:!x.Agent.enabled);
            Report.sawWindup |= robots.Any(x=>x.Snapshot.Mode==EnemyMode.Windup);
            Report.sawHacked |= robots.Any(x=>x.Snapshot.Mode==EnemyMode.Hacked);
            Report.sawCapture |= chip.Snapshot.Location==ChipLocation.Captured;
            Report.sawTrace |= chip.Snapshot.Trace>0;
            Report.sawModifier |= director.Snapshot.SelectionLocked&&director.Snapshot.Modifier==AdaptiveModifier.AreaDenial;
            Report.remoteShot |= defender.GetComponent<NetworkHitscanWeapon>().ObservedDamagingHits>0;
            Report.sawRetry |= mission.Snapshot.Attempt==2;
            if(!session.IsHost)Report.clientWritesDenied=!director.TestTryClientWrite()&&robots.All(x=>!x.TryApplyDamageServer(25)&&!x.GetComponent<EnemyBrain>().DisableServer(1));
            if(stage==17)
            {
                Report.finalState=$"{mission.Snapshot.Attempt}:{mission.Snapshot.CompletedStations}:{mission.Snapshot.ObjectiveIndex}:{chip.Snapshot.Location}:{chip.Snapshot.Trace}:{encounter.Snapshot.Spawned}:{encounter.Snapshot.Activations}:{director.Snapshot.MissionMeter}";
                Report.passed=Report.checks==Expected&&Report.authorityCorrect&&Report.sawWindup&&Report.sawCapture&&Report.sawTrace&&Report.sawModifier&&Report.remoteShot&&Report.sawRetry&&(session.IsHost||Report.clientWritesDenied);
                return;
            }
            if(Report.error!=null)return;
            if(session.IsHost)
            {
                if(stage==0){director.TestStage.Value=1;return;}
                foreach(var r in robots)r.DebugPaused=stage==1||stage==8||stage==13||stage==14||stage==16||r!=enemy;
                if(stage!=previous)
                {
                    previous=stage;at=time;dodged=false;
                    foreach(var p in ordered){p.GetComponent<PlayerInteraction>().ResetRoundServer();if(stage<12)p.GetComponent<PlayerHealth>().DebugResetHealthServer();}
                    if(stage==1)
                    {
                        for(int i=0;i<ordered.Length;i++)Warp(ordered[i],new Vector3(30+i*.8f,1.1f,-26));
                        mission.TestPrepareAIEncounter();
                    }
                    if(stage>=2&&enemy!=null)
                    {
                        if(stage==2){waveIds=Ids(robots);enemy.DebugWarpServer(new Vector3(56,0,-44));enemy.transform.rotation=Quaternion.Euler(0,270,0);origin=enemy.transform.position;}
                        if(stage==3)director.EmitNoiseServer(new Vector3(60,0,-44),EnemyNoise.Decoy);
                        if(stage==4){enemy.DebugWarpServer(new Vector3(56,0,-44));enemy.transform.rotation=Quaternion.Euler(0,90,0);Warp(carrier,new Vector3(61,1.1f,-44));}
                        if(stage==5)
                        {
                            enemy.DebugWarpServer(new Vector3(56,0,-44)); memoryPosition=enemy.Brain.Memory.Position;
                            wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="AI vision cover fixture";
                            wall.transform.position=new Vector3(59,1.5f,-44);wall.transform.localScale=new Vector3(.3f,4,6);
                            // Isolate sight from movement: temporary test cover is absent from the baked NavMesh.
                            enemy.Agent.speed=0;
                            Warp(carrier,new Vector3(62,1.1f,-44));
                        }
                        if(stage==6){enemy.Agent.speed=enemy.Brain.CurrentProfile.speed;if(wall!=null)UnityEngine.Object.Destroy(wall);foreach(var p in ordered)Warp(p,new Vector3(30,1.1f,-26));}
                        if(stage==7)
                        {
                            enemy.Brain.AssignRoleServer(director.RoleProfile(EnemyRole.Suppressor));enemy.DebugWarpServer(new Vector3(56,0,-44));enemy.transform.rotation=Quaternion.Euler(0,90,0);
                            Warp(carrier,new Vector3(61,1.1f,-44));healthBefore=carrier.GetComponent<PlayerHealth>().Snapshot.Current;
                        }
                        if(stage==8||stage==13)
                        {enemy.DebugWarpServer(new Vector3(56,0,-44));Warp(defender,new Vector3(58.5f,1.1f,-44));if(stage==13)Warp(carrier,new Vector3(60,1.1f,-41));healthBefore=enemy.Snapshot.Health.Current;}
                        if(stage==9)
                        {
                            enemy.Brain.AssignRoleServer(director.RoleProfile(EnemyRole.Support));robots[1].DebugWarpServer(new Vector3(53,0,-44));
                            robots[1].TryApplyDamageServer(25);allyBefore=robots[1].Snapshot.Health.Current;
                            foreach(var p in ordered)Warp(p,new Vector3(30,1.1f,-26));
                        }
                        if(stage==10){enemy.Brain.DisableServer(2);origin=enemy.transform.position;}
                        if(stage==11){enemy.Brain.HackServer(2);origin=enemy.transform.position;}
                        if(stage==12||stage==15)
                        {
                            if(stage==15)carrier.GetComponent<PlayerHealth>().DebugResetHealthServer();
                            var dropping=stage==12?carrier:defender;
                            Warp(dropping,new Vector3(58,1.1f,-44));
                            if(stage==15)Warp(carrier,new Vector3(64,1.1f,-44));else Warp(defender,new Vector3(64,1.1f,-44));
                            enemy.Brain.AssignRoleServer(director.RoleProfile(EnemyRole.Hunter));enemy.DebugWarpServer(new Vector3(56,0,-44));enemy.transform.rotation=Quaternion.Euler(0,90,0);
                            dropping.GetComponent<PlayerHealth>().TryApplyDamageServer(int.MaxValue);
                        }
                        if(stage==14){var drop=chip.Snapshot.DropPosition+new Vector3(1,0,-.3f);drop.y=1.1f;Warp(defender,drop);}
                    }
                    Physics.SyncTransforms();
                }
                float elapsed=time-at;
                if((stage==4&&elapsed>3||stage==12&&chip.Snapshot.Location==ChipLocation.Captured)&&screenshotStage!=stage
                    &&SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null)
                {
                    var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"--lp-ai-preview-dir");
                    if(index>=0&&index+1<args.Length)session.StartCoroutine(EnemyAIPreview.Capture(args[index+1],"AI-Stage-"+stage));
                    screenshotStage=stage;
                }
                if(stage==7&&!dodged&&enemy.Snapshot.Mode==EnemyMode.Windup)
                {dodged=true;Warp(carrier,new Vector3(61,1.1f,-41));}
                if(stage==15&&enemy!=null&&chip.IsEnemyHolder(enemy.NetworkObjectId))enemy.DebugPaused=true;
                float wait=stage switch{1=>1,2=>8,3=>3,4=>5,5=>2,6=>9,7=>2.5f,8=>1.6f,9=>2,10=>1,11=>3,12=>7,13=>1.6f,14=>3.5f,15=>18,16=>1.5f,_=>2};
                if(stage==16&&mission.Snapshot.Phase==MissionPhase.Failed)session.Room.RestartRoundServer();
                if(elapsed>wait)
                {
                    bool ok=stage switch{
                        1=>robots.Length==3&&robots.All(x=>x.HasTacticalAI)&&encounter.Snapshot.Activations==1,
                        2=>enemy.Snapshot.TargetClientId==EnemyState.NoTarget&&carrier.GetComponent<PlayerHealth>().Snapshot.Current==100,
                        3=>enemy.Brain.Memory.Confidence(Time.realtimeSinceStartupAsDouble)>0&&Vector3.Distance(origin,enemy.transform.position)>.4f&&Report.sawModifier,
                        4=>enemy.Snapshot.SuccessfulAttacks>0&&carrier.GetComponent<PlayerHealth>().Snapshot.Current<100,
                        5=>enemy.Brain.Memory.Position==memoryPosition&&enemy.Brain.Memory.Confidence(Time.realtimeSinceStartupAsDouble)<.9f,
                        6=>enemy.Brain.Memory.Confidence(Time.realtimeSinceStartupAsDouble)==0,
                        7=>dodged&&carrier.GetComponent<PlayerHealth>().Snapshot.Current==healthBefore,
                        8=>enemy.Snapshot.Health.Current<healthBefore&&Report.remoteShot&&director.Snapshot.Decisions==1&&Ids(robots)==waveIds,
                        9=>robots[1].Snapshot.Health.Current>allyBefore&&robots[1].Snapshot.Shield==20,
                        10=>enemy.Snapshot.Mode==EnemyMode.Disabled&&Vector3.Distance(origin,enemy.transform.position)<.35f,
                        11=>Report.sawHacked&&enemy.Snapshot.Mode!=EnemyMode.Hacked,
                        12=>chip.IsEnemyHolder(enemy.NetworkObjectId)&&Report.sawTrace,
                        13=>enemy.Snapshot.Health.Current<healthBefore&&chip.Snapshot.Location==ChipLocation.Dropped,
                        14=>chip.IsCarrier(defender.NetworkObjectId)&&chip.Snapshot.Trace==0,
                        15=>mission.Snapshot.Phase==MissionPhase.Failed&&mission.Snapshot.Failure==MissionFailure.ChipTraced&&robots.Length==0,
                        16=>mission.Snapshot.Phase==MissionPhase.Active&&mission.Snapshot.Attempt==2&&mission.Snapshot.CompletedStations==15
                            &&chip.Snapshot.Location==ChipLocation.Carried&&chip.Snapshot.Trace==0&&robots.Length==3&&encounter.Snapshot.Activations==1,
                        _=>false};
                    if(!ok){Fail($"AI stage {stage} failed mode:{enemy?.Snapshot.Mode} hp:{enemy?.Snapshot.Health.Current} chip:{chip.Snapshot.Location} trace:{chip.Snapshot.Trace} mission:{mission.Snapshot.Phase} modifier:{director.Snapshot.Modifier} movement:{(enemy!=null?Vector3.Distance(origin,enemy.transform.position):-1)}");return;}
                    director.TestChecks.Value|=1u<<(stage-1);director.TestStage.Value++;
                }
            }
            if(localStage!=stage){localStage=stage;localFired=false;nextSend=time+.4f;foreach(var p in ordered)if(p.IsOwner)p.GetComponent<PlayerInteraction>().TestRelease();}
            if(time<nextSend)return;nextSend=time+.2f;
            if(defender.IsOwner)
            {
                if((stage==8||stage==13)&&!localFired&&enemy!=null)
                {localFired=true;var gun=defender.GetComponent<NetworkHitscanWeapon>();gun.TestFire(gun.Eye,(enemy.transform.position+Vector3.up-gun.Eye).normalized);}
                if(stage==14)defender.GetComponent<PlayerInteraction>().TestHold(InteractionKind.RecoverChip,chip.NetworkObjectId);
            }
        }
        private void Fail(string message){Report.error=message;Debug.LogError(message);}
        private static string Ids(RobotEnemy[] robots)=>string.Join(",",robots.Select(x=>x.NetworkObjectId));
        private static void Warp(NetworkPlayerController p,Vector3 position)
        {var c=p.GetComponent<CharacterController>();c.enabled=false;p.GetComponent<NetworkTransform>().Teleport(position,Quaternion.identity,p.transform.localScale);p.GetComponent<PlayerMovement>().ResetMotion();c.enabled=true;}
    }
}
#endif
