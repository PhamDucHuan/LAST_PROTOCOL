using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LastProtocol.Editor
{
    public static class MenuSetupTool
    {
        public const string MenuPrefabPath = MultiplayerSetupTool.Root + "/Prefabs/UI/OperationsMenu.prefab";
        public const string YardNavigationPath = MultiplayerSetupTool.Root + "/Settings/ContainmentNavigation.asset";
        public static string YardPath => LevelCatalog.Get(LevelCatalog.Containment).ScenePath;
        private static TMP_FontAsset font;
        private static readonly Color Ink = new Color(0.025f, 0.043f, 0.065f), Panel = new Color(0.045f, 0.075f, 0.105f),
            Raised = new Color(0.075f, 0.12f, 0.16f), Cyan = new Color(0.18f, 0.87f, 0.9f),
            Muted = new Color(0.57f, 0.67f, 0.73f), Orange = new Color(1, 0.65f, 0.28f);

        [MenuItem("LastProtocol/Menu/Install Menus and Two Test Levels")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode before installing menus.");
            ReviveSetupTool.Install();
            foreach (var path in new[] { MultiplayerSetupTool.Root + "/Prefabs/UI", Path.GetDirectoryName(YardPath) }) Directory.CreateDirectory(path);
            AssetDatabase.Refresh();
            CreateYard();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MenuPrefabPath);
            if (prefab == null)
            {
                if (File.Exists(MenuPrefabPath)) throw new InvalidOperationException("Incompatible existing Menu asset preserved.");
                prefab = CreateMenu();
            }
            if (prefab.GetComponent<MenuFrontEnd>() == null) throw new InvalidOperationException("Existing Menu prefab is incompatible; preserved.");
            var config = AssetDatabase.LoadAssetAtPath<MultiplayerConfig>(MultiplayerSetupTool.ConfigPath);
            if (config.menuPrefab != prefab.GetComponent<MenuFrontEnd>())
            {
                Backup(MultiplayerSetupTool.ConfigPath);
                config.menuPrefab = prefab.GetComponent<MenuFrontEnd>(); EditorUtility.SetDirty(config);
            }
            var scenes = EditorBuildSettings.scenes.ToList();
            if (!scenes.Any(x => x.path == YardPath && x.enabled))
            {
                Backup("ProjectSettings/EditorBuildSettings.asset");
                int index = scenes.FindIndex(x => x.path == YardPath);
                if (index < 0) scenes.Add(new EditorBuildSettingsScene(YardPath, true));
                else scenes[index] = new EditorBuildSettingsScene(YardPath, true);
                EditorBuildSettings.scenes = scenes.ToArray();
            }
            AssetDatabase.SaveAssets(); Validate();
            Debug.Log("LAST_PROTOCOL menus + two playable test levels installed. Existing scenes preserved.");
        }
        private static void Backup(string path)
        {
            string directory = "Builds/MenuBackups/" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");
            Directory.CreateDirectory(directory); File.Copy(path, directory + "/" + Path.GetFileName(path));
            if (File.Exists(path + ".meta")) File.Copy(path + ".meta", directory + "/" + Path.GetFileName(path) + ".meta");
        }
        private static Material Material(string name, Color color)
        {
            string path = MultiplayerSetupTool.Root + "/Materials/" + name + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            if (File.Exists(path)) throw new InvalidOperationException("Existing material preserved: " + path);
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            material.SetColor("_BaseColor", color); AssetDatabase.CreateAsset(material, path); return material;
        }
        private static void CreateYard()
        {
            if (File.Exists(YardPath)) return;
            if (File.Exists(YardNavigationPath)) throw new InvalidOperationException("Orphan navigation asset preserved; review before creating Yard.");
            if (!AssetDatabase.CopyAsset(MultiplayerSetupTool.GamePath, YardPath)) throw new IOException("Could not copy the test scene.");
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(YardPath, OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                var ground = scene.GetRootGameObjects().First(x => x.name == "Ground");
                ground.GetComponent<Renderer>().sharedMaterial = Material("YardFloor", new Color(0.18f, 0.2f, 0.23f));
                var coverMaterial = Material("YardConcrete", new Color(0.32f, 0.36f, 0.4f));
                var stripeMaterial = Material("YardSafety", Orange);
                var geometry = new GameObject("Containment Yard Geometry");
                Box(geometry.transform, "West Barrier", new Vector3(-6, 1.25f, 2), new Vector3(4, 2.5f, 1), coverMaterial);
                Box(geometry.transform, "East Barrier", new Vector3(6, 1.25f, 2), new Vector3(4, 2.5f, 1), coverMaterial);
                Box(geometry.transform, "West Cover", new Vector3(-6, 1.25f, 7), new Vector3(1, 2.5f, 4), coverMaterial);
                Box(geometry.transform, "East Cover", new Vector3(6, 1.25f, 7), new Vector3(1, 2.5f, 4), coverMaterial);
                Box(geometry.transform, "Central Low Cover", new Vector3(0, 0.65f, 5), new Vector3(3, 1.3f, 1), coverMaterial);
                Box(geometry.transform, "North Wall", new Vector3(0, 1.5f, 19.5f), new Vector3(40, 3, 1), coverMaterial);
                Box(geometry.transform, "South Wall", new Vector3(0, 1.5f, -19.5f), new Vector3(40, 3, 1), coverMaterial);
                Box(geometry.transform, "West Wall", new Vector3(-19.5f, 1.5f, 0), new Vector3(1, 3, 38), coverMaterial);
                Box(geometry.transform, "East Wall", new Vector3(19.5f, 1.5f, 0), new Vector3(1, 3, 38), coverMaterial);
                for (int i = -3; i <= 3; i++)
                    Box(geometry.transform, "Floor Marking", new Vector3(i * 4, 0.008f, 0), new Vector3(0.035f, 0.01f, 36), stripeMaterial, false);
                foreach (var light in scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Light>()))
                { light.color = new Color(1, 0.85f, 0.65f); light.intensity = 1.7f; }
                RenderSettings.ambientLight = new Color(0.31f, 0.35f, 0.42f);
                var sources = new List<NavMeshBuildSource>();
                foreach (var box in scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<BoxCollider>()))
                    sources.Add(new NavMeshBuildSource { shape = NavMeshBuildSourceShape.Box, size = box.size,
                        transform = box.transform.localToWorldMatrix * Matrix4x4.Translate(box.center), area = box.gameObject == ground ? 0 : 1 });
                for (int i = 0; i < MultiplayerConfig.MaxPlayers; i++)
                    sources.Add(new NavMeshBuildSource { shape = NavMeshBuildSourceShape.Box, size = new Vector3(1.1f, 2, 1.1f),
                        transform = Matrix4x4.TRS(new Vector3(-9 + i * 6, 1, 10), Quaternion.identity, Vector3.one), area = 1 });
                var settings = NavMesh.GetSettingsByIndex(0); settings.agentRadius = 0.4f; settings.agentHeight = 2;
                settings.agentClimb = 0.4f; settings.overrideVoxelSize = true; settings.voxelSize = 0.13f;
                var navigation = NavMeshBuilder.BuildNavMeshData(settings, sources, new Bounds(Vector3.zero, new Vector3(44, 8, 44)), Vector3.zero, Quaternion.identity);
                if (navigation == null) throw new InvalidOperationException("Yard NavMesh bake failed.");
                AssetDatabase.CreateAsset(navigation, YardNavigationPath);
                var environment = geometry.AddComponent<LevelEnvironment>(); environment.levelId = LevelCatalog.Containment; environment.enemyNavigation = navigation;
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save Yard scene.");
            }
            finally
            { EditorSceneManager.CloseScene(scene, true); if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous); }
        }
        private static void Box(Transform parent, string name, Vector3 position, Vector3 size, Material material, bool collider = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.transform.SetParent(parent, false);
            go.transform.position = position; go.transform.localScale = size; go.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        }

        private static RectTransform Rect(string name, Transform parent, float x, float y, float width, float height)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height); return rect;
        }
        private static RectTransform Surface(string name, Transform parent, float x, float y, float width, float height, Color color, bool raycast = false)
        {
            var rect = Rect(name, parent, x, y, width, height);
            var image = rect.gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = raycast; return rect;
        }
        private static TMP_Text Text(Transform parent, string value, float x, float y, float w, float h, float size, Color color, bool bold = false)
        {
            var rect = Rect("Text " + value.Split('\n')[0], parent, x, y, w, h);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>(); label.font = font; label.text = value;
            label.fontSize = size; label.color = color; label.raycastTarget = false;
            label.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal; label.alignment = TextAlignmentOptions.TopLeft; return label;
        }
        private static Button Action(Transform parent, string value, float x, float y, float w, float h, out TMP_Text label, bool primary = false)
        {
            var rect = Surface(value, parent, x, y, w, h, primary ? Cyan : Raised, true);
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = rect.GetComponent<Image>();
            var colors = button.colors; colors.highlightedColor = new Color(0.75f, 0.94f, 1); colors.pressedColor = new Color(0.5f, 0.74f, 0.85f);
            colors.disabledColor = new Color(0.3f, 0.38f, 0.44f); button.colors = colors;
            label = Text(rect, value, 12, 0, w - 24, h, 23, primary ? Ink : Color.white, true); label.alignment = TextAlignmentOptions.Center;
            return button;
        }
        private static GameObject Page(Transform frame, string name) => Rect(name, frame, 0, 125, 1680, 740).gameObject;
        private static void Heading(Transform page, string number, string title, string subtitle)
        {
            Text(page, number + " / FIELD OPERATIONS", 40, 0, 1200, 30, 17, Cyan, true);
            Text(page, title, 40, 38, 1400, 65, 45, Color.white, true);
            Text(page, subtitle, 40, 105, 1500, 38, 21, Muted);
        }
        private static void Diagram(Transform parent, float x, float y, float w, float h, int level)
        {
            var rect = Surface("Arena schematic", parent, x, y, w, h, Ink);
            Color accent = LevelCatalog.Get(level).Accent;
            for (int i = 1; i < 8; i++) Surface("Grid X", rect, i * w / 8, 0, 1, h, new Color(0.1f, 0.16f, 0.2f));
            for (int i = 1; i < 5; i++) Surface("Grid Y", rect, 0, i * h / 5, w, 1, new Color(0.1f, 0.16f, 0.2f));
            Surface("North boundary", rect, 14, 14, w - 28, 2, accent);
            Surface("West boundary", rect, 14, 14, 2, h - 28, accent);
            Surface("East boundary", rect, w - 16, 14, 2, h - 28, accent);
            Surface("South boundary", rect, 14, h - 16, w - 28, 2, accent);
            for (int i = 0; i < 4; i++)
            {
                Surface("Target", rect, w * (0.22f + i * 0.18f) - 5, h * 0.33f, 10, 15, accent);
                Surface("Player spawn", rect, w * (0.35f + (i % 2) * 0.25f) - 5, h * (0.67f + (i / 2) * 0.12f), 10, 10, Cyan);
            }
            Surface("Enemy", rect, w * 0.5f - 7, h * 0.18f, 14, 14, new Color(0.96f, 0.28f, 0.3f));
            if (level == LevelCatalog.Containment)
            {
                foreach (float side in new[] { 0.28f, 0.65f })
                {
                    Surface("Cover", rect, w * side, h * 0.48f, w * 0.12f, 12, Orange);
                    Surface("Cover", rect, w * side, h * 0.34f, 12, h * 0.1f, Orange);
                }
                Surface("Low cover", rect, w * 0.45f, h * 0.43f, w * 0.1f, 8, Orange);
            }
        }
        private static GameObject CreateMenu()
        {
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MultiplayerSetupTool.Root + "/Settings/TextMeshPro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if (font == null) throw new InvalidOperationException("Existing TMP font missing.");
            var previous = SceneManager.GetActiveScene(); var scratch = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scratch);
                var root = new GameObject("LAST_PROTOCOL Operations Menu", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay; root.GetComponent<Canvas>().sortingOrder = 20;
                var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 0.5f;
                var backdrop = Surface("Backdrop", root.transform, 0, 0, 1920, 1080, Ink, true);
                backdrop.anchorMin = Vector2.zero; backdrop.anchorMax = Vector2.one; backdrop.offsetMin = backdrop.offsetMax = Vector2.zero;
                var frame = Rect("Safe Frame", root.transform, 0, 0, 1680, 920);
                frame.anchorMin = frame.anchorMax = frame.pivot = new Vector2(0.5f, 0.5f); frame.anchoredPosition = Vector2.zero;
                var ui = root.AddComponent<MenuFrontEnd>();
                ui.safeFrame = frame;
                Surface("Brand mark", frame, 40, 22, 8, 46, Cyan);
                Text(frame, "LP / LAST_PROTOCOL", 66, 20, 700, 55, 30, Color.white, true);
                ui.breadcrumb = Text(frame, "LAST PROTOCOL / HOME", 1000, 26, 640, 40, 18, Muted); ui.breadcrumb.alignment = TextAlignmentOptions.MidlineRight;
                Surface("Header rule", frame, 40, 98, 1600, 1, Raised);
                Surface("Footer rule", frame, 40, 884, 1600, 1, Raised);
                Text(frame, "HOST-AUTHORITATIVE CO-OP   /   UP TO 4 AGENTS", 40, 900, 1000, 28, 16, Muted);
                var version = Text(frame, "PROTOTYPE  /  BUILD " + MultiplayerConfig.ProtocolVersion.ToString("00"), 1200, 900, 440, 28, 16, Cyan); version.alignment = TextAlignmentOptions.TopRight;
                BuildHome(ui, frame); BuildLevels(ui, frame); BuildJoin(ui, frame); BuildLobby(ui, frame); BuildControls(ui, frame); BuildError(ui, frame);
                var busy = Surface("Connection overlay", root.transform, 0, 0, 1920, 1080, new Color(0.01f, 0.025f, 0.04f, 0.97f), true);
                busy.anchorMin = Vector2.zero; busy.anchorMax = Vector2.one; busy.offsetMin = busy.offsetMax = Vector2.zero;
                var busyCard = Rect("Loading card", busy, 0, 0, 900, 180); busyCard.anchorMin = busyCard.anchorMax = busyCard.pivot = new Vector2(0.5f, 0.5f); busyCard.anchoredPosition = Vector2.zero;
                Text(busyCard, "LAST_PROTOCOL / NETWORK", 0, 0, 900, 35, 19, Cyan).alignment = TextAlignmentOptions.Center;
                ui.busyLabel = Text(busyCard, "CONNECTING...", 0, 60, 900, 120, 38, Color.white, true); ui.busyLabel.alignment = TextAlignmentOptions.Center;
                ui.busyOverlay = busy.gameObject;
                foreach (var page in new[] { ui.levelsPage, ui.joinPage, ui.lobbyPage, ui.controlsPage, ui.errorPage, ui.busyOverlay }) page.SetActive(false);
                return PrefabUtility.SaveAsPrefabAsset(root, MenuPrefabPath);
            }
            finally { EditorSceneManager.CloseScene(scratch, true); if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous); }
        }
        private static void BuildHome(MenuFrontEnd ui, Transform frame)
        {
            var page = Page(frame, "Home"); ui.homePage = page; var p = page.transform;
            Text(p, "SQUAD SYSTEMS ONLINE", 40, 30, 730, 34, 19, Cyan, true);
            Text(p, "LAST_\nPROTOCOL", 34, 85, 800, 230, 100, Color.white, true);
            Text(p, "Connect your squad. Choose an operation.\nSurvive together.", 40, 330, 740, 80, 25, Muted);
            ui.playButton = Action(p, "PLAY CO-OP   >", 40, 440, 600, 64, out _, true);
            ui.openJoinButton = Action(p, "JOIN A ROOM", 40, 520, 600, 64, out _);
            ui.controlsButton = Action(p, "HOW TO PLAY", 40, 600, 288, 60, out _);
            ui.quitButton = Action(p, "QUIT GAME", 344, 600, 296, 60, out _);
            var card = Surface("Operations preview", p, 870, 35, 770, 640, Panel);
            Text(card, "FIELD OPERATIONS", 34, 28, 700, 42, 25, Color.white, true);
            Text(card, "02 PLAYABLE TRAINING SITES", 34, 78, 700, 30, 17, Cyan);
            Diagram(card, 34, 135, 702, 290, LevelCatalog.Containment);
            Text(card, "ONE SQUAD. ONE WORLD.", 34, 462, 700, 48, 29, Color.white, true);
            Text(card, "Create a Relay room or join with a code.\nMovement / combat / enemy / teammate revival.", 34, 525, 700, 90, 22, Muted);
        }
        private static void BuildLevels(MenuFrontEnd ui, Transform frame)
        {
            ui.levelsPage = Page(frame, "Select Operation"); var p = ui.levelsPage.transform;
            Heading(p, "01", "SELECT OPERATION", "Choose a test site. The Host's selection applies to the whole squad.");
            ui.levelButtons = new Button[LevelCatalog.Count]; ui.selectionStrips = new Image[LevelCatalog.Count]; ui.selectionBadges = new TMP_Text[LevelCatalog.Count];
            for (int i = 0; i < LevelCatalog.Count; i++)
            {
                var level = LevelCatalog.Get(i); var card = Surface(level.Title, p, 40 + i * 535, 170, 510, 450, Panel, true);
                var button = card.gameObject.AddComponent<Button>(); button.targetGraphic = card.GetComponent<Image>(); ui.levelButtons[i] = button;
                ui.selectionStrips[i] = Surface("Selection accent", card, 0, 0, 510, 4, Raised).GetComponent<Image>();
                Text(card, "0" + (i + 1) + " / SIMULATION", 26, 28, 280, 30, 17, Muted);
                ui.selectionBadges[i] = Text(card, "AVAILABLE", 290, 28, 194, 30, 17, Cyan, true); ui.selectionBadges[i].alignment = TextAlignmentOptions.TopRight;
                Diagram(card, 26, 85, 458, 220, i);
                Text(card, level.Title, 26, 335, 458, 42, 27, Color.white, true);
                Text(card, level.Summary, 26, 389, 458, 50, 20, Muted);
            }
            var detail = Surface("Operation briefing", p, 1120, 170, 520, 450, Panel);
            Text(detail, "OPERATION BRIEF", 28, 26, 460, 30, 17, Cyan, true);
            ui.detailTitle = Text(detail, "", 28, 80, 464, 85, 29, Color.white, true);
            ui.detailDescription = Text(detail, "", 28, 172, 464, 245, 22, Muted);
            ui.levelsBackButton = Action(p, "<  BACK", 40, 668, 250, 60, out _);
            ui.confirmLevelButton = Action(p, "CREATE ROOM  >", 1120, 668, 520, 60, out ui.confirmLevelLabel, true);
        }
        private static void BuildJoin(MenuFrontEnd ui, Transform frame)
        {
            ui.joinPage = Page(frame, "Join Room"); var p = ui.joinPage.transform;
            Heading(p, "02", "JOIN YOUR SQUAD", "Get a room code from your Host. No port forwarding required.");
            var card = Surface("Join form", p, 40, 180, 900, 430, Panel);
            Text(card, "ROOM CODE", 40, 35, 800, 30, 18, Cyan, true);
            var input = Surface("Join Code Input", card, 40, 96, 820, 100, Ink, true);
            ui.joinInput = input.gameObject.AddComponent<TMP_InputField>();
            var viewport = Rect("Viewport", input, 22, 10, 776, 80); viewport.gameObject.AddComponent<RectMask2D>();
            var text = (TextMeshProUGUI)Text(viewport, "", 0, 0, 776, 80, 48, Color.white, true); text.alignment = TextAlignmentOptions.Center;
            var placeholder = Text(viewport, "ABC123", 0, 0, 776, 80, 48, Muted, true); placeholder.alignment = TextAlignmentOptions.Center;
            ui.joinInput.textViewport = viewport; ui.joinInput.textComponent = text; ui.joinInput.placeholder = placeholder;
            ui.joinInput.fontAsset = font; ui.joinInput.contentType = TMP_InputField.ContentType.Alphanumeric; ui.joinInput.characterLimit = 6;
            ui.joinHint = Text(card, "Enter the Host's 6-character room code.", 40, 225, 820, 45, 21, Muted);
            ui.joinButton = Action(card, "CONNECT TO ROOM  >", 40, 310, 820, 66, out _, true);
            Text(p, "SQUAD CHECKLIST", 1030, 200, 560, 45, 26, Color.white, true);
            Text(p, "01   Use the same game build.\n\n02   Keep your Internet connection active.\n\n03   Wait for confirmed connection.\n\n04   Ready up in the lobby.", 1030, 275, 560, 285, 23, Muted);
            ui.joinBackButton = Action(p, "<  BACK", 40, 668, 250, 60, out _);
        }
        private static void BuildLobby(MenuFrontEnd ui, Transform frame)
        {
            ui.lobbyPage = Page(frame, "Squad Lobby"); var p = ui.lobbyPage.transform;
            Heading(p, "03", "SQUAD LOBBY", "Share the code, assemble your team and prepare to deploy.");
            var operation = Surface("Selected operation", p, 40, 170, 660, 450, Panel);
            Text(operation, "SELECTED OPERATION", 30, 25, 600, 30, 17, Cyan, true);
            ui.lobbyLevelTitle = Text(operation, "", 30, 82, 600, 80, 35, Color.white, true);
            ui.lobbyLevelSummary = Text(operation, "", 30, 170, 600, 80, 23, Muted);
            Text(operation, "TEAM PLAY / UP TO 4 AGENTS\nRobot / weapon targets / teammate revival", 30, 270, 600, 80, 21, Muted);
            ui.changeLevelButton = Action(operation, "CHANGE OPERATION", 30, 365, 600, 58, out _);
            var roster = Surface("Squad roster", p, 735, 170, 905, 450, Panel);
            Text(roster, "SQUAD", 25, 22, 300, 36, 23, Color.white, true);
            ui.playerCount = Text(roster, "", 580, 25, 300, 30, 19, Cyan, true); ui.playerCount.alignment = TextAlignmentOptions.TopRight;
            ui.rosterNames = new TMP_Text[4]; ui.rosterStates = new TMP_Text[4];
            for (int i = 0; i < 4; i++)
            {
                var row = Surface("Agent " + (i + 1), roster, 25, 83 + i * 82, 855, 67, Raised);
                Text(row, "0" + (i + 1), 18, 18, 50, 36, 23, Cyan, true);
                ui.rosterNames[i] = Text(row, "", 90, 18, 450, 36, 23, Color.white, true);
                ui.rosterStates[i] = Text(row, "", 540, 20, 295, 36, 19, Muted); ui.rosterStates[i].alignment = TextAlignmentOptions.TopRight;
            }
            Text(p, "ROOM CODE", 40, 642, 170, 30, 16, Muted);
            ui.roomCode = Text(p, "------", 40, 675, 220, 50, 34, Color.white, true);
            ui.copyButton = Action(p, "COPY CODE", 260, 670, 190, 58, out ui.copyLabel);
            ui.leaveButton = Action(p, "LEAVE", 470, 670, 170, 58, out _);
            ui.readyButton = Action(p, "READY UP", 735, 670, 380, 58, out ui.readyLabel);
            ui.startButton = Action(p, "DEPLOY SQUAD  >", 1135, 670, 505, 58, out _, true);
            ui.lobbyHint = Text(p, "", 735, 628, 905, 37, 17, Muted);
        }
        private static void BuildControls(MenuFrontEnd ui, Transform frame)
        {
            ui.controlsPage = Page(frame, "How To Play"); var p = ui.controlsPage.transform;
            Heading(p, "04", "SURVIVE TOGETHER", "Basic controls for the multiplayer prototype.");
            var controls = Surface("Controls", p, 40, 175, 760, 440, Panel);
            Text(controls, "YOUR CONTROLS", 30, 30, 700, 45, 26, Color.white, true);
            Text(controls, "WASD              MOVE\nSHIFT              SPRINT\nSPACE             JUMP\nMOUSE             LOOK\nLEFT CLICK     FIRE\nHOLD E           REVIVE TEAMMATE\nESC                  UNLOCK / LOCK CURSOR", 30, 110, 700, 300, 25, Muted);
            var squad = Surface("Co-op rules", p, 840, 175, 800, 440, Panel);
            Text(squad, "PROTECT THE REVIVE", 30, 30, 740, 45, 26, Cyan, true);
            Text(squad, "At 0 HP, wait for a living teammate.\n\nStay close and hold E until revival completes. Moving or taking damage interrupts the action.\n\nIf the entire squad is down, the Host can restart the test. Friendly fire is off by default.", 30, 110, 740, 300, 24, Muted);
            ui.controlsBackButton = Action(p, "<  BACK", 40, 668, 250, 60, out _);
        }
        private static void BuildError(MenuFrontEnd ui, Transform frame)
        {
            ui.errorPage = Page(frame, "Connection Error"); var p = ui.errorPage.transform;
            Heading(p, "!", "CONNECTION INTERRUPTED", "Your squad could not connect. Review the message and try again.");
            var card = Surface("Error message", p, 40, 190, 1600, 360, Panel);
            Surface("Warning stripe", card, 0, 0, 5, 360, Orange);
            Text(card, "SESSION MESSAGE", 40, 35, 1520, 40, 18, Orange, true);
            ui.errorMessage = Text(card, "", 40, 110, 1520, 190, 30, Color.white);
            ui.errorBackButton = Action(p, "<  BACK / TRY AGAIN", 40, 668, 440, 60, out _, true);
        }
        public static void Validate()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MenuPrefabPath)?.GetComponent<MenuFrontEnd>();
            var config = AssetDatabase.LoadAssetAtPath<MultiplayerConfig>(MultiplayerSetupTool.ConfigPath);
            if (prefab == null || config.menuPrefab != prefab || prefab.levelButtons.Length != LevelCatalog.Count
                || prefab.rosterNames.Length != MultiplayerConfig.MaxPlayers || prefab.joinInput == null)
                throw new InvalidOperationException("Menu prefab configuration invalid.");
            foreach (var component in prefab.GetComponentsInChildren<Transform>(true))
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(component.gameObject) > 0) throw new InvalidOperationException("Menu missing script.");
            if (!File.Exists(YardPath) || AssetDatabase.LoadAssetAtPath<NavMeshData>(YardNavigationPath) == null)
                throw new InvalidOperationException("Second level/NavMesh missing.");
            Debug.Log("LAST_PROTOCOL Menu and Level assets validation PASS");
        }
        public static void BatchInstall() { if (!Application.isBatchMode) throw new InvalidOperationException("Use Menu Setup interactively."); Install(); }
    }
}
