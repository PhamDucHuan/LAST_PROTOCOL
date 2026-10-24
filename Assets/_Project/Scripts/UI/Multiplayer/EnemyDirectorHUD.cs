using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace LastProtocol
{
    public sealed class EnemyDirectorHUD : MonoBehaviour
    {
        private EnemyDirector source;
        private GameObject root;
        private TMP_Text label;
        public string StatusText => label != null ? label.text : "";
        public void Bind(EnemyDirector director)
        {
            source = director;
            root = new GameObject("DEADLINE Director HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            root.transform.SetParent(transform, false); var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 8;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080);
            var panel = new GameObject("AI pressure", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            panel.SetParent(root.transform, false); panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0, 1);
            panel.anchoredPosition = new Vector2(18, -298); panel.sizeDelta = new Vector2(600, 140);
            panel.GetComponent<Image>().color = new Color(.04f, .025f, .025f, .88f); panel.GetComponent<Image>().raycastTarget = false;
            label = new GameObject("Director status", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            label.rectTransform.SetParent(panel, false); label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(14, 8); label.rectTransform.offsetMax = new Vector2(-14, -8);
            label.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF"); label.fontSize = 21; label.raycastTarget = false;
            label.color = new Color(1, .82f, .5f);
        }
        private void Update()
        {
            if (root == null || source == null) return;
            root.SetActive(source.EnabledForMission && source.GetComponent<NetworkMissionState>().Snapshot.Phase != MissionPhase.None);
            if (!root.activeSelf) return;
            var s = source.Snapshot;
            string decision = s.Modifier == AdaptiveModifier.None ? "Observing squad tactics" : source.NetworkManager.ServerTime.Time < s.ApplyAt ? "WARNING: " + s.Modifier : "Response: " + s.Modifier;
            label.text = $"DEADLINE | {DirectorRules.BandName(s.MissionMeter)} {s.MissionMeter}/100\nEncounter alert {s.EncounterAlert}/100 | Campaign {s.CampaignThreat}\n{decision}\n{s.Tendency} | Cause: {s.Reason}";
        }
        private void OnDestroy() { if (root != null) Destroy(root); }
    }
}
