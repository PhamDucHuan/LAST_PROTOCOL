using System;
using Unity.Netcode;
using UnityEngine;

namespace LastProtocol
{
    public enum ChipLocation : byte { Inactive, Carried, Dropped, Captured }
    public struct ChipState : INetworkSerializable, IEquatable<ChipState>
    {
        public ChipLocation Location;
        public ulong Carrier, PreviousCarrier, Actor;
        public int SavedCarrierSlot;
        public Vector3 DropPosition;
        public bool TutorialComplete, DrillStarted, EjectedFromDowned, Active;
        public InteractionKind Action;
        public double StartedAt;
        public float Duration;
        public uint Attempt, Transfers, Revision;
        public ulong EnemyHolder;
        public float Trace;
        public void NetworkSerialize<T>(BufferSerializer<T> s) where T : IReaderWriter
        {
            s.SerializeValue(ref Location); s.SerializeValue(ref Carrier); s.SerializeValue(ref PreviousCarrier);
            s.SerializeValue(ref Actor); s.SerializeValue(ref SavedCarrierSlot); s.SerializeValue(ref DropPosition);
            s.SerializeValue(ref TutorialComplete); s.SerializeValue(ref DrillStarted); s.SerializeValue(ref EjectedFromDowned);
            s.SerializeValue(ref Active); s.SerializeValue(ref Action); s.SerializeValue(ref StartedAt);
            s.SerializeValue(ref Duration); s.SerializeValue(ref Attempt); s.SerializeValue(ref Transfers); s.SerializeValue(ref Revision);
            s.SerializeValue(ref EnemyHolder); s.SerializeValue(ref Trace);
        }
        public bool Equals(ChipState x) => Location == x.Location && Carrier == x.Carrier && PreviousCarrier == x.PreviousCarrier
            && Actor == x.Actor && SavedCarrierSlot == x.SavedCarrierSlot && DropPosition.Equals(x.DropPosition)
            && TutorialComplete == x.TutorialComplete && DrillStarted == x.DrillStarted && EjectedFromDowned == x.EjectedFromDowned
            && Active == x.Active && Action == x.Action && StartedAt.Equals(x.StartedAt) && Duration.Equals(x.Duration)
            && Attempt == x.Attempt && Transfers == x.Transfers && Revision == x.Revision && EnemyHolder == x.EnemyHolder && Trace.Equals(x.Trace);
    }

    // A single Room-owned snapshot is the only chip. The visible capsule is presentation, not inventory.
    [DisallowMultipleComponent, RequireComponent(typeof(NetworkMissionState))]
    public sealed class NetworkChip : NetworkBehaviour
    {
        [Range(0, 3)] public int initialCarrierSlot;
        public float recoverySeconds = 2, drillSeconds = 1.5f, range = 2.5f;
        public Vector3 drillPosition = new(30, 1.1f, -26);
        private readonly NetworkVariable<ChipState> state = new(default,
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> validationStage = new(0);
        private readonly NetworkVariable<uint> validationChecks = new(0);
        public ChipState Snapshot => state.Value;
        public event Action<ChipState> Changed;
        private NetworkMissionState mission;
        private NetworkRoomState room;
        private Vector3 lastCarrierPosition;
        private GameObject capsule;
        private Material capsuleMaterial;
        private double nextTraceTick;
        public bool EnabledForMission => mission?.UsesCheckpoints == true;
        public bool BlocksGenerator => EnabledForMission && !Snapshot.TutorialComplete;
        public bool IsCarrier(ulong objectId) => EnabledForMission && Snapshot.Location == ChipLocation.Carried && Snapshot.Carrier == objectId;
        public bool Secure => EnabledForMission && Snapshot.Location == ChipLocation.Carried && IsSpawned
            && NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(Snapshot.Carrier,out var carrier)
            && carrier.IsSpawned&&carrier.GetComponent<PlayerHealth>()?.CanAct==true;
        public bool DrillPending => EnabledForMission && !Snapshot.TutorialComplete && !Snapshot.DrillStarted
            && mission.Snapshot.Phase == MissionPhase.Active && mission.Snapshot.ObjectiveIndex == 2 && Snapshot.Location == ChipLocation.Carried;

        public override void OnNetworkSpawn()
        {
            mission = GetComponent<NetworkMissionState>(); room = GetComponent<NetworkRoomState>();
            state.OnValueChanged += OnChanged; mission.Changed += OnMissionChanged;
            if (IsServer) OnMissionChanged(mission.Snapshot);
        }
        private void OnChanged(ChipState before, ChipState after) => Changed?.Invoke(after);
        private void OnMissionChanged(MissionSnapshot value)
        {
            if (!IsServer || !IsSpawned) return;
            if (!EnabledForMission) { state.Value = default; return; }
            if (Snapshot.Location == ChipLocation.Inactive || Snapshot.Attempt != value.Attempt) RestoreCarrier(value.Attempt);
            if (value.Phase != MissionPhase.Active) ClearChannel();
        }
        private void RestoreCarrier(uint attempt)
        {
            var old = Snapshot;
            int slot = old.Location == ChipLocation.Inactive ? initialCarrierSlot : old.SavedCarrierSlot;
            NetworkObject chosen = null;
            // Prefer the most recently confirmed carrier; a departed player cannot own the capsule.
            int best = int.MaxValue;
            foreach (var client in NetworkManager.ConnectedClients.Values)
            {
                var p = client.PlayerObject; int current = room.GetSlot(client.ClientId);
                if (p == null || !p.IsSpawned || current < 0) continue;
                if (current == slot) { chosen = p; break; }
                if (current < best) { best = current; chosen = p; }
            }
            if (chosen == null) return;
            lastCarrierPosition = chosen.transform.position;
            state.Value = new ChipState { Location = ChipLocation.Carried, Carrier = chosen.NetworkObjectId,
                SavedCarrierSlot = room.GetSlot(chosen.OwnerClientId), Attempt = attempt, Revision = old.Revision + 1,
                Transfers = old.Transfers, TutorialComplete = old.TutorialComplete };
        }
        private void Update()
        {
            if (!IsSpawned) return;
            if (IsServer && EnabledForMission && mission.Snapshot.Phase == MissionPhase.Active && Snapshot.Location == ChipLocation.Carried)
            {
                bool exists = NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(Snapshot.Carrier, out var p) && p.IsSpawned;
                if (exists) lastCarrierPosition = p.transform.position;
                if (!exists || p.GetComponent<PlayerHealth>()?.CanAct != true)
                    Eject(lastCarrierPosition, exists && p.GetComponent<PlayerHealth>()?.Snapshot.IsDowned == true);
            }
            UpdateVisual();
            if (IsServer && EnabledForMission && mission.Snapshot.Phase == MissionPhase.Active) TickEnemyCapture();
        }
        private void Eject(Vector3 position, bool downed)
        {
            if (Snapshot.Location != ChipLocation.Carried) return;
            var next = WithoutChannel(Snapshot); next.Location = ChipLocation.Dropped; next.PreviousCarrier = next.Carrier;
            next.Carrier = 0; next.EjectedFromDowned = downed;
            next.DropPosition = MissionRules.Finite(position) ? position + Vector3.up * .1f : drillPosition;
            next.Revision++; state.Value = next;
            GetComponent<EnemyDirector>()?.EmitNoiseServer(next.DropPosition, EnemyNoise.ChipEject, .6f);
        }
        public bool InRecoveryRange(Vector3 position) => EnabledForMission && Snapshot.Location == ChipLocation.Dropped
            && MissionRules.Finite(position) && Vector3.Distance(position, Snapshot.DropPosition) <= range;
        public bool NearDrill(Vector3 position) => DrillPending && Vector3.Distance(position, drillPosition) <= range;
        public bool HasDrillBuddy(PlayerInteraction actor)
        {
            foreach (var client in NetworkManager.ConnectedClients.Values)
            {
                var p = client.PlayerObject;
                if (p != null && p.IsSpawned && p.NetworkObjectId != actor.NetworkObjectId && p.GetComponent<PlayerHealth>()?.CanAct == true
                    && Vector3.Distance(p.transform.position, drillPosition) <= 5) return true;
            }
            return false;
        }
        public bool CanUse(PlayerInteraction actor, InteractionKind kind)
        {
            if (!EnabledForMission || !IsSpawned || actor == null || !actor.IsSpawned || mission.Snapshot.Phase != MissionPhase.Active
                || MultiplayerSession.Instance?.CanMove != true || actor.GetComponent<PlayerHealth>()?.CanAct != true
                || (Snapshot.Active && (Snapshot.Actor != actor.NetworkObjectId || Snapshot.Action != kind))) return false;
            if (!MissionRules.Positive(range) || range > 3 || !MissionRules.Positive(recoverySeconds) || recoverySeconds > 10
                || !MissionRules.Positive(drillSeconds) || drillSeconds > 10) return false;
            if (kind == InteractionKind.RelayDrill)
            {
                if (!NearDrill(actor.transform.position) || !IsCarrier(actor.NetworkObjectId) || !HasDrillBuddy(actor)) return false;
                return ClearSight(actor, drillPosition, false);
            }
            if (kind != InteractionKind.RecoverChip || !InRecoveryRange(actor.transform.position)
                || (!Snapshot.TutorialComplete && Snapshot.EjectedFromDowned && Snapshot.PreviousCarrier == actor.NetworkObjectId)) return false;
            return ClearSight(actor, Snapshot.DropPosition, true);
        }
        private bool ClearSight(PlayerInteraction actor, Vector3 point, bool ignorePreviousCarrier)
        {
            Vector3 eye = actor.transform.position + Vector3.up * .4f, delta = point + Vector3.up * .25f - eye;
            foreach (var hit in Physics.RaycastAll(eye, delta.normalized, delta.magnitude, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.transform.IsChildOf(actor.transform)) continue;
                var p = hit.collider.GetComponentInParent<NetworkObject>();
                if (ignorePreviousCarrier && p != null && p.NetworkObjectId == Snapshot.PreviousCarrier) continue;
                return false;
            }
            return true;
        }
        internal bool TryBeginServer(PlayerInteraction actor, InteractionKind kind)
        {
            if (!IsServer || !CanUse(actor, kind)) return false;
            if (!Snapshot.Active)
            {
                var next = Snapshot; next.Active = true; next.Actor = actor.NetworkObjectId; next.Action = kind;
                next.StartedAt = NetworkManager.ServerTime.Time; next.Duration = kind == InteractionKind.RecoverChip ? recoverySeconds : drillSeconds;
                state.Value = next;
            }
            return true;
        }
        internal bool CompleteServer(PlayerInteraction actor, InteractionKind kind)
        {
            if (!IsServer || !Snapshot.Active || Snapshot.Actor != actor.NetworkObjectId || Snapshot.Action != kind
                || NetworkManager.ServerTime.Time - Snapshot.StartedAt < Snapshot.Duration || !CanUse(actor, kind)) return false;
            var next = WithoutChannel(Snapshot);
            if (kind == InteractionKind.RelayDrill)
            {
                next.DrillStarted = true; state.Value = next;
                actor.GetComponent<PlayerHealth>().TryApplyDamageServer(int.MaxValue);
                Eject(actor.transform.position, true);
            }
            else
            {
                next.Location = ChipLocation.Carried; next.Carrier = actor.NetworkObjectId;
                next.SavedCarrierSlot = room.GetSlot(actor.OwnerClientId); next.Transfers++; next.Revision++;
                if (next.EjectedFromDowned && next.PreviousCarrier != next.Carrier) next.TutorialComplete = true;
                next.DrillStarted = false; next.EjectedFromDowned = false;
                next.Trace = 0; next.EnemyHolder = 0; state.Value = next;
                lastCarrierPosition = actor.transform.position;
            }
            return true;
        }
        internal void CancelServer(PlayerInteraction actor)
        { if (IsServer && IsSpawned && Snapshot.Active && actor != null && Snapshot.Actor == actor.NetworkObjectId) ClearChannel(); }
        private void ClearChannel() { if (Snapshot.Active) state.Value = WithoutChannel(Snapshot); }
        private static ChipState WithoutChannel(ChipState next)
        { next.Active = false; next.Actor = 0; next.Action = InteractionKind.None; next.StartedAt = 0; next.Duration = 0; return next; }
        public bool IsEnemyHolder(ulong objectId) => Snapshot.Location == ChipLocation.Captured && Snapshot.EnemyHolder == objectId;
        private RobotEnemy EnemyById(ulong id) => NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(id, out var obj) ? obj.GetComponent<RobotEnemy>() : null;
        internal bool EnemyEligible(RobotEnemy enemy) => IsServer && EnabledForMission && mission.Snapshot.Phase == MissionPhase.Active
            && GetComponent<EnemyDirector>()?.EncounterActive == true && Snapshot.TutorialComplete
            && Snapshot.Location == ChipLocation.Dropped && enemy != null && enemy.IsSpawned && enemy.IsServer && !enemy.Snapshot.Health.IsDowned
            && enemy.HasTacticalAI && enemy.Brain.CurrentProfile.role == EnemyRole.Hunter
            && (!Snapshot.Active || Snapshot.Actor == enemy.NetworkObjectId && Snapshot.Action == InteractionKind.EnemyCapture);
        private bool EnemyClose(RobotEnemy enemy)
        {
            if (Vector3.Distance(enemy.transform.position, Snapshot.DropPosition) > 2.5f) return false;
            return EnemyCanSeeDrop(enemy);
        }
        internal bool EnemyCanSeeDrop(RobotEnemy enemy)
        {
            Vector3 delta = Snapshot.DropPosition + Vector3.up * .25f - enemy.Eye;
            foreach (var hit in Physics.RaycastAll(enemy.Eye, delta.normalized, delta.magnitude, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform.IsChildOf(enemy.transform)) continue;
                var p = hit.collider.GetComponentInParent<NetworkObject>();
                if (p != null && p.NetworkObjectId == Snapshot.PreviousCarrier) continue;
                return false;
            }
            return true;
        }
        internal bool TryCaptureEnemyServer(RobotEnemy enemy, float seconds)
        {
            if (!EnemyEligible(enemy) || !EnemyClose(enemy) || !MissionRules.Positive(seconds) || seconds < 2 || seconds > 6) return false;
            if (!Snapshot.Active)
            {
                var next = Snapshot; next.Active = true; next.Actor = enemy.NetworkObjectId; next.Action = InteractionKind.EnemyCapture;
                next.StartedAt = NetworkManager.ServerTime.Time; next.Duration = seconds; state.Value = next;
            }
            return true;
        }
        internal void ReleaseEnemyServer(RobotEnemy enemy)
        {
            if (!IsServer || !IsSpawned || enemy == null) return;
            if (Snapshot.Active && Snapshot.Actor == enemy.NetworkObjectId && Snapshot.Action == InteractionKind.EnemyCapture) ClearChannel();
            if (!IsEnemyHolder(enemy.NetworkObjectId)) return;
            var next = WithoutChannel(Snapshot); next.Location = ChipLocation.Dropped; next.EnemyHolder = 0;
            // Keep the released capsule outside the holder's collider so E recovery has a clear approach.
            Vector3 release = enemy.transform.position + Vector3.right * .85f;
            if (UnityEngine.AI.NavMesh.SamplePosition(release, out var ground, 1, UnityEngine.AI.NavMesh.AllAreas)) release = ground.position;
            next.DropPosition = release + Vector3.up * .1f; next.Revision++; state.Value = next;
        }
        private void TickEnemyCapture()
        {
            if (Snapshot.Action == InteractionKind.EnemyCapture && Snapshot.Active)
            {
                var enemy = EnemyById(Snapshot.Actor);
                if (!EnemyEligible(enemy) || !EnemyClose(enemy)) ClearChannel();
                else if (NetworkManager.ServerTime.Time - Snapshot.StartedAt >= Snapshot.Duration)
                {
                    var next = WithoutChannel(Snapshot); next.Location = ChipLocation.Captured; next.EnemyHolder = enemy.NetworkObjectId;
                    next.DropPosition = enemy.transform.position; next.Revision++; state.Value = next;
                }
            }
            if (Time.realtimeSinceStartupAsDouble < nextTraceTick) return; nextTraceTick = Time.realtimeSinceStartupAsDouble + .5;
            if (Snapshot.Location == ChipLocation.Captured)
            {
                var holder = EnemyById(Snapshot.EnemyHolder);
                if (holder == null || !holder.IsSpawned || holder.Snapshot.Health.IsDowned)
                { var next = WithoutChannel(Snapshot); next.Location = ChipLocation.Dropped; next.EnemyHolder = 0; next.Revision++; state.Value = next; }
                else { var next = Snapshot; next.DropPosition = holder.transform.position + Vector3.up * 1.4f; state.Value = next; }
            }
            bool exposed = Snapshot.Location == ChipLocation.Dropped || Snapshot.Location == ChipLocation.Captured;
            if (!exposed || GetComponent<EnemyDirector>()?.EncounterActive != true) return;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Existing opt-in checkpoint fixtures explicitly pause combat, including its chip timer.
            bool allPaused = true; foreach (var enemy in FindObjectsByType<RobotEnemy>()) if (!enemy.DebugPaused) allPaused = false;
            if (allPaused && Array.IndexOf(Environment.GetCommandLineArgs(), "--lp-checkpoint-chip-test") >= 0) return;
#endif
            var traced = Snapshot; traced.Trace = ChipAIRules.AdvanceTrace(traced.Trace, traced.Location, .5f); state.Value = traced;
            if (traced.Trace >= 100) mission.FailChipTraceServer();
        }
        private void UpdateVisual()
        {
            bool show = EnabledForMission && (Snapshot.Location == ChipLocation.Dropped || Snapshot.Location == ChipLocation.Captured) && mission.Snapshot.Phase == MissionPhase.Active;
            if (show && capsule == null && SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule); capsule.name = "CONTROL CHIP - local presentation";
                capsule.transform.SetParent(transform, false); Destroy(capsule.GetComponent<Collider>());
                capsule.transform.localScale = new Vector3(.24f, .26f, .24f);
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader != null)
                { capsuleMaterial = new Material(shader); capsuleMaterial.SetColor("_BaseColor", new Color(1, .65f, .1f)); capsule.GetComponent<Renderer>().sharedMaterial = capsuleMaterial; }
            }
            if (capsule != null)
            { capsule.SetActive(show); capsule.transform.position = Snapshot.DropPosition; capsule.transform.rotation = Quaternion.Euler(0, Time.unscaledTime * 65, 35); }
        }
        public override void OnNetworkDespawn()
        {
            state.OnValueChanged -= OnChanged; if (mission != null) mission.Changed -= OnMissionChanged;
            if (capsule != null) Destroy(capsule); if (capsuleMaterial != null) Destroy(capsuleMaterial);
        }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public NetworkVariable<int> TestStage => validationStage;
        internal void TestMarkRelayLearned() { if (IsServer) { var s = Snapshot; s.TutorialComplete = true; state.Value = s; } }
        public NetworkVariable<uint> TestChecks => validationChecks;
        public bool TestTryClientWrite() => !IsServer && IsSpawned && state.CanClientWrite(NetworkManager.LocalClientId);
#endif
    }
}
