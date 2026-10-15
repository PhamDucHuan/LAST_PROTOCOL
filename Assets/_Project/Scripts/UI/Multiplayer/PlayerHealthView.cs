using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace LastProtocol
{
    [DisallowMultipleComponent, RequireComponent(typeof(PlayerHealth))]
    public sealed class PlayerHealthView : NetworkBehaviour
    {
        public TMP_FontAsset font;
        private PlayerHealth health;
        private GameObject worldRoot, hudRoot;
        private RectTransform worldFill, hudFill;
        private TMP_Text worldText, hudText;
        private Transform viewCamera;

        public override void OnNetworkSpawn()
        {
            health = GetComponent<PlayerHealth>();
            health.Changed += Refresh;
            worldRoot = CreateCanvas("Player Health Bar", RenderMode.WorldSpace);
            worldRoot.transform.SetParent(transform, false);
            worldRoot.transform.localPosition = new Vector3(0, 1.65f, 0);
            worldRoot.transform.localScale = Vector3.one * 0.006f;
            var worldRect = (RectTransform)worldRoot.transform;
            worldRect.sizeDelta = new Vector2(240, 65);
            worldFill = CreateBar(worldRoot.transform, new Vector2(240, 18));
            worldText = Text(worldRoot.transform, new Vector2(260, 38), new Vector2(0, 30), 22);
            if (IsOwner)
            {
                hudRoot = CreateCanvas("Local Player Health HUD", RenderMode.ScreenSpaceOverlay);
                var scaler = hudRoot.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                var panel = new GameObject("Health", typeof(RectTransform)).GetComponent<RectTransform>();
                panel.SetParent(hudRoot.transform, false);
                panel.anchorMin = panel.anchorMax = panel.pivot = Vector2.zero;
                panel.anchoredPosition = new Vector2(190, 65);
                hudFill = CreateBar(panel, new Vector2(320, 24));
                hudText = Text(panel, new Vector2(360, 50), new Vector2(0, 42), 24);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (IsServer) CreateDebugPanel(hudRoot.transform);
#endif
            }
            Refresh(health.Snapshot);
        }
        private void Refresh(HealthState state)
        {
            string value = state.IsDowned ? "DOWNED - 0 / " + state.Maximum : state.Current + " / " + state.Maximum;
            int slot = MultiplayerSession.Instance?.Room?.GetSlot(OwnerClientId) ?? 0;
            if (worldText != null) worldText.text = "Player " + (slot + 1) + " | " + value;
            if (hudText != null) hudText.text = "HP " + value;
            SetFill(worldFill, state.Fraction);
            SetFill(hudFill, state.Fraction);
        }
        private static void SetFill(RectTransform fill, float fraction)
        {
            if (fill == null) return;
            fill.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1);
            fill.GetComponent<Image>().color = fraction <= 0.25f ? new Color(0.95f, 0.2f, 0.2f) : new Color(0.25f, 0.85f, 0.4f);
        }
        private void LateUpdate()
        {
            if (worldRoot == null) return;
            if (viewCamera == null) viewCamera = Camera.main != null ? Camera.main.transform : null;
            if (viewCamera != null) worldRoot.transform.rotation = viewCamera.rotation;
        }
        private static GameObject CreateCanvas(string name, RenderMode mode)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Canvas));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = mode;
            canvas.sortingOrder = 5;
            return root;
        }
        private static RectTransform CreateBar(Transform parent, Vector2 size)
        {
            var background = new GameObject("HP Background", typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)background.transform;
            rect.SetParent(parent, false);
            rect.sizeDelta = size;
            background.GetComponent<Image>().color = new Color(0.03f, 0.04f, 0.06f, 0.9f);
            background.GetComponent<Image>().raycastTarget = false;
            var fill = new GameObject("HP Fill", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            fill.SetParent(rect, false);
            fill.anchorMin = Vector2.zero; fill.anchorMax = Vector2.one;
            fill.offsetMin = fill.offsetMax = Vector2.zero;
            fill.GetComponent<Image>().raycastTarget = false;
            return fill;
        }
        private TMP_Text Text(Transform parent, Vector2 size, Vector2 position, float fontSize)
        {
            var text = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            text.rectTransform.SetParent(parent, false);
            text.rectTransform.sizeDelta = size; text.rectTransform.anchoredPosition = position;
            text.font = font; text.fontSize = fontSize; text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            return text;
        }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void CreateDebugPanel(Transform canvas)
        {
            hudRoot.AddComponent<GraphicRaycaster>();
            var panel = new GameObject("Host Health Test", typeof(RectTransform)).GetComponent<RectTransform>();
            panel.SetParent(canvas, false);
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(1, 0);
            panel.anchoredPosition = new Vector2(-210, 40);
            Text(panel, new Vector2(380, 40), new Vector2(0, 160), 20).text = "HOST TEST | Esc: unlock cursor";
            DebugButton(panel, "SELF: -25 HP", 110, () => health.TryApplyDamageServer(25));
            DebugButton(panel, "ALL: -25 HP", 60, () =>
            {
                foreach (var player in FindObjectsByType<PlayerHealth>()) player.TryApplyDamageServer(25);
            });
            DebugButton(panel, "RESET ALL HP (TEST)", 10, () =>
            {
                foreach (var player in FindObjectsByType<PlayerHealth>()) player.DebugResetHealthServer();
            });
        }
        private void DebugButton(Transform parent, string label, float y, UnityAction action)
        {
            var button = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button)).GetComponent<Button>();
            var rect = (RectTransform)button.transform;
            rect.SetParent(parent, false); rect.sizeDelta = new Vector2(360, 42); rect.anchoredPosition = new Vector2(0, y);
            button.GetComponent<Image>().color = new Color(0.06f, 0.3f, 0.4f, 0.95f);
            button.targetGraphic = button.GetComponent<Image>();
            Text(rect, rect.sizeDelta, Vector2.zero, 21).text = label;
            button.onClick.AddListener(action);
        }
#endif
        public override void OnNetworkDespawn() => Cleanup();
        public override void OnDestroy() { Cleanup(); base.OnDestroy(); }
        private void Cleanup()
        {
            if (health != null) health.Changed -= Refresh;
            if (worldRoot != null) Destroy(worldRoot);
            if (hudRoot != null) Destroy(hudRoot);
        }
    }
}
