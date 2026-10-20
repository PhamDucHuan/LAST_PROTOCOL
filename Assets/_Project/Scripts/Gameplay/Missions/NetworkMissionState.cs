using System;
using Unity.Netcode;
using UnityEngine;

namespace LastProtocol
{
    [DisallowMultipleComponent, RequireComponent(typeof(NetworkRoomState))]
    public sealed class NetworkMissionState : NetworkBehaviour
    {
        public MissionDefinition campusMission;
        public Material markerMaterial;
        private readonly NetworkVariable<MissionSnapshot> state = new(default,
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public MissionSnapshot Snapshot => state.Value;
        public event Action<MissionSnapshot> Changed;
        private NetworkRoomState room;
        private MissionDefinition activeDefinition;
        private MissionHUD hud;
        private double nextCheck, lastCheck;
        public MissionDefinition Definition => campusMission != null && Snapshot.MissionId == campusMission.missionId ? campusMission : null;

        public override void OnNetworkSpawn()
        {
            room = GetComponent<NetworkRoomState>();
            state.OnValueChanged += OnChanged;
            room.RoundRestarts.OnValueChanged += OnRestart;
            if (IsClient && SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
            { hud = gameObject.AddComponent<MissionHUD>(); hud.Bind(this); }
        }
        public bool BeginServer(int levelId)
        {
            if (!IsSpawned || !IsServer || Snapshot.Phase != MissionPhase.None) return false;
            if (campusMission == null || campusMission.levelId != levelId) return false;
            if (!campusMission.Validate(out string error)) { Debug.LogError(error); return false; }
            activeDefinition = campusMission;
            state.Value = MissionSnapshot.Begin(activeDefinition, 1);
            lastCheck = NetworkManager.ServerTime.Time; nextCheck = lastCheck;
            return true;
        }
        private void OnRestart(uint oldValue, uint newValue)
        {
            if (!IsServer || activeDefinition == null || Snapshot.Phase == MissionPhase.None) return;
            state.Value = MissionSnapshot.Begin(activeDefinition, Snapshot.Attempt + 1);
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
            int inside = 0;
            foreach (var client in NetworkManager.ConnectedClients.Values)
            {
                var player = client.PlayerObject;
                if (player != null && player.IsSpawned && player.GetComponent<PlayerHealth>()?.CanAct == true
                    && objective.Contains(player.transform.position)) inside++;
            }
            state.Value = MissionRules.Observe(activeDefinition, Snapshot, inside, required, elapsed);
        }
        private void OnChanged(MissionSnapshot before, MissionSnapshot after) => Changed?.Invoke(after);
        public override void OnNetworkDespawn()
        {
            state.OnValueChanged -= OnChanged;
            if (room != null) room.RoundRestarts.OnValueChanged -= OnRestart;
            if (hud != null) Destroy(hud);
            hud = null;
        }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public bool TestTryClientWrite()
        {
            if (IsServer || !IsSpawned) return false;
            // Permission is checked without emitting expected NGO error logs into a normal run.
            return state.CanClientWrite(NetworkManager.LocalClientId);
        }
#endif
    }
}
