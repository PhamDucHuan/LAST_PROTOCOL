using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastProtocol
{
    // Local presentation only. No trigger/RPC here can advance the mission.
    public sealed class MissionHUD : MonoBehaviour
    {
        private NetworkMissionState mission;
        private GameObject root, marker;
        private TMP_Text title, objective, progress, worldLabel;
        private LineRenderer ring;
        public bool Visible => root != null && root.activeSelf;
        public string ObjectiveText => objective != null ? objective.text : string.Empty;
        public void Bind(NetworkMissionState source)
        {
            mission = source;
            var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            root = new GameObject("Squad Mission HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            root.transform.SetParent(transform, false);
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            root.GetComponent<Canvas>().sortingOrder = 8;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            var panel = new GameObject("Mission", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            panel.SetParent(root.transform, false);
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0, 1);
            panel.anchoredPosition = new Vector2(18, -94); panel.sizeDelta = new Vector2(600, 150);
            panel.GetComponent<Image>().color = new Color(0.015f, 0.035f, 0.055f, 0.88f);
            panel.GetComponent<Image>().raycastTarget = false;
            title = Label(panel, font, -18, 24, new Color(0.25f, 0.9f, 0.95f));
            objective = Label(panel, font, -56, 24, Color.white);
            progress = Label(panel, font, -97, 20, new Color(0.8f, 0.88f, 0.92f));
            marker = new GameObject("Local Mission Destination"); marker.transform.SetParent(transform, false);
            ring = marker.AddComponent<LineRenderer>(); ring.useWorldSpace = true; ring.loop = true;
            ring.positionCount = 64; ring.startWidth = ring.endWidth = 0.14f;
            ring.sharedMaterial = source.markerMaterial;
            ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ring.receiveShadows = false;
            worldLabel = new GameObject("Destination Name", typeof(TextMeshPro)).GetComponent<TextMeshPro>();
            worldLabel.transform.SetParent(marker.transform, false);
            worldLabel.font = font; worldLabel.fontSize = 3.5f; worldLabel.alignment = TextAlignmentOptions.Center;
            worldLabel.rectTransform.sizeDelta = new Vector2(18, 3); worldLabel.color = new Color(0.3f, 1, 1);
            mission.Changed += Refresh; Refresh(mission.Snapshot);
        }
        private static TMP_Text Label(RectTransform parent, TMP_FontAsset font, float y, int size, Color color)
        {
            var text = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            text.rectTransform.SetParent(parent, false);
            text.rectTransform.anchorMin = text.rectTransform.anchorMax = text.rectTransform.pivot = new Vector2(0, 1);
            text.rectTransform.anchoredPosition = new Vector2(14, y); text.rectTransform.sizeDelta = new Vector2(572, 46);
            text.font = font; text.fontSize = size; text.color = color; text.raycastTarget = false;
            text.alignment = TextAlignmentOptions.TopLeft;
            return text;
        }
        private void Refresh(MissionSnapshot snapshot)
        {
            var definition = mission.Definition;
            bool visible = definition != null && snapshot.Phase != MissionPhase.None;
            root.SetActive(visible); marker.SetActive(visible && snapshot.Phase == MissionPhase.Active);
            if (!visible) return;
            title.text = definition.title + $" | {Mathf.Min(snapshot.ObjectiveIndex + 1, definition.objectives.Length)}/{definition.objectives.Length}";
            if (snapshot.Phase == MissionPhase.Completed)
            { objective.text = "RECON COMPLETE"; progress.text = definition.completionText; return; }
            if (snapshot.Phase == MissionPhase.Failed)
            {
                objective.text = "RECON FAILED";
                progress.text = snapshot.Failure == MissionFailure.TeamWiped
                    ? "Squad down. Host can restart the round."
                    : "Not enough players. Leave the session and regroup.";
                return;
            }
            var current = definition.objectives[snapshot.ObjectiveIndex];
            objective.text = current.title;
            progress.text = $"Squad {snapshot.Inside}/{snapshot.Required} | Hold {snapshot.HeldSeconds:0.0}/{current.holdSeconds:0.0}s\n{current.instruction}";
            for (int i = 0; i < ring.positionCount; i++)
            {
                float angle = i * Mathf.PI * 2 / ring.positionCount;
                // Campus pavers reach 0.135 m; keep the ring above their visible surface.
                ring.SetPosition(i, current.center + new Vector3(Mathf.Cos(angle) * current.radius, 0.22f, Mathf.Sin(angle) * current.radius));
            }
            worldLabel.transform.position = current.center + Vector3.up * 3;
            worldLabel.text = current.title;
        }
        private void LateUpdate()
        {
            if (worldLabel != null && marker.activeSelf && Camera.main != null)
                worldLabel.transform.rotation = Camera.main.transform.rotation;
        }
        private void OnDestroy()
        {
            if (mission != null) mission.Changed -= Refresh;
            if (root != null) Destroy(root);
            if (marker != null) Destroy(marker);
        }
    }
}
