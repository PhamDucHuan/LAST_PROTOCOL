using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
namespace LastProtocol
{
    public struct ChapterOperationState : INetworkSerializable,IEquatable<ChapterOperationState>
    {
        public uint Attempt;
        public int Objective,Waves,Alive,Spawned,Defeated;
        public float ElapsedSeconds;
        public double NextWaveAt;
        public void NetworkSerialize<T>(BufferSerializer<T> s)where T:IReaderWriter
        {s.SerializeValue(ref Attempt);s.SerializeValue(ref Objective);s.SerializeValue(ref Waves);s.SerializeValue(ref Alive);s.SerializeValue(ref Spawned);s.SerializeValue(ref Defeated);s.SerializeValue(ref ElapsedSeconds);s.SerializeValue(ref NextWaveAt);}
        public bool Equals(ChapterOperationState x)=>Attempt==x.Attempt&&Objective==x.Objective&&Waves==x.Waves&&Alive==x.Alive&&Spawned==x.Spawned&&Defeated==x.Defeated&&ElapsedSeconds.Equals(x.ElapsedSeconds)&&NextWaveAt.Equals(x.NextWaveAt);
    }
    [DisallowMultipleComponent,RequireComponent(typeof(NetworkMissionState))]
    public sealed class ChapterOperation : NetworkBehaviour
    {
        public NavMeshData navigation;
        public DataStation terminalPrefab;
        public float reinforcementInterval=18;
        private readonly NetworkVariable<ChapterOperationState> state=new(default);
        private readonly NetworkVariable<int> validationStage=new(0);
        private readonly NetworkVariable<uint> validationChecks=new(0);
        public ChapterOperationState Snapshot=>state.Value;
        public event Action<ChapterOperationState> Changed;
        private NetworkMissionState mission;
        private EnemyDirector director;
        private readonly List<RobotEnemy> robots=new();
        private readonly List<DataStation> devices=new();
        private NavMeshDataInstance mesh;
        private double nextAudit,lastElapsed;
        private bool prepared,transitioning;
        public bool Enabled=>mission?.IsLongChapter==true;
        public bool HandlesNavigation=>IsServer&&mesh.valid;
        public bool CombatActive=>Enabled&&mission.Snapshot.Phase==MissionPhase.Active&&mission.Snapshot.ObjectiveIndex>=4&&mission.Snapshot.ObjectiveIndex<=10
            &&(ChapterPlan.Timed(Current.kind)||Snapshot.Alive>0);
        public RendezvousObjective Current=>mission.Definition.objectives[Mathf.Clamp(mission.Snapshot.ObjectiveIndex,0,mission.Definition.objectives.Length-1)];
        public Vector3 Node=>Enabled?Current.center:transform.position;
        public override void OnNetworkSpawn()
        {
            mission=GetComponent<NetworkMissionState>();director=GetComponent<EnemyDirector>();
            mission.Changed+=MissionChanged;state.OnValueChanged+=StateChanged;lastElapsed=Time.realtimeSinceStartupAsDouble;
        }
        private void StateChanged(ChapterOperationState before,ChapterOperationState after)=>Changed?.Invoke(after);
        private void MissionChanged(MissionSnapshot value)
        {
            if(!IsServer||!IsSpawned||!Enabled||transitioning)return;
            transitioning=true;
            try
            {
                if(!prepared)
                {
                    if(navigation==null||terminalPrefab==null||!MissionRules.Positive(reinforcementInterval)||reinforcementInterval<12||reinforcementInterval>30)
                    {Fail("Invalid chapter operation configuration");return;}
                    mesh=NavMesh.AddNavMeshData(navigation);prepared=true;
                    for(int i=0;i<ChapterPlan.DeviceIds.Length;i++)
                    {
                        var terminal=Instantiate(terminalPrefab,ChapterPlan.DevicePositions[i],Quaternion.identity);
                        devices.Add(terminal);terminal.NetworkObject.Spawn(true);terminal.ConfigureServer(ChapterPlan.DeviceIds[i],new[]{"GRID EAST","GRID WEST","B1 ARCHIVE","RLC ARCHIVE","ROUTE WEST","RADIO RELAY"}[i]);
                    }
                }
                if(value.Phase!=MissionPhase.Active){ClearRobots();var stopped=Snapshot;stopped.Alive=0;stopped.NextWaveAt=0;state.Value=stopped;return;}
                if(Snapshot.Attempt==value.Attempt&&Snapshot.Objective==value.ObjectiveIndex)return;
                ClearRobots();state.Value=new ChapterOperationState{Attempt=value.Attempt,Objective=value.ObjectiveIndex,ElapsedSeconds=Snapshot.ElapsedSeconds,
                    NextWaveAt=value.ObjectiveIndex>=4&&value.ObjectiveIndex<=10?NetworkManager.ServerTime.Time+3:0};
                lastElapsed=Time.realtimeSinceStartupAsDouble;
                if(value.ObjectiveIndex>=4&&value.ObjectiveIndex<=10)director.BeginEncounterServer();
                Debug.Log($"CHAPTER_STAGE attempt={value.Attempt} objective={value.ObjectiveIndex} confirmed={value.CompletedStations}");
            }
            finally{transitioning=false;}
        }
        private void Update()
        {
            if(!IsSpawned||!IsServer||!Enabled||mission.Snapshot.Phase!=MissionPhase.Active)return;
            double now=Time.realtimeSinceStartupAsDouble;if(now<nextAudit)return;nextAudit=now+.25;
            var next=Snapshot;next.ElapsedSeconds+=(float)Math.Min(1,now-lastElapsed);lastElapsed=now;
            int alive=robots.Count(x=>x!=null&&x.IsSpawned&&!x.Snapshot.Health.IsDowned);next.Alive=alive;
            next.Defeated=next.Spawned-alive;state.Value=next;
            int objective=mission.Snapshot.ObjectiveIndex;
            if(objective<4||objective>10||next.NextWaveAt==0||NetworkManager.ServerTime.Time<next.NextWaveAt)return;
            int quota=ChapterPlan.Timed(Current.kind)?Mathf.CeilToInt(Current.holdSeconds/reinforcementInterval)+1:1;
            if(next.Waves>=quota){next.NextWaveAt=0;state.Value=next;return;}
            if(alive>=3){next.NextWaveAt=NetworkManager.ServerTime.Time+3;state.Value=next;return;}
            for(int i=robots.Count-1;i>=0;i--)
                if(robots[i]==null||!robots[i].IsSpawned||robots[i].Snapshot.Health.IsDowned)
                {if(robots[i]!=null&&robots[i].IsSpawned)robots[i].NetworkObject.Despawn(true);robots.RemoveAt(i);}
            int spawned=0;
            for(int slot=alive;slot<3;slot++)
            {
                Vector3 anchor=Current.kind==ObjectiveKind.SplitScan&&(slot+next.Waves)%2==1?Current.secondaryCenter:Current.center;
                if(!TrySpawnPoint(anchor,slot+next.Waves*3,out var point))continue;
                EnemyRole[][] patterns={new[]{EnemyRole.Frontliner,EnemyRole.Suppressor,EnemyRole.Hunter},new[]{EnemyRole.Flanker,EnemyRole.Support,EnemyRole.Hunter},
                    new[]{EnemyRole.Frontliner,EnemyRole.Controller,EnemyRole.Hunter},new[]{EnemyRole.Flanker,EnemyRole.Suppressor,EnemyRole.Hunter}};
                var role=patterns[next.Waves%patterns.Length][slot];
                if(role==EnemyRole.Suppressor)role=director.ResponseRole(role);
                var prefab=director.profile.roster[(int)role];var robot=Instantiate(prefab,point,Quaternion.LookRotation((anchor-point).normalized));
                robot.encounterId="CTU01_STAGE_"+objective;robots.Add(robot);robot.NetworkObject.Spawn(true);director.ConfigureReinforcement(robot.Brain);spawned++;
            }
            if(spawned==0){next.NextWaveAt=NetworkManager.ServerTime.Time+3;state.Value=next;return;}
            next.Waves++;next.Spawned+=spawned;next.Alive=alive+spawned;next.NextWaveAt=NetworkManager.ServerTime.Time+reinforcementInterval;state.Value=next;
            Debug.Log($"CHAPTER_WAVE attempt={next.Attempt} objective={objective} wave={next.Waves}/{quota} alive={next.Alive}/3");
        }
        private bool TrySpawnPoint(Vector3 anchor,int offset,out Vector3 point)
        {
            point=default;
            for(int i=0;i<48;i++)
            {
                float angle=(i+offset*7)*Mathf.PI*2/16;float radius=18+(i/16)*3;
                var wanted=anchor+new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius);
                if(!NavMesh.SamplePosition(wanted,out var hit,2,NavMesh.AllAreas))continue;
                if(NetworkManager.ConnectedClients.Values.Any(c=>c.PlayerObject!=null&&c.PlayerObject.GetComponent<PlayerHealth>()?.CanAct==true
                    &&Vector3.Distance(c.PlayerObject.transform.position,hit.position)<9))continue;
                if(robots.Any(r=>r!=null&&Vector3.Distance(r.transform.position,hit.position)<2))continue;
                var path=new NavMeshPath();if(!NavMesh.CalculatePath(hit.position,anchor,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)continue;
                point=hit.position;return true;
            }
            return false;
        }
        private void Fail(string reason){Debug.LogError(reason);mission.FailEncounterServer();}
        private void ClearRobots()
        {foreach(var robot in robots)if(robot!=null){if(IsServer&&robot.IsSpawned)robot.NetworkObject.Despawn(true);else if(!robot.IsSpawned)Destroy(robot.gameObject);}robots.Clear();}
        public override void OnNetworkDespawn()
        {
            if(mission!=null)mission.Changed-=MissionChanged;state.OnValueChanged-=StateChanged;ClearRobots();
            foreach(var d in devices)if(d!=null&&IsServer&&d.IsSpawned)d.NetworkObject.Despawn(true);devices.Clear();
            if(mesh.valid)mesh.Remove();prepared=false;
        }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public NetworkVariable<int> TestStage=>validationStage;
        public NetworkVariable<uint> TestChecks=>validationChecks;
        public bool TestTryClientWrite()=>!IsServer&&IsSpawned&&state.CanClientWrite(NetworkManager.LocalClientId);
#endif
    }
}
