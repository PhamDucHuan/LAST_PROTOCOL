using System;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace LastProtocol
{
    public struct RoomMember : INetworkSerializable, IEquatable<RoomMember>
    {
        public ulong ClientId;
        public int Slot;
        public FixedString32Bytes DisplayName;
        public bool Ready;
        public bool Host;
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ClientId);
            serializer.SerializeValue(ref Slot);
            serializer.SerializeValue(ref DisplayName);
            serializer.SerializeValue(ref Ready);
            serializer.SerializeValue(ref Host);
        }
        public bool Equals(RoomMember other) => ClientId == other.ClientId && Slot == other.Slot
            && DisplayName.Equals(other.DisplayName) && Ready == other.Ready && Host == other.Host;
    }

    public sealed class NetworkRoomState : NetworkBehaviour
    {
        private NetworkList<RoomMember> members;
        public NetworkList<RoomMember> Members => members;
        public readonly NetworkVariable<MatchPhase> Phase = new NetworkVariable<MatchPhase>(MatchPhase.Lobby);
        public readonly NetworkVariable<bool> TeamWiped = new NetworkVariable<bool>(false);
        public readonly NetworkVariable<uint> RoundRestarts = new NetworkVariable<uint>(0);
        public readonly NetworkVariable<int> SelectedLevel = new NetworkVariable<int>(LevelCatalog.Training);
        public event Action Changed;

        private void Awake() => members = new NetworkList<RoomMember>();

        public override void OnNetworkSpawn()
        {
            DontDestroyOnLoad(gameObject);
            members.OnListChanged += OnMembersChanged;
            Phase.OnValueChanged += OnPhaseChanged;
            TeamWiped.OnValueChanged += OnWipeChanged;
            SelectedLevel.OnValueChanged += OnLevelChanged;
            if (IsServer)
            {
                NetworkManager.OnClientConnectedCallback += AddMember;
                NetworkManager.OnClientDisconnectCallback += RemoveMember;
                foreach (ulong id in NetworkManager.ConnectedClientsIds) AddMember(id);
            }
            MultiplayerSession.Instance?.RegisterRoom(this);
        }

        public override void OnNetworkDespawn()
        {
            members.OnListChanged -= OnMembersChanged;
            Phase.OnValueChanged -= OnPhaseChanged;
            TeamWiped.OnValueChanged -= OnWipeChanged;
            SelectedLevel.OnValueChanged -= OnLevelChanged;
            if (NetworkManager != null)
            {
                NetworkManager.OnClientConnectedCallback -= AddMember;
                NetworkManager.OnClientDisconnectCallback -= RemoveMember;
            }
            MultiplayerSession.Instance?.UnregisterRoom(this);
        }

        private void Update()
        {
            if (!IsSpawned || !IsServer || Phase.Value != MatchPhase.Playing || TeamWiped.Value
                || NetworkManager.ConnectedClients.Count == 0) return;
            foreach (var client in NetworkManager.ConnectedClients.Values)
            {
                var player = client.PlayerObject;
                if (player == null || !player.IsSpawned || player.GetComponent<PlayerHealth>()?.Snapshot.IsDowned != true) return;
            }
            TeamWiped.Value = true;
            foreach (var revive in FindObjectsByType<PlayerRevive>()) revive.CancelServer(true);
        }

        // Called only by the Host's local UI; no client RPC exposes round resets.
        public bool RestartRoundServer()
        {
            if (!IsSpawned || !IsServer || !TeamWiped.Value || Phase.Value != MatchPhase.Playing) return false;
            var points = FindAnyObjectByType<PlayerSpawnPoints>();
            if (points == null) return false;
            foreach (var client in NetworkManager.ConnectedClients.Values)
                if (client.PlayerObject == null || !client.PlayerObject.IsSpawned || GetSlot(client.ClientId) < 0) return false;
            foreach (var client in NetworkManager.ConnectedClients.Values)
            {
                var player = client.PlayerObject;
                player.GetComponent<PlayerRevive>()?.ResetRoundServer();
                var point = points.GetPoint(GetSlot(client.ClientId));
                var controller = player.GetComponent<CharacterController>();
                controller.enabled = false;
                player.GetComponent<NetworkTransform>().Teleport(point.position, point.rotation, player.transform.localScale);
                controller.enabled = true;
                player.GetComponent<PlayerMovement>().ResetMotion();
                player.GetComponent<PlayerHealth>().ResetRoundServer();
            }
            foreach (var enemy in FindObjectsByType<RobotEnemy>()) enemy.ResetRoundServer();
            RoundRestarts.Value++;
            TeamWiped.Value = false;
            return true;
        }
        private void OnWipeChanged(bool previous, bool current) => Notify();

        private void AddMember(ulong id)
        {
            if (!IsServer || GetSlot(id) >= 0) return;
            int slot = 0;
            for (; slot < MultiplayerConfig.MaxPlayers; slot++)
            {
                bool used = false;
                foreach (RoomMember member in members) if (member.Slot == slot) { used = true; break; }
                if (!used) break;
            }
            if (slot == MultiplayerConfig.MaxPlayers) return;
            members.Add(new RoomMember { ClientId = id, Slot = slot, DisplayName = new FixedString32Bytes("Player " + (slot + 1)),
                Host = id == NetworkManager.ServerClientId, Ready = false });
        }

        private void RemoveMember(ulong id)
        {
            if (!IsServer) return;
            for (int i = members.Count - 1; i >= 0; i--) if (members[i].ClientId == id) members.RemoveAt(i);
        }

        public int GetSlot(ulong id)
        { foreach (RoomMember member in members) if (member.ClientId == id) return member.Slot; return -1; }
        public bool IsReady(ulong id)
        { foreach (RoomMember member in members) if (member.ClientId == id) return member.Ready; return false; }

        public bool CanStart(int minimum)
        {
            if (Phase.Value != MatchPhase.Lobby || !LevelCatalog.IsValid(SelectedLevel.Value) || members.Count < minimum) return false;
            foreach (RoomMember member in members) if (!member.Ready) return false;
            return true;
        }

        public bool SelectLevelServer(int id)
        {
            if (!IsSpawned || !IsServer || Phase.Value != MatchPhase.Lobby || !LevelCatalog.IsValid(id)
                || MultiplayerSession.Instance?.IsBusy == true) return false;
            if (SelectedLevel.Value == id) return true;
            SelectedLevel.Value = id;
            for (int i = 0; i < members.Count; i++)
            { var member = members[i]; if (member.Ready) { member.Ready = false; members[i] = member; } }
            return true;
        }
        private void OnLevelChanged(int before, int after) => Notify();

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void SetReadyRpc(bool ready, RpcParams rpcParams = default)
        {
            if (Phase.Value != MatchPhase.Lobby) return;
            ulong sender = rpcParams.Receive.SenderClientId;
            for (int i = 0; i < members.Count; i++)
            {
                RoomMember member = members[i];
                if (member.ClientId != sender) continue;
                if (member.Ready != ready) { member.Ready = ready; members[i] = member; }
                return;
            }
        }

        private void OnMembersChanged(NetworkListEvent<RoomMember> change) => Notify();
        private void OnPhaseChanged(MatchPhase previous, MatchPhase current) => Notify();
        private void Notify() { Changed?.Invoke(); MultiplayerSession.Instance?.NotifyRoomChanged(); }
    }
}
