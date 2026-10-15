using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace LastProtocol
{
    [DisallowMultipleComponent, RequireComponent(typeof(NetworkHitscanWeapon))]
    public sealed class WeaponHUD : NetworkBehaviour
    {
        public TMP_FontAsset font;
        private GameObject root;
        private TMP_Text crosshair, hitMarker;
        private NetworkHitscanWeapon weapon;
        private PlayerHealth health;
        private float hideHitAt;
        public override void OnNetworkSpawn()
        {
            if (!IsOwner) return;
            weapon = GetComponent<NetworkHitscanWeapon>(); health = GetComponent<PlayerHealth>();
            weapon.ShotObserved += OnShot; health.Changed += OnHealth;
            root = new GameObject("Local Weapon HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            root.GetComponent<Canvas>().sortingOrder = 6;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            crosshair = Label(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40, 40), 30, "+");
            hitMarker = Label(new Vector2(0.5f, 0.5f), new Vector2(0, 45), new Vector2(160, 35), 22, "");
            hitMarker.color = new Color(1, 0.65f, 0.15f);
            var config = MultiplayerSession.Instance.config;
            Label(new Vector2(0.5f, 0), new Vector2(0, 55), new Vector2(850, 40), 20,
                "LMB: FIRE | " + config.weaponDamage + " DMG | " + config.weaponShotsPerSecond
                + " SHOTS/S | FRIENDLY FIRE " + (config.friendlyFire ? "ON" : "OFF"));
            OnHealth(health.Snapshot);
        }
        private TMP_Text Label(Vector2 anchor, Vector2 position, Vector2 size, float fontSize, string value)
        {
            var text = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            text.rectTransform.SetParent(root.transform, false);
            text.rectTransform.anchorMin = text.rectTransform.anchorMax = anchor;
            text.rectTransform.anchoredPosition = position; text.rectTransform.sizeDelta = size;
            text.font = font; text.fontSize = fontSize; text.alignment = TextAlignmentOptions.Center;
            text.text = value; text.raycastTarget = false;
            return text;
        }
        private void OnShot(bool damaged)
        {
            if (!damaged || hitMarker == null) return;
            hitMarker.text = "HIT"; hideHitAt = Time.unscaledTime + 0.2f;
        }
        private void OnHealth(HealthState state)
        {
            if (crosshair != null) crosshair.color = state.IsDowned ? Color.gray : Color.white;
        }
        private void Update()
        {
            if (hitMarker != null && hitMarker.text.Length > 0 && Time.unscaledTime >= hideHitAt) hitMarker.text = "";
        }
        public override void OnNetworkDespawn() => Cleanup();
        public override void OnDestroy() { Cleanup(); base.OnDestroy(); }
        private void Cleanup()
        {
            if (weapon != null) weapon.ShotObserved -= OnShot;
            if (health != null) health.Changed -= OnHealth;
            if (root != null) Destroy(root);
        }
    }
}
