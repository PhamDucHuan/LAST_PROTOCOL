using System;
using Unity.Netcode;
using Unity.Collections;
using UnityEngine;

namespace LastProtocol
{
    public struct StationIdentity : INetworkSerializable, IEquatable<StationIdentity>
    {
        public FixedString64Bytes Id,Label;
        public void NetworkSerialize<T>(BufferSerializer<T> s) where T:IReaderWriter {s.SerializeValue(ref Id);s.SerializeValue(ref Label);}
        public bool Equals(StationIdentity other)=>Id.Equals(other.Id)&&Label.Equals(other.Label);
    }
    public struct DataStationState : INetworkSerializable, IEquatable<DataStationState>
    {
        public bool Active, Completed;
        public ulong Actor;
        public double StartedAt;
        public float Duration;
        public uint Completions;
        public void NetworkSerialize<T>(BufferSerializer<T> s) where T : IReaderWriter
        {
            s.SerializeValue(ref Active); s.SerializeValue(ref Completed); s.SerializeValue(ref Actor);
            s.SerializeValue(ref StartedAt); s.SerializeValue(ref Duration); s.SerializeValue(ref Completions);
        }
        public bool Equals(DataStationState other) => Active == other.Active && Completed == other.Completed && Actor == other.Actor
            && StartedAt.Equals(other.StartedAt) && Duration.Equals(other.Duration) && Completions == other.Completions;
    }
    [DisallowMultipleComponent, RequireComponent(typeof(NetworkObject))]
    public sealed class DataStation : NetworkBehaviour
    {
        public const string AdministrationId = "CTU_ADMIN_DATA_01";
        public const string GeneratorId = "CTU_AUX_GENERATOR";
        public const string UplinkId = "CTU_LIBRARY_UPLINK";
        public string stationId = AdministrationId;
        public string displayName = "ADMIN 01";
        public string completionMessage = "Rescue channel offline. Auxiliary power required.";
        public float range = 2.5f, holdSeconds = 3;
        public Vector3 interactionOffset = new(0, 1.25f, 0);
        private readonly NetworkVariable<DataStationState> state = new(default,
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        // Stable wire layout across Development and release players.
        private readonly NetworkVariable<int> validationStage = new(0);
        private readonly NetworkVariable<uint> validationChecks = new(0);
        private readonly NetworkVariable<StationIdentity> identity = new(default);
        public DataStationState Snapshot => state.Value;
        public NetworkMissionState Mission => MultiplayerSession.Instance?.Room?.GetComponent<NetworkMissionState>();
        public bool Available => Mission != null && Mission.CanUseStation(stationId);
        public bool RequiresCarrier => stationId == UplinkId && Mission?.GetComponent<NetworkChip>()?.EnabledForMission == true;
        public string ActionText => stationId == GeneratorId ? "START GENERATOR" : stationId == UplinkId ? "RESTORE UPLINK" : "ACCESS DATA";
        public string SuccessText => stationId == GeneratorId ? "GENERATOR ONLINE" : stationId == UplinkId ? Mission?.IsLongChapter==true?"LOCAL LINK ONLINE":"COMMS RESTORED" : "DATA SECURED";
        private Renderer[] displays;
        private TMPro.TMP_Text label;
        private MaterialPropertyBlock tint;
        private float nextVisual;
        public override void OnNetworkSpawn()
        {
            identity.OnValueChanged+=IdentityChanged;IdentityChanged(default,identity.Value);
            displays = System.Array.FindAll(GetComponentsInChildren<Renderer>(), x => x.name == "Cyan display" || x.name == "Status strip");
            label = GetComponentInChildren<TMPro.TMP_Text>(); tint = new MaterialPropertyBlock();
        }
        private void IdentityChanged(StationIdentity before,StationIdentity after)
        {if(after.Id.Length>0){stationId=after.Id.ToString();displayName=after.Label.ToString();}}
        internal void ConfigureServer(string id,string label)
        {if(IsServer&&IsSpawned&&MissionDefinition.ValidId(id))identity.Value=new StationIdentity{Id=new FixedString64Bytes(id),Label=new FixedString64Bytes(label)};}
        public override void OnNetworkDespawn(){identity.OnValueChanged-=IdentityChanged;}
        private void Update()
        {
            if (!IsSpawned || Time.unscaledTime < nextVisual) return;
            nextVisual = Time.unscaledTime + .2f;
            Color color = Snapshot.Completed ? new Color(.15f, 1, .45f) : Snapshot.Active ? new Color(1, .7f, .15f)
                : Available ? new Color(.05f, .8f, .95f) : new Color(.3f, .35f, .4f);
            tint.SetColor("_BaseColor", color);
            foreach (var display in displays) display.SetPropertyBlock(tint);
            if (label != null) label.text = displayName + "\n" + (Snapshot.Completed ? "ONLINE" : Available ? "[ E ]" : "LOCKED");
        }
        public bool InRange(Vector3 position) => MissionRules.Finite(position) && Vector3.Distance(position, transform.position + Vector3.up) <= range;
        public bool CanUse(PlayerInteraction actor)
        {
            var session = MultiplayerSession.Instance;
            if (!IsSpawned || actor == null || !actor.IsSpawned || session == null || !session.CanMove
                || session.SelectedLevelId != LevelCatalog.CanTho || !Available
                || !MissionRules.Positive(holdSeconds) || holdSeconds > 30 || !MissionRules.Positive(range) || range > 3
                || actor.GetComponent<PlayerHealth>()?.CanAct != true || !InRange(actor.transform.position)
                || (RequiresCarrier && !Mission.GetComponent<NetworkChip>().IsCarrier(actor.NetworkObjectId))
                || Snapshot.Completed || (Snapshot.Active && Snapshot.Actor != actor.NetworkObjectId)) return false;
            Vector3 eye = actor.transform.position + Vector3.up * .4f;
            Vector3 delta = transform.TransformPoint(interactionOffset) - eye;
            return CombatRules.Raycast(eye, delta.normalized, delta.magnitude + .1f, actor.transform, out var hit)
                && hit.collider.GetComponentInParent<DataStation>() == this;
        }
        internal bool TryBeginServer(PlayerInteraction actor)
        {
            if (!IsServer || !CanUse(actor)) return false;
            if (!Snapshot.Active)
                state.Value = new DataStationState { Active = true, Actor = actor.NetworkObjectId,
                    StartedAt = NetworkManager.ServerTime.Time, Duration = holdSeconds, Completions = Snapshot.Completions };
            return true;
        }
        internal bool CompleteServer(PlayerInteraction actor)
        {
            if (!IsServer || !CanUse(actor) || !Snapshot.Active || Snapshot.Actor != actor.NetworkObjectId
                || NetworkManager.ServerTime.Time - Snapshot.StartedAt < Snapshot.Duration || !Mission.CompleteStationServer(stationId)) return false;
            state.Value = new DataStationState { Completed = true, Completions = Snapshot.Completions + 1 };
            return true;
        }
        internal void CancelServer(PlayerInteraction actor)
        {
            if (IsSpawned && IsServer && Snapshot.Active && actor != null && Snapshot.Actor == actor.NetworkObjectId)
                state.Value = new DataStationState { Completions = Snapshot.Completions };
        }
        internal void ResetRoundServer() { if (IsSpawned && IsServer) state.Value = default; }
        internal void RestoreCheckpointServer(MissionDefinition definition, MissionSnapshot saved)
        {
            if (!IsSpawned || !IsServer) return;
            bool done = CheckpointRules.StationCompleted(definition, saved, stationId);
            state.Value = new DataStationState { Completed = done, Completions = done ? 1u : 0u };
        }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public bool TestTryClientWrite() => !IsServer && IsSpawned && state.CanClientWrite(NetworkManager.LocalClientId);
        public NetworkVariable<int> TestStage => validationStage;
        public NetworkVariable<uint> TestChecks => validationChecks;
#endif
    }
}
