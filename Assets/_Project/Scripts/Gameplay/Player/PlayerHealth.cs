using System;
using Unity.Netcode;
using UnityEngine;

namespace LastProtocol
{
    [DisallowMultipleComponent, RequireComponent(typeof(NetworkObject))]
    public sealed class PlayerHealth : NetworkBehaviour
    {
        private readonly NetworkVariable<HealthState> state = new NetworkVariable<HealthState>(default,
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public HealthState Snapshot => state.Value;
        public bool CanAct => Snapshot.IsInitialized && !Snapshot.IsDowned;
        public uint DamageRevision { get; private set; }
        public uint LifeRevision { get; private set; }
        public event Action<HealthState> Changed;

        public override void OnNetworkSpawn()
        {
            state.OnValueChanged += OnHealthChanged;
            if (IsServer) state.Value = new HealthState(MultiplayerSession.Instance.config.playerMaxHealth);
            Changed?.Invoke(Snapshot);
        }
        public override void OnNetworkDespawn() => state.OnValueChanged -= OnHealthChanged;
        private void OnHealthChanged(HealthState previous, HealthState current) => Changed?.Invoke(current);

        // Server gameplay calls these after validating its own hit/environment rules.
        // No client RPC accepts a target, damage amount, or requested HP value.
        public bool TryApplyDamageServer(int amount)
        {
            if (!CanChange || !SetState(Snapshot.Damage(amount))) return false;
            DamageRevision++;
            return true;
        }
        public bool TryHealServer(int amount) => CanChange && SetState(Snapshot.Heal(amount));
        internal bool CompleteReviveServer(int amount) => CanChange && SetState(Snapshot.Revive(amount));
        internal bool ResetRoundServer()
        {
            if (!IsSpawned || !IsServer || MultiplayerSession.Instance?.Room?.TeamWiped.Value != true) return false;
            SetState(new HealthState(Snapshot.Maximum));
            return true;
        }
        private bool CanChange => IsSpawned && IsServer && MultiplayerSession.Instance != null
            && MultiplayerSession.Instance.CanMove;
        private bool SetState(HealthState next)
        {
            if (next.Equals(Snapshot)) return false;
            if (next.IsDowned != Snapshot.IsDowned) LifeRevision++;
            state.Value = next;
            return true;
        }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // Explicit test reset, not a gameplay revive mechanic. Never present in release.
        public bool DebugResetHealthServer() => CanChange && SetState(new HealthState(Snapshot.Maximum));
#endif
    }
}
