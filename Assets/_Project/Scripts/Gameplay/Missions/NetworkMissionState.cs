using System;
using Unity.Netcode;
using UnityEngine;

namespace LastProtocol
{
    [DisallowMultipleComponent, RequireComponent(typeof(NetworkRoomState))]
    public sealed class NetworkMissionState : NetworkBehaviour
    {
        public MissionDefinition campusMission;
        public MissionDefinition uplinkMission;
        public MissionDefinition chapterMission;
        public Material markerMaterial;
        private readonly NetworkVariable<MissionSnapshot> state = new(default,
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<MissionSnapshot> checkpoint = new(default,
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> validationStage = new(0);
        private readonly NetworkVariable<uint> validationChecks = new(0);
        public MissionSnapshot Snapshot => state.Value;
        public MissionSnapshot Checkpoint => checkpoint.Value;
        public bool UsesCheckpoints => !IsLegacyRecon && Definition != null && ChapterPlan.Campus(Definition.missionId) && !RegressionRoundReset;
        public bool IsLongChapter => Snapshot.MissionId == ChapterPlan.MissionId;
        internal bool RegressionRoundReset { get; private set; }
        public event Action<MissionSnapshot> Changed;
        private NetworkRoomState room;
        private MissionDefinition activeDefinition;
        private MissionHUD hud;
        private double nextCheck, lastCheck;
        public MissionDefinition Definition => chapterMission != null && Snapshot.MissionId == chapterMission.missionId ? chapterMission
            : uplinkMission != null && Snapshot.MissionId == uplinkMission.missionId ? uplinkMission
            : campusMission != null && Snapshot.MissionId == campusMission.missionId ? campusMission : null;
        public bool IsLegacyRecon => Definition != null && Definition.schemaVersion == 0;
        public bool CanUseStation(string id) => IsLegacyRecon ? id == DataStation.AdministrationId
            : MissionRules.CanUseStation(Definition, Snapshot, id)
                && (id != DataStation.GeneratorId || GetComponent<NetworkChip>()?.BlocksGenerator != true);
        internal bool CompleteStationServer(string id)
        {
            if (!IsSpawned || !IsServer || !CanUseStation(id)) return false;
            if (!IsLegacyRecon) Commit(MissionRules.CompleteStation(activeDefinition, Snapshot, id));
            return true;
        }
        private void Commit(MissionSnapshot next)
        {
            if (UsesCheckpoints && (next.ObjectiveIndex != Snapshot.ObjectiveIndex || next.CompletedStations != Snapshot.CompletedStations))
                checkpoint.Value = CheckpointRules.Capture(next);
            state.Value = next;
        }
        internal void FailEncounterServer()
        { if (IsServer && IsSpawned) state.Value = MissionRules.Fail(Snapshot, MissionFailure.EncounterUnavailable); }
        internal void FailChipTraceServer()
        { if (IsServer && IsSpawned && UsesCheckpoints) state.Value = MissionRules.Fail(Snapshot, MissionFailure.ChipTraced); }

        public override void OnNetworkSpawn()
        {
            room = GetComponent<NetworkRoomState>();
            state.OnValueChanged += OnChanged;
            checkpoint.OnValueChanged += OnCheckpointChanged;
            room.RoundRestarts.OnValueChanged += OnRestart;
            if (IsClient && SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
            { hud = gameObject.AddComponent<MissionHUD>(); hud.Bind(this); }
        }
        public bool BeginServer(int levelId)
        {
            if (!IsSpawned || !IsServer || Snapshot.Phase != MissionPhase.None) return false;
            activeDefinition = chapterMission != null ? chapterMission : uplinkMission != null ? uplinkMission : campusMission;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Original content remains available only through explicit regression/preview flags.
            var args = Environment.GetCommandLineArgs();
            foreach(string flag in new[]{"--lp-ai-test","--lp-checkpoint-chip-test","--lp-uplink-test","--lp-uplink-defense-test","--lp-uplink-preview","--lp-checkpoint-chip-preview","--lp-chip-preview-client"})
                if(Array.IndexOf(args,flag)>=0)activeDefinition=uplinkMission;
            // Keep historical Protocol24/25 regression fixtures reproducible. New gameplay and
            // the checkpoint/chip fixture always use the production policy.
            RegressionRoundReset = Array.IndexOf(args, "--lp-uplink-test") >= 0
                || Array.IndexOf(args, "--lp-uplink-defense-test") >= 0 || Array.IndexOf(args, "--lp-uplink-preview") >= 0;
            foreach (string flag in new[] { "--lp-mission-test", "--lp-mission-preview", "--lp-interaction-test", "--lp-interaction-preview" })
                if (Array.IndexOf(args, flag) >= 0) activeDefinition = campusMission;
#endif
            if (activeDefinition == null || activeDefinition.levelId != levelId) return false;
            if (!activeDefinition.Validate(out string error)) { Debug.LogError(error); return false; }
            checkpoint.Value = MissionSnapshot.Begin(activeDefinition, 1);
            state.Value = checkpoint.Value;
            lastCheck = NetworkManager.ServerTime.Time; nextCheck = lastCheck;
            return true;
        }
        private void OnRestart(uint oldValue, uint newValue)
        {
            if (!IsServer || activeDefinition == null || Snapshot.Phase == MissionPhase.None) return;
            state.Value = UsesCheckpoints ? CheckpointRules.Restore(activeDefinition, Checkpoint, Snapshot.Attempt + 1)
                : MissionSnapshot.Begin(activeDefinition, Snapshot.Attempt + 1);
            lastCheck = NetworkManager.ServerTime.Time; nextCheck = lastCheck;
        }
        private void Update()
        {
            if (!IsSpawned || !IsServer || activeDefinition == null || Snapshot.Phase != MissionPhase.Active) return;
            if (room.TeamWiped.Value) { state.Value = MissionRules.Fail(Snapshot, MissionFailure.TeamWiped); return; }
            var session = MultiplayerSession.Instance;
            if (session == null || !session.CanMove) return;
            double now = NetworkManager.ServerTime.Time;
            if (now < nextCheck) return;
            float elapsed = (float)(now - lastCheck); lastCheck = now; nextCheck = now + 0.1;
            int required = NetworkManager.ConnectedClients.Count;
            if (required < session.config.minimumPlayersToStart)
            { state.Value = MissionRules.Fail(Snapshot, MissionFailure.NotEnoughPlayers); return; }
            var objective = activeDefinition.objectives[Snapshot.ObjectiveIndex];
            if (ChapterPlan.Timed(objective.kind))
            {
                int a=0,b=0;
                foreach(var client in NetworkManager.ConnectedClients.Values)
                {
                    var p=client.PlayerObject;if(p==null||!p.IsSpawned||p.GetComponent<PlayerHealth>()?.CanAct!=true)continue;
                    if(objective.Contains(p.transform.position))a++;
                    else if(objective.kind==ObjectiveKind.SplitScan&&Mathf.Abs(p.transform.position.y-objective.secondaryCenter.y)<=objective.heightTolerance
                        &&EnemyRules.PlanarDistance(p.transform.position,objective.secondaryCenter)<=objective.radius)b++;
                }
                bool safe=GetComponent<NetworkChip>()?.Secure==true;
                int steps=1;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                steps=TestClockSteps;
#endif
                var next=Snapshot;for(int i=0;i<steps&&next.ObjectiveIndex==Snapshot.ObjectiveIndex;i++)next=ChapterPlan.Observe(activeDefinition,next,a,b,safe,elapsed);
                Commit(next);return;
            }
            if (objective.kind != ObjectiveKind.Rendezvous) return;
            int inside = 0;
            foreach (var client in NetworkManager.ConnectedClients.Values)
            {
                var player = client.PlayerObject;
                if (player != null && player.IsSpawned && player.GetComponent<PlayerHealth>()?.CanAct == true
                    && objective.Contains(player.transform.position)) inside++;
            }
            if(IsLongChapter&&Snapshot.ObjectiveIndex==11&&GetComponent<NetworkChip>()?.Secure!=true)inside=0;
            Commit(MissionRules.Observe(activeDefinition, Snapshot, inside, required, elapsed));
        }
        private void OnChanged(MissionSnapshot before, MissionSnapshot after) => Changed?.Invoke(after);
        private void OnCheckpointChanged(MissionSnapshot before, MissionSnapshot after)
        {
            // NGO applies variable deltas in field order. A Client may receive mission
            // state before its checkpoint; refresh presentation again once both arrived.
            if (IsServer) return;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            TestCheckpointNotifications++;
#endif
            Changed?.Invoke(Snapshot);
        }
        public override void OnNetworkDespawn()
        {
            state.OnValueChanged -= OnChanged;
            checkpoint.OnValueChanged -= OnCheckpointChanged;
            if (room != null) room.RoundRestarts.OnValueChanged -= OnRestart;
            if (hud != null) Destroy(hud);
            hud = null;
        }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public NetworkVariable<int> TestStage => validationStage;
        internal int TestClockSteps { get; set; } = 1;
        internal void TestPrepareAIEncounter()
        {
            if (!IsServer || !UsesCheckpoints || Snapshot.ObjectiveIndex != 0) return;
            var next = Snapshot; next.ObjectiveIndex = 3; next.CompletedStations = 15; next.HeldSeconds = 0;
            GetComponent<NetworkChip>().TestMarkRelayLearned(); Commit(next);
        }
        public int TestCheckpointNotifications { get; private set; }
        public NetworkVariable<uint> TestChecks => validationChecks;
        public bool TestTryClientWrite()
        {
            if (IsServer || !IsSpawned) return false;
            // Permission is checked without emitting expected NGO error logs into a normal run.
            return state.CanClientWrite(NetworkManager.LocalClientId) || checkpoint.CanClientWrite(NetworkManager.LocalClientId);
        }
#endif
    }
}
