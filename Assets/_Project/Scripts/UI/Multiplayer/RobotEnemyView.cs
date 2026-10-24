using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace LastProtocol
{
    [DisallowMultipleComponent, RequireComponent(typeof(RobotEnemy))]
    public sealed class RobotEnemyView : NetworkBehaviour
    {
        public TMP_FontAsset font;
        public Transform visual;
        public string displayName = "ROBOT";
        private RobotEnemy enemy;
        private GameObject root;
        private RectTransform fill;
        private TMP_Text text;
        private Renderer[] renderers;
        private Transform viewCamera;
        private float flashUntil;
        private bool flashing;
        private LineRenderer warning;
        private Material warningMaterial;
        public override void OnNetworkSpawn()
        {
            enemy = GetComponent<RobotEnemy>(); renderers = visual.GetComponentsInChildren<Renderer>();
            root = new GameObject("Robot HP", typeof(RectTransform), typeof(Canvas));
            root.transform.SetParent(transform, false); root.transform.localPosition = Vector3.up * 2.7f;
            root.transform.localScale = Vector3.one * 0.006f;
            root.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            ((RectTransform)root.transform).sizeDelta = new Vector2(300, 65);
            var background = new GameObject("HP Background", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            background.SetParent(root.transform, false); background.sizeDelta = new Vector2(280, 18);
            background.GetComponent<Image>().color = new Color(0.04f, 0.04f, 0.04f, 0.95f);
            background.GetComponent<Image>().raycastTarget = false;
            fill = new GameObject("HP Fill", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            fill.SetParent(background, false); fill.anchorMin = Vector2.zero; fill.anchorMax = Vector2.one;
            fill.offsetMin = fill.offsetMax = Vector2.zero; fill.GetComponent<Image>().raycastTarget = false;
            text = new GameObject("Status", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            text.rectTransform.SetParent(root.transform, false); text.rectTransform.sizeDelta = new Vector2(340, 40);
            text.rectTransform.anchoredPosition = new Vector2(0, 32);
            text.font = font; text.fontSize = 23; text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false;
            enemy.Changed += Refresh;
            if (GetComponent<EnemyBrain>() != null && SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                var line = new GameObject("Enemy attack telegraph"); line.transform.SetParent(transform, false);
                warning = line.AddComponent<LineRenderer>(); warning.useWorldSpace = true; warning.startWidth = warning.endWidth = .08f;
                warningMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit")); warningMaterial.SetColor("_BaseColor", new Color(1, .45f, .05f));
                warning.sharedMaterial = warningMaterial; warning.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                warning.enabled = false;
            }
            Refresh(enemy.Snapshot);
        }
        private int previousHealth;
        private void Refresh(EnemyState state)
        {
            if (state.Health.Current < previousHealth) { flashUntil = Time.unscaledTime + 0.12f; flashing = true; }
            previousHealth = state.Health.Current;
            if (text != null) text.text = (GetComponent<EnemyBrain>() != null ? state.Role.ToString() : displayName) + " | " + state.Health.Current + "/" + state.Health.Maximum + " | " + state.Mode
                + (state.Shield > 0 ? " +" + state.Shield : "");
            if (fill != null)
            {
                fill.anchorMax = new Vector2(state.Health.Fraction, 1);
                fill.GetComponent<Image>().color = state.Health.Fraction <= 0.25f ? Color.red : new Color(0.8f, 0.25f, 0.15f);
            }
            visual.localRotation = state.Health.IsDowned ? Quaternion.Euler(0, 0, 85) : Quaternion.identity;
            ApplyColor();
        }
        private void ApplyColor()
        {
            Color color = enemy.Snapshot.Health.IsDowned ? Color.gray : flashing ? Color.white
                : enemy.Snapshot.Mode == EnemyMode.Windup ? new Color(1, 0.75f, 0.1f) : new Color(0.75f, 0.15f, 0.12f);
            var block = new MaterialPropertyBlock(); block.SetColor("_BaseColor", color);
            foreach (var renderer in renderers) renderer.SetPropertyBlock(block);
        }
        private void LateUpdate()
        {
            if (flashing && Time.unscaledTime >= flashUntil) { flashing = false; ApplyColor(); }
            if (root == null) return;
            if (viewCamera == null) viewCamera = Camera.main != null ? Camera.main.transform : null;
            if (viewCamera != null) root.transform.rotation = viewCamera.rotation;
            if (warning != null)
            {
                var s = enemy.Snapshot;
                bool show = !s.Health.IsDowned && s.ActionDuration > 0 && NetworkManager != null && NetworkManager.ServerTime.Time < s.ActionStartedAt + s.ActionDuration;
                warning.enabled = show;
                if (show)
                {
                    warning.loop = s.ActionRadius > 0; warning.positionCount = warning.loop ? 40 : 2;
                    if (warning.loop) for (int i = 0; i < 40; i++)
                    { float a = i * Mathf.PI * 2 / 40; warning.SetPosition(i, new Vector3(s.ActionPoint.x + Mathf.Cos(a) * s.ActionRadius, transform.position.y + .08f, s.ActionPoint.z + Mathf.Sin(a) * s.ActionRadius)); }
                    else { warning.SetPosition(0, enemy.Eye); warning.SetPosition(1, s.ActionPoint); }
                }
            }
        }
        public override void OnNetworkDespawn() => Cleanup();
        public override void OnDestroy() { Cleanup(); base.OnDestroy(); }
        private void Cleanup()
        {
            if (enemy != null) enemy.Changed -= Refresh;
            if (root != null) Destroy(root);
            if (warning != null) Destroy(warning.gameObject); if (warningMaterial != null) Destroy(warningMaterial);
        }
    }
}
