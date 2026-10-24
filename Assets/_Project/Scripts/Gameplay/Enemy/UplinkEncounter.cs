using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace LastProtocol
{
    [DisallowMultipleComponent, RequireComponent(typeof(NetworkMissionState))]
    public sealed class UplinkEncounter : NetworkBehaviour
    {
        public UplinkEncounterDefinition definition;
        public EnemyEncounterProfile tacticalProfile;
        private readonly NetworkVariable<UplinkEncounterState> state = new(default,
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        // Kept in every build so Development fixtures never change the wire layout.
        private readonly NetworkVariable<int> validationStage = new(0);
        private readonly NetworkVariable<uint> validationChecks = new(0);
        public UplinkEncounterState Snapshot => state.Value;
        public event Action<UplinkEncounterState> Changed;
        private NetworkMissionState mission;
        private readonly List<RobotEnemy> robots = new();
        private NavMeshDataInstance navigation;
        private float nextAudit;
        public bool EnabledForMission => mission?.Definition != null && ChapterPlan.Campus(mission.Definition.missionId);
        public override void OnNetworkSpawn()
        {
            mission = GetComponent<NetworkMissionState>();
            state.OnValueChanged += OnChanged; mission.Changed += OnMissionChanged;
            if (IsServer) OnMissionChanged(mission.Snapshot);
        }
        private void OnChanged(UplinkEncounterState before, UplinkEncounterState after) => Changed?.Invoke(after);
        private void OnMissionChanged(MissionSnapshot snapshot)
        {
            if (!IsSpawned || !IsServer) return;
            if (!EnabledForMission) { Cleanup(); state.Value = default; return; }
            if (Snapshot.Attempt != snapshot.Attempt)
            { Cleanup(); state.Value = new UplinkEncounterState { Attempt = snapshot.Attempt }; }
            if(mission.IsLongChapter&&snapshot.ObjectiveIndex>=4)
            {var secured=Snapshot;CountDefeated(ref secured);Cleanup();secured.Phase=UplinkEncounterPhase.Secured;secured.Alive=0;state.Value=secured;return;}
            if (snapshot.Phase == MissionPhase.Failed || snapshot.Phase == MissionPhase.Completed)
            {
                var next = Snapshot;
                next.Phase = snapshot.Phase == MissionPhase.Completed ? UplinkEncounterPhase.Secured : UplinkEncounterPhase.Failed;
                CountDefeated(ref next); Cleanup(); next.Alive = 0; state.Value = next; return;
            }
            if (UplinkEncounterRules.ShouldActivate(mission.Definition, snapshot, Snapshot)) Activate();
        }
        private void Activate()
        {
            // Latch before spawning; E requests and cancellation never reset this ledger.
            var next = Snapshot; next.Activations = 1; next.Phase = UplinkEncounterPhase.Active; state.Value = next;
            if (definition == null || !definition.Validate(out _))
            { Abort("Uplink encounter configuration is invalid."); return; }
            bool tactical = mission.UsesCheckpoints && tacticalProfile != null;
            if (tactical && (!tacticalProfile.Validate(out _) || tacticalProfile.opening.Length != definition.spawnPoints.Length
                || GetComponent<EnemyDirector>()?.BeginEncounterServer() != true))
            { Abort("Tactical encounter configuration is invalid."); return; }
            if(GetComponent<ChapterOperation>()?.HandlesNavigation!=true)navigation = NavMesh.AddNavMeshData(definition.navigation);
            var positions = new Vector3[definition.spawnPoints.Length];
            for (int i = 0; i < positions.Length; i++)
            {
                if (!NavMesh.SamplePosition(definition.spawnPoints[i], out var hit, .3f, NavMesh.AllAreas))
                { Abort("Uplink encounter spawn is outside navigation."); return; }
                positions[i] = hit.position;
            }
            for (int i = 0; i < positions.Length; i++)
            {
                var prefab = tactical ? tacticalProfile.roster[(int)tacticalProfile.opening[i]] : definition.enemyPrefab;
                // Face the uplink approach. No client target information is used for spawning.
                var robot = Instantiate(prefab, positions[i], tactical ? Quaternion.Euler(0, 0, 0) : Quaternion.identity);
                robots.Add(robot); robot.NetworkObject.Spawn(true);
            }
            next = Snapshot; next.Spawned = next.Alive = robots.Count; state.Value = next;
        }
        private void Abort(string message)
        {
            Debug.LogError(message); Cleanup();
            mission.FailEncounterServer();
        }
        private void Update()
        {
            if (!IsSpawned || !IsServer || Snapshot.Phase != UplinkEncounterPhase.Active || Time.unscaledTime < nextAudit) return;
            nextAudit = Time.unscaledTime + .1f;
            var next = Snapshot; CountDefeated(ref next);
            if (next.Alive == 0) next.Phase = UplinkEncounterPhase.Cleared;
            state.Value = next;
        }
        private void CountDefeated(ref UplinkEncounterState next)
        {
            int dead = 0;
            foreach (var robot in robots) if (robot == null || !robot.IsSpawned || robot.Snapshot.Health.IsDowned) dead++;
            next.Defeated = Math.Max(next.Defeated, dead); next.Alive = Math.Max(0, next.Spawned - next.Defeated);
        }
        private void Cleanup()
        {
            foreach (var robot in robots)
            {
                if (robot == null) continue;
                if (robot.IsSpawned && IsServer) robot.NetworkObject.Despawn(true);
                else if (!robot.IsSpawned) Destroy(robot.gameObject);
            }
            robots.Clear();
            if (navigation.valid) navigation.Remove();
            navigation = default;
        }
        public override void OnNetworkDespawn()
        {
            if (mission != null) mission.Changed -= OnMissionChanged;
            state.OnValueChanged -= OnChanged;
            Cleanup();
        }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public NetworkVariable<int> TestStage => validationStage;
        public NetworkVariable<uint> TestChecks => validationChecks;
        public bool TestTryClientWrite() => !IsServer && IsSpawned && state.CanClientWrite(NetworkManager.LocalClientId);
#endif
    }
}
