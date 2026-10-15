using System;
using Unity.Collections;
using Unity.Netcode;
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
        public event Action Changed;

        private void Awake() => members = new NetworkList<RoomMember>();

        public override void OnNetworkSpawn()
        {
            DontDestroyOnLoad(gameObject);
            members.OnListChanged += OnMembersChanged;
            Phase.OnValueChanged += OnPhaseChanged;
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
            if (NetworkManager != null)
            {
                NetworkManager.OnClientConnectedCallback -= AddMember;
                NetworkManager.OnClientDisconnectCallback -= RemoveMember;
            }
            MultiplayerSession.Instance?.UnregisterRoom(this);
        }

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
            if (Phase.Value != MatchPhase.Lobby || members.Count < minimum) return false;
            foreach (RoomMember member in members) if (!member.Ready) return false;
            return true;
        }

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
