using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LastProtocol
{
    /// <summary>Local-only diagnostics and optional rendering budget; never changes project assets.</summary>
    [DisallowMultipleComponent]
    public sealed class ClientPerformance : MonoBehaviour
    {
        public bool Lightweight { get; private set; }
        public float FramesPerSecond { get; private set; }
        public float WorstFrameMilliseconds { get; private set; }
        public ulong RoundTripMilliseconds { get; private set; }
        private TMP_Text label;
        private GameObject overlay;
        private UniversalRenderPipelineAsset sourcePipeline, runtimePipeline;
        private RenderPipelineAsset originalQualityPipeline;
        private readonly Dictionary<UniversalAdditionalCameraData, bool> cameraPostProcessing = new();
        private int frames;
        private float sampleTime, worstFrame;
        private bool visible = true;
        private int originalFrameRate;
        private bool originalBackground;

        private void Awake()
        {
            originalFrameRate = Application.targetFrameRate;
            originalBackground = Application.runInBackground;
            // Avoid an uncapped renderer starving the Host's network/physics work.
            if (!Application.isEditor) Application.targetFrameRate = 60;
            Application.runInBackground = true;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        private void Start()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) return;
            overlay = new GameObject("Local Performance", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            overlay.transform.SetParent(transform, false);
            var canvas = overlay.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 40;
            var scaler = overlay.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            var panel = new GameObject("Stats", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            panel.SetParent(overlay.transform, false);
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(1, 1);
            panel.anchoredPosition = new Vector2(-12, -94); panel.sizeDelta = new Vector2(440, 80);
            panel.GetComponent<Image>().color = new Color(0, 0, 0, 0.7f);
            panel.GetComponent<Image>().raycastTarget = false;
            label = new GameObject("Metrics", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            label.rectTransform.SetParent(panel, false);
            label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(10, 4); label.rectTransform.offsetMax = new Vector2(-6, -4);
            label.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            label.fontSize = 20; label.raycastTarget = false;
        }
        private void Update()
        {
            if (Keyboard.current?.f8Key.wasPressedThisFrame == true && Application.isFocused)
                SetLightweight(!Lightweight);
            if (Keyboard.current?.f9Key.wasPressedThisFrame == true && Application.isFocused)
            { visible = !visible; if (overlay != null) overlay.SetActive(visible); }
            frames++;
            sampleTime += Time.unscaledDeltaTime;
            worstFrame = Mathf.Max(worstFrame, Time.unscaledDeltaTime);
            if (sampleTime < 0.5f) return;
            FramesPerSecond = frames / sampleTime;
            WorstFrameMilliseconds = worstFrame * 1000;
            frames = 0; sampleTime = worstFrame = 0;
            RoundTripMilliseconds = GetRtt();
            if (label != null && visible)
            {
                var manager = NetworkManager.Singleton;
                string role = manager != null && manager.IsHost ? "HOST max RTT" : "RTT";
                string rtt = RoundTripMilliseconds > 0 ? RoundTripMilliseconds + " ms" : "--";
                label.text = $"{FramesPerSecond:0} FPS | peak {WorstFrameMilliseconds:0} ms | {role} {rtt}\n"
                    + $"F8: {(Lightweight ? "LIGHT" : "NORMAL")} | F9: stats | v{MultiplayerConfig.ProtocolVersion}";
            }
        }
        private static ulong GetRtt()
        {
            var manager = NetworkManager.Singleton;
            if (manager == null || !manager.IsListening) return 0;
            var transport = manager.NetworkConfig.NetworkTransport;
            if (!manager.IsServer) return transport.GetCurrentRtt(NetworkManager.ServerClientId);
            ulong maximum = 0;
            foreach (ulong id in manager.ConnectedClientsIds)
                if (id != manager.LocalClientId) maximum = System.Math.Max(maximum, transport.GetCurrentRtt(id));
            return maximum;
        }
        public void SetLightweight(bool enabled)
        {
            if (Lightweight == enabled) return;
            if (enabled)
            {
                sourcePipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
                if (sourcePipeline == null) return;
                originalQualityPipeline = QualitySettings.renderPipeline;
                runtimePipeline = Instantiate(sourcePipeline);
                runtimePipeline.name = "LastProtocol Local Lightweight (runtime only)";
                runtimePipeline.renderScale = Mathf.Min(sourcePipeline.renderScale, 0.7f);
                runtimePipeline.shadowDistance = Mathf.Min(sourcePipeline.shadowDistance, 25);
                runtimePipeline.shadowCascadeCount = 1;
                runtimePipeline.mainLightShadowmapResolution = Mathf.Min(sourcePipeline.mainLightShadowmapResolution, 1024);
                QualitySettings.renderPipeline = runtimePipeline;
            }
            else RestoreRendering();
            Lightweight = enabled;
            if (enabled) ApplyCameraBudget();
        }
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (Lightweight) ApplyCameraBudget();
        }
        private void ApplyCameraBudget()
        {
            foreach (var data in FindObjectsByType<UniversalAdditionalCameraData>())
            {
                if (!cameraPostProcessing.ContainsKey(data)) cameraPostProcessing.Add(data, data.renderPostProcessing);
                data.renderPostProcessing = false;
            }
        }
        private void RestoreRendering()
        {
            if (runtimePipeline != null)
            {
                QualitySettings.renderPipeline = originalQualityPipeline;
                Destroy(runtimePipeline); runtimePipeline = null;
            }
            foreach (var entry in cameraPostProcessing)
                if (entry.Key != null) entry.Key.renderPostProcessing = entry.Value;
            cameraPostProcessing.Clear();
        }
        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            RestoreRendering();
            if (overlay != null) Destroy(overlay);
            Application.targetFrameRate = originalFrameRate;
            Application.runInBackground = originalBackground;
        }
    }
}
