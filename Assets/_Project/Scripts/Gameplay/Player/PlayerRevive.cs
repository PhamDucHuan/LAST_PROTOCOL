using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LastProtocol
{
    [DisallowMultipleComponent, RequireComponent(typeof(PlayerHealth), typeof(PlayerInput))]
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
        private InputAction interact, move, jump, attack;
        private PlayerRevive localTarget;
        private bool mustRelease = true, sentHold, serverRequiresRelease, hasSequence;
        private uint sequence, lastSequence, actorDamage, targetLife;
        private double nextSend, lastHeartbeat, nextAcceptedPacket;

        public override void OnNetworkSpawn()
        {
            health = GetComponent<PlayerHealth>();
            channel.OnValueChanged += OnChannelChanged;
            if (IsOwner)
            {
                var actions = GetComponent<PlayerInput>().actions;
                interact = actions.FindAction("Player/Interact", true); move = actions.FindAction("Player/Move", true);
                jump = actions.FindAction("Player/Jump", true); attack = actions.FindAction("Player/Attack", true);
            }
            AnyChannelChanged?.Invoke();
        }
        private void OnChannelChanged(ReviveState oldValue, ReviveState newValue) => AnyChannelChanged?.Invoke();
        private void Update()
        {
            if (!IsSpawned) return;
            if (IsOwner) ReadOwnerInput();
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
        private void ReadOwnerInput()
        {
            var session = MultiplayerSession.Instance;
            bool pressed = interact?.IsPressed() == true;
            bool active = session != null && session.CanMove && health.CanAct && Application.isFocused
                && Cursor.lockState == CursorLockMode.Locked;
            bool interrupted = move?.ReadValue<Vector2>().sqrMagnitude > 0.01f || jump?.WasPressedThisFrame() == true || attack?.IsPressed() == true;
            if (!active || interrupted) { mustRelease = true; localTarget = null; }
            else if (!pressed) { mustRelease = false; localTarget = null; }
            else if (!mustRelease)
            {
                if (localTarget == null) localTarget = FindNearbyDowned();
                if (localTarget != null && !localTarget.health.Snapshot.IsDowned) { localTarget = null; mustRelease = true; }
            }
            bool holding = pressed && active && !mustRelease && localTarget != null;
            if (!holding)
            {
                if (sentHold) { ReleaseReviveRpc(++sequence); sentHold = false; }
                return;
            }
            double now = Time.realtimeSinceStartupAsDouble;
            if (now < nextSend) return;
            nextSend = now + MultiplayerConfig.ReviveInputInterval;
            HoldReviveRpc(localTarget.NetworkObjectId, ++sequence); sentHold = true;
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
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner, Delivery = RpcDelivery.Unreliable)]
        private void HoldReviveRpc(ulong targetId, uint requestSequence, RpcParams rpc = default)
        {
            if (!IsServer || rpc.Receive.SenderClientId != OwnerClientId || !AcceptSequence(requestSequence)) return;
            if (serverRequiresRelease) return;
            double now = Time.realtimeSinceStartupAsDouble;
            if (now < nextAcceptedPacket) return;
            nextAcceptedPacket = now + MultiplayerConfig.ReviveInputInterval * 0.5;
            var session = MultiplayerSession.Instance;
            if (session == null || !session.CanMove || !NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(targetId, out var targetObject)
                || !targetObject.IsPlayerObject)
            { CancelServer(true); return; }
            var target = targetObject.GetComponent<PlayerRevive>();
            if (!ValidTarget(target)) { CancelServer(true); return; }
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
            lastHeartbeat = now;
        }
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void ReleaseReviveRpc(uint requestSequence, RpcParams rpc = default)
        {
            if (!IsServer || rpc.Receive.SenderClientId != OwnerClientId || !AcceptSequence(requestSequence)) return;
            CancelServer(false); serverRequiresRelease = false;
        }
        private bool AcceptSequence(uint value)
        {
            if (hasSequence && unchecked((int)(value - lastSequence)) <= 0) return false;
            hasSequence = true; lastSequence = value; return true;
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
            serverRequiresRelease = false;
        }
        public void CancelServer(bool requireRelease)
        {
            if (!IsServer) return;
            if (currentTarget != null && currentTarget.reservedBy == this) currentTarget.reservedBy = null;
            currentTarget = null;
            serverRequiresRelease |= requireRelease;
            if (IsSpawned && Snapshot.Active)
            {
                var next = Snapshot; next.Active = false; next.TargetObjectId = 0;
                next.StartedAt = 0; next.Duration = 0; channel.Value = next;
            }
        }
        private void OnApplicationFocus(bool focused) { if (!focused) StopLocal(); }
        private void OnApplicationPause(bool paused) { if (paused) StopLocal(); }
        private void StopLocal()
        {
            mustRelease = true; localTarget = null;
            if (IsSpawned && IsOwner && sentHold) { ReleaseReviveRpc(++sequence); sentHold = false; }
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
        public void TestHold(ulong targetId) => HoldReviveRpc(targetId, ++sequence);
        public void TestRelease() => ReleaseReviveRpc(++sequence);
#endif
    }
}
