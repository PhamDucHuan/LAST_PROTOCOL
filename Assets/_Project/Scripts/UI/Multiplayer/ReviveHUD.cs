using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace LastProtocol
{
    [DisallowMultipleComponent, RequireComponent(typeof(PlayerRevive))]
    public sealed class ReviveHUD : NetworkBehaviour
    {
        public TMP_FontAsset font;
        private PlayerRevive revive;
        private PlayerHealth health;
        private GameObject root;
        private TMP_Text prompt;
        private Button restart;
        private float nextHintRefresh;
        private string hint = "";

        public override void OnNetworkSpawn()
        {
            if (!IsOwner) return;
            revive = GetComponent<PlayerRevive>(); health = GetComponent<PlayerHealth>();
            root = new GameObject("Local Revive HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            root.GetComponent<Canvas>().sortingOrder = 8;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            prompt = Label(root.transform, new Vector2(0, -140), new Vector2(1100, 100), 28);
            var button = new GameObject("Host Restart", typeof(RectTransform), typeof(Image), typeof(Button));
            button.transform.SetParent(root.transform, false);
            var rect = (RectTransform)button.transform; rect.anchoredPosition = new Vector2(0, -230); rect.sizeDelta = new Vector2(440, 55);
            button.GetComponent<Image>().color = new Color(0.05f, 0.4f, 0.5f);
            restart = button.GetComponent<Button>(); restart.targetGraphic = button.GetComponent<Image>();
            Label(button.transform, Vector2.zero, rect.sizeDelta, 25).text = "RESTART TEST (HOST)";
            restart.onClick.AddListener(() => MultiplayerSession.Instance?.Room?.RestartRoundServer());
            restart.gameObject.SetActive(false);
        }
        private TMP_Text Label(Transform parent, Vector2 position, Vector2 size, float fontSize)
        {
            var label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            label.rectTransform.SetParent(parent, false); label.rectTransform.anchoredPosition = position; label.rectTransform.sizeDelta = size;
            label.font = font; label.fontSize = fontSize; label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
            return label;
        }
        private void Update()
        {
            if (root == null || !IsSpawned) return;
            var room = MultiplayerSession.Instance?.Room;
            bool wiped = room?.TeamWiped.Value == true;
            restart.gameObject.SetActive(wiped && IsServer);
            string text;
            if (wiped) text = "TEAM DOWN\n" + (IsServer ? "Restart the test or leave the session." : "Waiting for Host to restart the test.");
            else
            {
                PlayerRevive channel = revive.IsChanneling ? revive : null;
                if (health.Snapshot.IsDowned)
                    foreach (var other in FindObjectsByType<PlayerRevive>())
                        if (other.IsChanneling && other.Snapshot.TargetObjectId == NetworkObjectId) { channel = other; break; }
                if (channel != null)
                {
                    var state = channel.Snapshot;
                    int percent = Mathf.Clamp(Mathf.FloorToInt((float)((NetworkManager.ServerTime.Time - state.StartedAt) / state.Duration) * 100), 0, 99);
                    text = (health.Snapshot.IsDowned ? "TEAMMATE IS REVIVING YOU" : "REVIVING TEAMMATE - HOLD E") + "\n" + percent + "%";
                }
                else if (health.Snapshot.IsDowned) text = "DOWNED\nWait for a teammate to hold E nearby.";
                else
                {
                    // Proximity is a local hint; refresh at 5 Hz instead of searching every frame.
                    if (Time.unscaledTime >= nextHintRefresh)
                    { nextHintRefresh = Time.unscaledTime + 0.2f; hint = revive.FindNearbyDowned() != null ? "HOLD E TO REVIVE | Stay still and avoid damage" : ""; }
                    text = hint;
                }
            }
            if (prompt.text != text) prompt.text = text;
        }
        public override void OnNetworkDespawn() => Cleanup();
        public override void OnDestroy() { Cleanup(); base.OnDestroy(); }
        private void Cleanup() { if (root != null) Destroy(root); }
    }
}
