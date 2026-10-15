using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LastProtocol
{
    [RequireComponent(typeof(NetworkObject), typeof(NetworkTransform), typeof(PlayerMovement))]
    [RequireComponent(typeof(PlayerInput), typeof(CharacterController))]
    public sealed class NetworkPlayerController : NetworkBehaviour
    {
        public Transform cameraTarget;
        private PlayerInput playerInput;
        private PlayerMovement movement;
        private PlayerHealth health;
        private InputAction moveAction;
        private InputAction sprintAction;
        private InputAction jumpAction;
        private Vector2 serverInput;
        private bool serverSprint;
        private bool pendingServerJump;
        private uint localJumpCounter;
        private uint lastJumpCounter;
        private float sendTimer;
        private double lastAcceptedTime = double.NegativeInfinity;
        private uint sequence;
        private uint lastSequence;
        private bool hasSequence;

        private void Awake()
        {
            playerInput = GetComponent<PlayerInput>();
            playerInput.enabled = false;
            movement = GetComponent<PlayerMovement>();
            health = GetComponent<PlayerHealth>();
        }
        public override void OnNetworkSpawn()
        {
            GetComponent<CharacterController>().enabled = IsServer;
            movement.enabled = IsServer;
            playerInput.enabled = IsOwner;
            if (IsOwner)
            {
                playerInput.SwitchCurrentActionMap("Player");
                moveAction = playerInput.actions.FindAction("Player/Move", true);
                sprintAction = playerInput.actions.FindAction("Player/Sprint", true);
                jumpAction = playerInput.actions.FindAction("Player/Jump", true);
                var camera = FindAnyObjectByType<LocalPlayerCamera>();
                if (camera != null) camera.Bind(cameraTarget);
            }
            // Visual slot identity without modifying a shared material asset.
            int slot = MultiplayerSession.Instance?.Room?.GetSlot(OwnerClientId) ?? 0;
            var renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                var properties = new MaterialPropertyBlock();
                properties.SetColor("_BaseColor", Color.HSVToRGB(Mathf.Repeat(slot * 0.23f, 1), 0.7f, 0.95f));
                renderer.SetPropertyBlock(properties);
            }
        }
        private void Update()
        {
            if (!IsSpawned || !IsOwner || MultiplayerSession.Instance == null) return;
            bool controlsActive = MultiplayerSession.Instance.CanMove && (health == null || health.CanAct) && Application.isFocused
                && Cursor.lockState == CursorLockMode.Locked;
            // Sample button edges every render frame, even between network sends.
            if (controlsActive && jumpAction?.WasPressedThisFrame() == true) localJumpCounter++;
            sendTimer += Time.unscaledDeltaTime;
            if (sendTimer < MultiplayerConfig.InputInterval) return;
            sendTimer %= MultiplayerConfig.InputInterval;
            Vector2 input = Vector2.zero;
            bool sprint = false;
            if (controlsActive && moveAction != null)
            {
                sprint = sprintAction?.IsPressed() == true;
                Vector2 raw = Vector2.ClampMagnitude(moveAction.ReadValue<Vector2>(), 1);
                Transform camera = UnityEngine.Camera.main?.transform;
                float yaw = camera != null ? camera.eulerAngles.y : 0;
                Vector3 world = Quaternion.Euler(0, yaw, 0) * new Vector3(raw.x, 0, raw.y);
                input = new Vector2(world.x, world.z);
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Smoke tests may deliberately send input while downed to verify the server gate.
            if (TestInput.HasValue && MultiplayerSession.Instance.CanMove)
            { input = TestInput.Value; sprint = TestSprint; controlsActive = true; }
#endif
            SubmitInputRpc(input, sprint, localJumpCounter, controlsActive, ++sequence);
        }
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner, Delivery = RpcDelivery.Unreliable)]
        private void SubmitInputRpc(Vector2 input, bool sprint, uint jumpCounter, bool controlsActive,
            uint inputSequence, RpcParams rpcParams = default)
        {
            var session = MultiplayerSession.Instance;
            if (!IsServer || session == null || rpcParams.Receive.SenderClientId != OwnerClientId || !ConnectionPolicy.IsFinite(input)) return;
            controlsActive &= health == null || health.CanAct;
            if (hasSequence && unchecked((int)(inputSequence - lastSequence)) <= 0) return;
            double now = Time.realtimeSinceStartupAsDouble;
            if (now - lastAcceptedTime < MultiplayerConfig.InputInterval * 0.5) return;
            lastAcceptedTime = now;
            lastSequence = inputSequence;
            hasSequence = true;
            serverInput = controlsActive ? Vector2.ClampMagnitude(input, 1) : Vector2.zero;
            serverSprint = controlsActive && sprint;
            // Repeated counters survive lost UDP packets but cannot re-trigger a jump.
            if (unchecked((int)(jumpCounter - lastJumpCounter)) > 0)
            {
                lastJumpCounter = jumpCounter;
                pendingServerJump |= controlsActive && session.CanMove;
            }
            if (!controlsActive) pendingServerJump = false;
        }
        private void FixedUpdate()
        {
            if (!IsSpawned || !IsServer || MultiplayerSession.Instance == null) return;
            var session = MultiplayerSession.Instance;
            bool fresh = session.CanMove && (health == null || health.CanAct)
                && Time.realtimeSinceStartupAsDouble - lastAcceptedTime < 0.25;
            movement.Simulate(fresh ? serverInput : Vector2.zero, fresh && serverSprint,
                fresh && pendingServerJump, session.config, Time.fixedDeltaTime);
            // Airborne/cooldown requests are consumed, never queued until landing.
            pendingServerJump = false;
        }
        public override void OnNetworkDespawn()
        {
            if (playerInput != null) playerInput.enabled = false;
            if (IsOwner)
            {
                var camera = FindAnyObjectByType<LocalPlayerCamera>();
                if (camera != null) camera.Unbind(cameraTarget);
            }
        }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public Vector2? TestInput { get; set; }
        public bool TestSprint { get; set; }
        public void RequestTestJump() => localJumpCounter++;
#endif
    }
}
