#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

namespace LastProtocol
{
    // Opt-in graphical preview + button checks. No authentication or Relay requests.
    public static class MenuPreviewHarness
    {
        private static bool started;
        [Serializable] private sealed class Report
        { public List<string> passed = new List<string>(), failures = new List<string>(); public bool complete; }
        public static void TryStart(MenuFrontEnd ui)
        {
            var args = Environment.GetCommandLineArgs(); int index = Array.IndexOf(args, "--lp-menu-preview");
            if (started || index < 0 || index + 1 >= args.Length) return;
            started = true;
            MultiplayerSession.Instance.StartCoroutine(Run(ui, Path.GetFullPath(args[index + 1])));
        }
        private static IEnumerator Run(MenuFrontEnd ui, string directory)
        {
            Application.runInBackground = true; Directory.CreateDirectory(directory);
            var report = new Report();
            for (int i = 0; i < 10; i++) yield return null;
            Check(report, ui.Page == MenuPage.Home, "Home opens first");
            Check(report, UnityEngine.Object.FindObjectsByType<NetworkManager>().Length == 1, "One NetworkManager");
            Check(report, UnityEngine.Object.FindObjectsByType<EventSystem>().Length == 1, "One EventSystem");
            Check(report, UnityEngine.Object.FindObjectsByType<AudioListener>().Length == 1, "One AudioListener");
            yield return Capture(ui, directory, "01-home", report);
            ui.playButton.onClick.Invoke();
            Check(report, ui.Page == MenuPage.Levels && ui.DraftLevel == LevelCatalog.Training, "Play opens operation selector");
            yield return Capture(ui, directory, "02-training-selection", report);
            ui.levelButtons[LevelCatalog.Containment].onClick.Invoke();
            Check(report, ui.DraftLevel == LevelCatalog.Containment && ui.detailTitle.text == LevelCatalog.Get(1).Title, "Second operation can be selected");
            yield return Capture(ui, directory, "03-yard-selection", report);
            var session = MultiplayerSession.Instance;
            session.TrySelectLevel(ui.DraftLevel);
            var host = session.StartLocalTestAsync(true);
            while (!host.IsCompleted) yield return null;
            Check(report, !host.IsFaulted && session.IsHost && session.SelectedLevelId == 1, "Offline preference initializes Host room");
            ui.levelsBackButton.onClick.Invoke();
            yield return Capture(ui, directory, "04-lobby", report);
            ui.readyButton.onClick.Invoke();
            Check(report, session.Room.IsReady(NetworkManager.Singleton.LocalClientId), "Ready button updates Host member");
            ui.changeLevelButton.onClick.Invoke(); ui.levelButtons[0].onClick.Invoke(); ui.confirmLevelButton.onClick.Invoke();
            Check(report, session.SelectedLevelId == 0 && !session.Room.IsReady(NetworkManager.Singleton.LocalClientId), "Confirm operation resets Ready");
            Check(report, !ui.startButton.interactable, "Deploy disabled until squad requirements met");
            yield return Capture(ui, directory, "05-lobby-ready-reset", report);
            // Runtime-only solo fixture to render the actual second scene, no Cloud account.
            session.config.minimumPlayersToStart = 1;
            ui.changeLevelButton.onClick.Invoke(); ui.levelButtons[1].onClick.Invoke(); ui.confirmLevelButton.onClick.Invoke();
            ui.readyButton.onClick.Invoke(); ui.startButton.onClick.Invoke();
            float deadline = Time.realtimeSinceStartup + 20;
            while (session.State != SessionState.InGame && Time.realtimeSinceStartup < deadline) yield return null;
            Check(report, session.State == SessionState.InGame && SceneManager.GetActiveScene().name == LevelCatalog.Get(1).SceneName, "Host deploys into Containment Yard");
            for (int i = 0; i < 15; i++) yield return null;
            var enemy = UnityEngine.Object.FindAnyObjectByType<RobotEnemy>();
            Check(report, enemy != null && enemy.Agent.enabled && enemy.Agent.isOnNavMesh, "Yard Enemy uses baked navigation");
            SaveCameraImage(Camera.main, Path.Combine(directory, "06-yard-gameplay.png"), report);
            for (int i = 0; i < 8; i++) yield return null;
            var leave = session.LeaveRoomAsync(); while (!leave.IsCompleted) yield return null;
            while (session.State != SessionState.Offline) yield return null;
            for (int i = 0; i < 10; i++) yield return null;
            session.config.minimumPlayersToStart = 2;
            ui = UnityEngine.Object.FindAnyObjectByType<MenuFrontEnd>();
            Check(report, ui != null && UnityEngine.Object.FindObjectsByType<NetworkManager>().Length == 1, "Return from Yard to Menu without duplicate manager");
            ui.openJoinButton.onClick.Invoke(); ui.joinInput.text = "ab";
            Check(report, !ui.joinButton.interactable && ui.joinInput.text == "AB", "Short code blocked and normalized");
            ui.joinInput.text = "abc123";
            Check(report, ui.joinButton.interactable && ui.joinInput.text == "ABC123", "Six-character code enables Join");
            yield return Capture(ui, directory, "06-join", report);
            var invalid = session.JoinRoomAsync("BAD"); while (!invalid.IsCompleted) yield return null;
            Check(report, session.State == SessionState.Error && ui.Page == MenuPage.Error, "Validation error opens error page");
            yield return Capture(ui, directory, "07-error", report);
            ui.errorBackButton.onClick.Invoke(); while (session.State != SessionState.Offline) yield return null;
            ui.controlsButton.onClick.Invoke();
            yield return Capture(ui, directory, "08-controls", report);
            ui.controlsBackButton.onClick.Invoke();
            Screen.SetResolution(1024, 768, FullScreenMode.Windowed);
            for (int i = 0; i < 15; i++) yield return null;
            yield return Capture(ui, directory, "09-home-4x3", report);
            report.complete = true; File.WriteAllText(Path.Combine(directory, "Checks.json"), JsonUtility.ToJson(report, true));
            Application.Quit(report.failures.Count == 0 ? 0 : 2);
        }
        private static IEnumerator Capture(MenuFrontEnd ui, string directory, string name, Report report)
        {
            // A hidden Windows player may have a black swapchain. Render the real Canvas
            // into a texture instead of treating a black screenshot as visual evidence.
            // A fresh render copy also avoids stale TMP batches across Canvas mode changes.
            var source = ui;
            var copy = UnityEngine.Object.Instantiate(ui.gameObject);
            ui = copy.GetComponent<MenuFrontEnd>(); ui.enabled = false;
            source.gameObject.SetActive(false);
            var canvas = ui.GetComponent<Canvas>();
            var cameraObject = new GameObject("Menu capture camera", typeof(Camera));
            var camera = cameraObject.GetComponent<Camera>(); camera.enabled = true;
            camera.transform.position = new Vector3(0, 0, -100); camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.025f, 0.043f, 0.065f);
            camera.cullingMask = 1 << 30;
            foreach (var transform in ui.GetComponentsInChildren<Transform>(true)) transform.gameObject.layer = 30;
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
            var destination = new RenderTexture(Screen.width, Screen.height, 24); destination.Create(); camera.targetTexture = destination;
            for (int i = 0; i < 8; i++) yield return null;
            Canvas.ForceUpdateCanvases();
            foreach (var text in ui.GetComponentsInChildren<TextMeshProUGUI>())
            {
                if (!text.isActiveAndEnabled || string.IsNullOrWhiteSpace(text.text)) continue;
                text.ForceMeshUpdate();
                if (text.isTextOverflowing) report.failures.Add(name + " text overflow: " + text.name);
                var corners = new Vector3[4]; text.rectTransform.GetWorldCorners(corners);
                foreach (var corner in corners)
                {
                    Vector2 pixel = RectTransformUtility.WorldToScreenPoint(camera, corner);
                    if (pixel.x < -1 || pixel.y < -1 || pixel.x > Screen.width + 1 || pixel.y > Screen.height + 1)
                    { report.failures.Add(name + " outside screen: " + text.name); break; }
                }
            }
            string path = Path.Combine(directory, name + ".png");
            foreach (var graphic in ui.GetComponentsInChildren<UnityEngine.UI.Graphic>()) graphic.SetAllDirty();
            Canvas.ForceUpdateCanvases();
            yield return new WaitForEndOfFrame();
            SaveCameraImage(camera, path, report, destination, false);
            camera.targetTexture = null; destination.Release(); UnityEngine.Object.Destroy(destination); UnityEngine.Object.Destroy(cameraObject);
            UnityEngine.Object.Destroy(copy); source.gameObject.SetActive(true);
            Check(report, File.Exists(path), name + " captured");
        }
        private static void SaveCameraImage(Camera camera, string path, Report report, RenderTexture destination = null, bool submitRequest = true)
        {
            bool own = destination == null;
            if (own) { destination = new RenderTexture(Screen.width, Screen.height, 24); destination.Create(); }
            if (submitRequest) RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = destination });
            var previous = RenderTexture.active; RenderTexture.active = destination;
            var pixels = new Texture2D(destination.width, destination.height, TextureFormat.RGB24, false);
            pixels.ReadPixels(new Rect(0, 0, destination.width, destination.height), 0, 0); pixels.Apply();
            RenderTexture.active = previous;
            Color baseline = pixels.GetPixel(1, 1); bool content = false;
            for (int y = 20; y < destination.height; y += 25)
                for (int x = 20; x < destination.width; x += 25)
                {
                    Color color = pixels.GetPixel(x, y);
                    content |= Mathf.Abs(color.r - baseline.r) + Mathf.Abs(color.g - baseline.g) + Mathf.Abs(color.b - baseline.b) > 0.2f;
                }
            Check(report, content, Path.GetFileName(path) + " contains rendered content");
            File.WriteAllBytes(path, pixels.EncodeToPNG()); UnityEngine.Object.Destroy(pixels);
            if (own) { destination.Release(); UnityEngine.Object.Destroy(destination); }
        }
        private static void Check(Report report, bool success, string name)
        { if (success) report.passed.Add(name); else report.failures.Add(name); }
    }
}
#endif
