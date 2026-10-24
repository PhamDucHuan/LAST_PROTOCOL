using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LastProtocol
{
    [DisallowMultipleComponent, RequireComponent(typeof(PlayerRevive), typeof(PlayerHealth), typeof(PlayerInput))]
    public sealed class PlayerInteraction : NetworkBehaviour
    {
        private readonly InteractionGate gesture = new();
        private PlayerRevive revive;
        private PlayerHealth health;
        private InputAction interact, move, jump, attack;
        private InteractionKind localKind;
        private ulong localTarget;
        private bool mustRelease = true, claimed, sentHold, hasSequence;
        private uint sequence, lastSequence, damageRevision;
        private double nextSend, nextAcceptedPacket, lastHeartbeat;
        private DataStation currentStation;
        private NetworkChip currentChip;
        private InteractionKind chipAction;
        private uint localAttempt, observedAttempt;
        private NetworkMissionState Mission => MultiplayerSession.Instance?.Room?.GetComponent<NetworkMissionState>();
        private uint Attempt => Mission?.Snapshot.Attempt ?? 0;
        private Vector3 channelPosition;
        public PlayerRevive HintRevive { get; private set; }
        public DataStation HintStation { get; private set; }
        public NetworkChip HintChip { get; private set; }
        public InteractionKind HintChipAction { get; private set; }
        public NetworkChip ActiveChip
        {
            get
            {
                var chip = MultiplayerSession.Instance?.Room?.GetComponent<NetworkChip>();
                return chip != null && chip.Snapshot.Active && chip.Snapshot.Actor == NetworkObjectId ? chip : null;
            }
        }
        private float nextHint;
        public bool IsChanneling => revive?.IsChanneling == true || ActiveStation != null || ActiveChip != null;
        public DataStation ActiveStation
        {
            get
            {
                if (!IsSpawned) return null;
                foreach (var station in FindObjectsByType<DataStation>())
                    if (station.IsSpawned && station.Snapshot.Active && station.Snapshot.Actor == NetworkObjectId) return station;
                return null;
            }
        }
        public override void OnNetworkSpawn()
        {
            revive = GetComponent<PlayerRevive>(); health = GetComponent<PlayerHealth>();
            if (!IsOwner) return;
            var actions = GetComponent<PlayerInput>().actions;
            interact = actions.FindAction("Player/Interact", true); move = actions.FindAction("Player/Move", true);
            jump = actions.FindAction("Player/Jump", true); attack = actions.FindAction("Player/Attack", true);
        }
        private void Update()
        {
            if (!IsSpawned) return;
            if (IsOwner) ReadInput();
            if (!IsServer || (currentStation == null && currentChip == null)) return;
            var session = MultiplayerSession.Instance;
            if (session == null || !session.CanMove || !health.CanAct || health.DamageRevision != damageRevision
                || Time.realtimeSinceStartupAsDouble - lastHeartbeat > session.config.reviveHeartbeatTimeout
                || Vector3.Distance(transform.position, channelPosition) > .2f
                || (currentStation != null && !currentStation.CanUse(this)) || (currentChip != null && !currentChip.CanUse(this, chipAction)))
            { CancelServer(true); return; }
            if (currentChip != null)
            {
                var chip = currentChip.Snapshot;
                if (NetworkManager.ServerTime.Time - chip.StartedAt >= chip.Duration)
                { currentChip.CompleteServer(this, chipAction); currentChip = null; gesture.Stop(true); }
                return;
            }
            var state = currentStation.Snapshot;
            if (NetworkManager.ServerTime.Time - state.StartedAt >= state.Duration)
            { currentStation.CompleteServer(this); currentStation = null; gesture.Stop(true); }
        }
        private void RefreshHint()
        {
            if (Time.unscaledTime < nextHint) return;
            nextHint = Time.unscaledTime + .15f;
            HintChip = null; HintChipAction = InteractionKind.None;
            var chip = MultiplayerSession.Instance?.Room?.GetComponent<NetworkChip>();
            if (health.CanAct && chip != null && chip.InRecoveryRange(transform.position))
            { HintChip = chip; HintChipAction = InteractionKind.RecoverChip; }
            HintRevive = health.CanAct && HintChip == null ? revive.FindNearbyDowned() : null;
            HintStation = null;
            if (!health.CanAct || HintRevive != null || HintChip != null) return;
            if (chip != null && chip.NearDrill(transform.position) && chip.IsCarrier(NetworkObjectId))
            { HintChip = chip; HintChipAction = InteractionKind.RelayDrill; return; }
            float nearest = float.MaxValue;
            foreach (var station in FindObjectsByType<DataStation>())
            {
                if (!station.IsSpawned || !station.InRange(transform.position)) continue;
                float distance = Vector3.Distance(transform.position, station.transform.position);
                if (distance < nearest) { nearest = distance; HintStation = station; }
            }
        }
        private void ReadInput()
        {
            if (observedAttempt != Attempt) { observedAttempt = Attempt; StopLocal(); }
            RefreshHint();
            bool pressed = interact?.IsPressed() == true;
            var session = MultiplayerSession.Instance;
            bool focused = Application.isFocused && Cursor.lockState == CursorLockMode.Locked;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (TestPressed.HasValue) pressed = TestPressed.Value;
            if (TestFocused.HasValue) focused = TestFocused.Value;
#endif
            bool active = session != null && session.CanMove && health.CanAct && focused;
            bool interrupted = move.ReadValue<Vector2>().sqrMagnitude > .01f || jump.WasPressedThisFrame() || attack.IsPressed();
            if (!active || interrupted)
            { StopLocal(); return; }
            if (!pressed)
            {
                // Initial release also arms a fresh network session.
                if (sentHold || claimed || mustRelease) ReleaseInteractionRpc(++sequence, Attempt);
                mustRelease = false; claimed = false; sentHold = false; localKind = InteractionKind.None; return;
            }
            if (mustRelease) return;
            if (!claimed)
            {
                claimed = true;
                localAttempt = Attempt;
                if (HintChip != null) { localKind = HintChipAction; localTarget = HintChip.NetworkObjectId; }
                else if (HintRevive != null) { localKind = InteractionKind.Revive; localTarget = HintRevive.NetworkObjectId; }
                else if (HintStation != null && !HintStation.Snapshot.Completed)
                { localKind = InteractionKind.DataStation; localTarget = HintStation.NetworkObjectId; }
                else localKind = InteractionKind.None;
            }
            if (localKind == InteractionKind.None) return;
            if (Time.realtimeSinceStartupAsDouble < nextSend) return;
            nextSend = Time.realtimeSinceStartupAsDouble + MultiplayerConfig.ReviveInputInterval;
            HoldInteractionRpc(localKind, localTarget, ++sequence, localAttempt); sentHold = true;
        }
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner, Delivery = RpcDelivery.Unreliable)]
        private void HoldInteractionRpc(InteractionKind kind, ulong target, uint requestSequence, uint requestAttempt, RpcParams rpc = default)
        {
            if (!IsServer || rpc.Receive.SenderClientId != OwnerClientId || requestAttempt != Attempt || !AcceptSequence(requestSequence)) return;
            double now = Time.realtimeSinceStartupAsDouble;
            if (now < nextAcceptedPacket) return;
            nextAcceptedPacket = now + MultiplayerConfig.ReviveInputInterval * .5;
            if (!gesture.TryHold(kind, target)) { CancelServer(true); return; }
            if (kind == InteractionKind.Revive)
            {
                if (!revive.HoldServer(target)) CancelServer(true);
                return;
            }
            if (!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(target, out var obj)) { CancelServer(true); return; }
            if (kind == InteractionKind.RecoverChip || kind == InteractionKind.RelayDrill)
            {
                var chip = obj.GetComponent<NetworkChip>();
                if (chip == null || !chip.TryBeginServer(this, kind)) { CancelServer(true); return; }
                if (currentChip == null)
                { currentChip = chip; chipAction = kind; damageRevision = health.DamageRevision; channelPosition = transform.position; }
                lastHeartbeat = now; return;
            }
            if (kind != InteractionKind.DataStation) { CancelServer(true); return; }
            var station = obj.GetComponent<DataStation>();
            if (station == null || !station.TryBeginServer(this)) { CancelServer(true); return; }
            if (currentStation == null)
            { currentStation = station; damageRevision = health.DamageRevision; channelPosition = transform.position; }
            lastHeartbeat = now;
        }
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void ReleaseInteractionRpc(uint requestSequence, uint requestAttempt, RpcParams rpc = default)
        {
            if (!IsServer || rpc.Receive.SenderClientId != OwnerClientId || requestAttempt != Attempt || !AcceptSequence(requestSequence)) return;
            CancelServer(false); gesture.Release();
        }
        private bool AcceptSequence(uint value)
        {
            if (hasSequence && unchecked((int)(value - lastSequence)) <= 0) return false;
            hasSequence = true; lastSequence = value; return true;
        }
        public void CancelServer(bool requireRelease)
        {
            if (!IsServer) return;
            revive?.CancelServer(requireRelease);
            if (currentStation != null) currentStation.CancelServer(this);
            if (currentChip != null) currentChip.CancelServer(this);
            currentStation = null; currentChip = null; gesture.Stop(requireRelease);
        }
        internal void OnReviveStoppedServer(bool requireRelease) => gesture.Stop(requireRelease);
        internal void ResetRoundServer() { CancelServer(true); }
        private void StopLocal()
        {
            mustRelease = true;
            if (IsSpawned && IsOwner && sentHold) ReleaseInteractionRpc(++sequence, Attempt);
            sentHold = false; claimed = false; localKind = InteractionKind.None;
        }
        private void OnApplicationFocus(bool focused) { if (!focused) StopLocal(); }
        private void OnApplicationPause(bool paused) { if (paused) StopLocal(); }
        public override void OnNetworkDespawn() { if (IsServer) CancelServer(true); }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public bool? TestPressed, TestFocused;
        public void TestHold(InteractionKind kind, ulong target) => HoldInteractionRpc(kind, target, ++sequence, Attempt);
        public void TestHoldOldAttempt(InteractionKind kind, ulong target, uint oldAttempt) => HoldInteractionRpc(kind, target, ++sequence, oldAttempt);
        public void TestRelease() => ReleaseInteractionRpc(++sequence, Attempt);
#endif
    }
}
