using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LastProtocol
{
    [DisallowMultipleComponent, RequireComponent(typeof(PlayerHealth))]
    public sealed class NetworkHitscanWeapon : NetworkBehaviour
    {
        public TrainingTarget trainingTargetPrefab;
        public Material tracerMaterial;
        private PlayerHealth health;
        private InputAction attack;
        private ShotGate shotGate;
        private uint sequence;
        private double nextLocalShot;
        private bool fireArmed;
        private LineRenderer tracer;
        private float hideTracerAt;
        public uint ObservedShots { get; private set; }
        public uint ObservedDamagingHits { get; private set; }
        public event Action<bool> ShotObserved;
        public Vector3 Eye => transform.position + Vector3.up * 0.6f;

        public override void OnNetworkSpawn()
        {
            health = GetComponent<PlayerHealth>();
            shotGate = new ShotGate();
            if (IsOwner) attack = GetComponent<PlayerInput>().actions.FindAction("Player/Attack", true);
            var line = new GameObject("Shot Tracer");
            line.transform.SetParent(transform, false);
            tracer = line.AddComponent<LineRenderer>();
            tracer.sharedMaterial = tracerMaterial;
            tracer.useWorldSpace = true; tracer.positionCount = 2;
            tracer.startWidth = tracer.endWidth = 0.025f;
            tracer.enabled = false;
        }
        private void Update()
        {
            if (tracer != null && tracer.enabled && Time.unscaledTime >= hideTracerAt) tracer.enabled = false;
            if (!IsSpawned || !IsOwner) return;
            var session = MultiplayerSession.Instance;
            bool active = session != null && session.CanMove && health.CanAct && Application.isFocused
                && Cursor.lockState == CursorLockMode.Locked;
            if (!active) { fireArmed = false; return; }
            bool pressed = attack?.IsPressed() == true;
            // A click that only recaptures the cursor must not also fire the weapon.
            if (!fireArmed) { fireArmed = !pressed; return; }
            if (!pressed || Time.realtimeSinceStartupAsDouble < nextLocalShot) return;
            var camera = Camera.main;
            if (camera == null) return;
            nextLocalShot = Time.realtimeSinceStartupAsDouble + 1f / session.config.weaponShotsPerSecond;
            Ray ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f));
            RequestFireRpc(ray.origin, ray.direction, ++sequence);
        }
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void RequestFireRpc(Vector3 cameraOrigin, Vector3 aimDirection, uint shotSequence, RpcParams rpc = default)
        {
            var session = MultiplayerSession.Instance;
            if (!IsServer || !IsSpawned || session == null || !session.CanMove || !health.CanAct
                || rpc.Receive.SenderClientId != OwnerClientId) return;
            var config = session.config;
            if (!CombatRules.ValidAim(Eye, cameraOrigin, aimDirection, config.cameraDistance + 2)) return;
            if (!shotGate.TryAccept(Time.realtimeSinceStartupAsDouble, shotSequence, 1f / config.weaponShotsPerSecond)) return;
            Vector3 cameraOffset = cameraOrigin - Eye;
            // A submitted camera cannot be on the other side of cover from the server's player.
            if (cameraOffset.magnitude > 0.01f && CombatRules.Raycast(Eye, cameraOffset.normalized,
                cameraOffset.magnitude, transform, out _)) return;
            Vector3 direction = aimDirection.normalized;
            float aimDistance = config.weaponRange + cameraOffset.magnitude;
            Vector3 aimPoint = cameraOrigin + direction * aimDistance;
            if (CombatRules.Raycast(cameraOrigin, direction, aimDistance, transform, out var cameraHit))
                aimPoint = cameraHit.point;
            Vector3 shotDirection = aimPoint - Eye;
            if (shotDirection.sqrMagnitude < 0.0001f) return;
            float distance = Mathf.Min(shotDirection.magnitude, config.weaponRange);
            Vector3 endpoint = Eye + shotDirection.normalized * distance;
            bool damaged = false;
            // The authoritative muzzle ray resolves close cover, range and actual damage.
            if (CombatRules.Raycast(Eye, shotDirection.normalized, Mathf.Min(distance + 0.01f, config.weaponRange), transform, out var hit))
            {
                endpoint = hit.point;
                var target = hit.collider.GetComponentInParent<TrainingTarget>();
                var player = hit.collider.GetComponentInParent<PlayerHealth>();
                if (target != null) damaged = target.TryApplyDamageServer(config.weaponDamage);
                else if (player != null && player != health && config.friendlyFire)
                    damaged = player.TryApplyDamageServer(config.weaponDamage);
            }
            ShotFeedbackRpc(Eye, endpoint, damaged);
        }
        [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
        private void ShotFeedbackRpc(Vector3 origin, Vector3 endpoint, bool damaged)
        {
            ObservedShots++;
            if (damaged) ObservedDamagingHits++;
            if (tracer != null)
            {
                tracer.SetPosition(0, origin); tracer.SetPosition(1, endpoint);
                tracer.enabled = true; hideTracerAt = Time.unscaledTime + 0.08f;
            }
            ShotObserved?.Invoke(damaged);
        }
        public override void OnNetworkDespawn()
        {
            attack = null; fireArmed = false;
            if (tracer != null) Destroy(tracer.gameObject);
        }
        private void OnApplicationFocus(bool focused) => fireArmed = false;
        private void OnApplicationPause(bool paused) => fireArmed = false;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // Opt-in harness uses the same owner RPC and server validation as actual mouse input.
        public void TestFire(Vector3 origin, Vector3 direction) => RequestFireRpc(origin, direction, ++sequence);
#endif
    }
}
