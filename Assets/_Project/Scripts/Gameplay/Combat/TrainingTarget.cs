using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace LastProtocol
{
    [DisallowMultipleComponent, RequireComponent(typeof(NetworkObject))]
    public sealed class TrainingTarget : NetworkBehaviour
    {
        public TMP_FontAsset font;
        private readonly NetworkVariable<HealthState> state = new NetworkVariable<HealthState>(default,
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public HealthState Snapshot => state.Value;
        private double resetAt;
        private TextMeshPro label;
        private Transform viewCamera;
        public override void OnNetworkSpawn()
        {
            state.OnValueChanged += OnChanged;
            if (IsServer) state.Value = new HealthState(MultiplayerSession.Instance.config.targetMaxHealth);
            label = new GameObject("Target HP").AddComponent<TextMeshPro>();
            label.transform.SetParent(transform, false); label.transform.localPosition = Vector3.up * 1.6f;
            label.font = font; label.fontSize = 2.5f; label.alignment = TextAlignmentOptions.Center;
            label.rectTransform.sizeDelta = new Vector2(7, 1);
            Refresh();
        }
        public bool TryApplyDamageServer(int amount)
        {
            if (!IsSpawned || !IsServer || MultiplayerSession.Instance == null || !MultiplayerSession.Instance.CanMove) return false;
            var next = Snapshot.Damage(amount);
            if (next.Equals(Snapshot)) return false;
            state.Value = next;
            if (next.IsDowned) resetAt = Time.realtimeSinceStartupAsDouble + MultiplayerSession.Instance.config.targetResetSeconds;
            return true;
        }
        private void Update()
        {
            if (!IsSpawned || !IsServer || !Snapshot.IsDowned || Time.realtimeSinceStartupAsDouble < resetAt) return;
            state.Value = new HealthState(Snapshot.Maximum);
        }
        private void LateUpdate()
        {
            if (label == null) return;
            if (viewCamera == null) viewCamera = Camera.main != null ? Camera.main.transform : null;
            if (viewCamera != null) label.transform.rotation = viewCamera.rotation;
        }
        private void OnChanged(HealthState previous, HealthState next) => Refresh();
        private void Refresh()
        {
            if (label != null) label.text = Snapshot.IsDowned ? "TARGET DOWN - RESETTING" : "TARGET " + Snapshot.Current + " / " + Snapshot.Maximum;
            var renderer = GetComponent<Renderer>();
            var block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", Snapshot.IsDowned ? new Color(0.35f, 0.08f, 0.08f) : new Color(0.15f, 0.75f, 0.85f));
            renderer.SetPropertyBlock(block);
        }
        public override void OnNetworkDespawn()
        {
            state.OnValueChanged -= OnChanged;
            if (label != null) Destroy(label.gameObject);
        }
        public static void SpawnArenaServer(TrainingTarget prefab)
        {
            if (prefab == null || NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;
            if (FindObjectsByType<TrainingTarget>().Length > 0) return;
            for (int i = 0; i < MultiplayerConfig.MaxPlayers; i++)
            {
                var target = Instantiate(prefab, new Vector3(-9 + i * 6, 1, 10), Quaternion.identity);
                target.NetworkObject.Spawn(true);
            }
        }
    }
}
