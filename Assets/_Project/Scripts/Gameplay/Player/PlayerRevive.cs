using System;
using Unity.Netcode;
using UnityEngine;

namespace LastProtocol
{
    [DisallowMultipleComponent, RequireComponent(typeof(PlayerHealth), typeof(UnityEngine.InputSystem.PlayerInput))]
    public sealed class PlayerRevive : NetworkBehaviour
    {
        private readonly NetworkVariable<ReviveState> channel = new NetworkVariable<ReviveState>(default,
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        // Keep wire layout identical between Editor, Development and release builds.
        // These remain zero in normal sessions; only the opt-in Development harness writes them.
        private readonly NetworkVariable<int> validationStage = new NetworkVariable<int>(0);
        private readonly NetworkVariable<uint> validationChecks = new NetworkVariable<uint>(0);
        public ReviveState Snapshot => channel.Value;
        public bool IsChanneling => Snapshot.Active;
        public static event Action AnyChannelChanged;
        private PlayerHealth health;
        private PlayerRevive currentTarget, reservedBy;
        private uint actorDamage, targetLife;
        private double lastHeartbeat;

        public override void OnNetworkSpawn()
        {
            health = GetComponent<PlayerHealth>();
            channel.OnValueChanged += OnChannelChanged;
            AnyChannelChanged?.Invoke();
        }
        private void OnChannelChanged(ReviveState oldValue, ReviveState newValue) => AnyChannelChanged?.Invoke();
        private void Update()
        {
            if (!IsSpawned) return;
            if (!IsServer || !Snapshot.Active) return;
            var session = MultiplayerSession.Instance;
            double now = Time.realtimeSinceStartupAsDouble;
            if (session == null || !session.CanMove || currentTarget == null || !ValidTarget(currentTarget)
                || now - lastHeartbeat > session.config.reviveHeartbeatTimeout || health.DamageRevision != actorDamage
                || currentTarget.health.LifeRevision != targetLife)
            { CancelServer(true); return; }
            if (NetworkManager.ServerTime.Time - Snapshot.StartedAt < Snapshot.Duration) return;
            int hp = ReviveRules.RestoredHealth(currentTarget.health.Snapshot.Maximum, session.config.reviveHealthFraction);
            if (currentTarget.health.CompleteReviveServer(hp))
            {
                var next = Snapshot; next.CompletedRevives++; channel.Value = next;
            }
            CancelServer(true);
        }
        // Client hint only; all real eligibility checks happen again on the Host.
        public PlayerRevive FindNearbyDowned()
        {
            var session = MultiplayerSession.Instance;
            if (session == null) return null;
            PlayerRevive nearest = null; float distance = session.config.reviveRange;
            foreach (var other in FindObjectsByType<PlayerRevive>())
            {
                if (other == this || !other.IsSpawned || other.health == null || !other.health.Snapshot.IsDowned) continue;
                float d = Vector3.Distance(transform.position, other.transform.position);
                if (d <= distance) { distance = d; nearest = other; }
            }
            return nearest;
        }
        internal bool HoldServer(ulong targetId)
        {
            if (!IsSpawned || !IsServer) return false;
            var session = MultiplayerSession.Instance;
            if (session == null || !session.CanMove || !NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(targetId, out var targetObject)
                || !targetObject.IsPlayerObject) return false;
            var target = targetObject.GetComponent<PlayerRevive>();
            if (!ValidTarget(target)) return false;
            if (currentTarget != target)
            {
                CancelServer(false);
                currentTarget = target; target.reservedBy = this;
                actorDamage = health.DamageRevision; targetLife = target.health.LifeRevision;
                var next = Snapshot;
                next.Active = true; next.TargetObjectId = targetId;
                next.StartedAt = NetworkManager.ServerTime.Time; next.Duration = session.config.reviveDuration;
                channel.Value = next;
            }
            lastHeartbeat = Time.realtimeSinceStartupAsDouble;
            return true;
        }
        private bool ValidTarget(PlayerRevive target)
        {
            var session = MultiplayerSession.Instance;
            if (session == null || target == null || !target.IsSpawned || target.health == null) return false;
            bool sight = false;
            Vector3 eye = transform.position + Vector3.up * 0.4f;
            Vector3 delta = target.transform.position + Vector3.up * 0.1f - eye;
            if (delta.sqrMagnitude > 0.001f)
                sight = CombatRules.Raycast(eye, delta.normalized, delta.magnitude + 0.1f, transform, out var hit)
                    && hit.collider.GetComponentInParent<PlayerRevive>() == target;
            return ReviveRules.CanStart(health.CanAct, target.health.Snapshot.IsDowned, target != this,
                target.reservedBy == null || target.reservedBy == this, sight,
                Vector3.Distance(transform.position, target.transform.position), session.config.reviveRange);
        }
        internal void ResetRoundServer()
        {
            if (!IsServer) return;
            CancelServer(false);
        }
        public void CancelServer(bool requireRelease)
        {
            if (!IsServer) return;
            if (currentTarget != null && currentTarget.reservedBy == this) currentTarget.reservedBy = null;
            currentTarget = null;
            GetComponent<PlayerInteraction>()?.OnReviveStoppedServer(requireRelease);
            if (IsSpawned && Snapshot.Active)
            {
                var next = Snapshot; next.Active = false; next.TargetObjectId = 0;
                next.StartedAt = 0; next.Duration = 0; channel.Value = next;
            }
        }
        public override void OnNetworkDespawn()
        {
            channel.OnValueChanged -= OnChannelChanged;
            if (IsServer)
            {
                if (reservedBy != null) reservedBy.CancelServer(true);
                CancelServer(true);
            }
            AnyChannelChanged?.Invoke();
        }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public NetworkVariable<int> TestStage => validationStage;
        public NetworkVariable<uint> TestChecks => validationChecks;
        public void TestHold(ulong targetId) => GetComponent<PlayerInteraction>().TestHold(InteractionKind.Revive, targetId);
        public void TestRelease() => GetComponent<PlayerInteraction>().TestRelease();
#endif
    }
}
