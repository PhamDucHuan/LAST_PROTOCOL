using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using Unity.Cinemachine;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LastProtocol.Editor
{
    public static class MultiplayerSetupTool
    {
        public const string Root = "Assets/_Project";
        public const string MenuPath = Root + "/Scenes/MainMenu/MainMenu.unity";
        public const string GamePath = Root + "/Scenes/MultiplayerTest/MultiplayerTest.unity";
        public const string PlayerPath = Root + "/Prefabs/Player/NetworkPlayer.prefab";
        public const string RoomPath = Root + "/Prefabs/Networking/NetworkRoomState.prefab";
        public const string ConfigPath = Root + "/Settings/MultiplayerConfig.asset";
        public const string PrefabListPath = Root + "/Settings/MvpNetworkPrefabs.asset";
        private const string FontPath = Root + "/Settings/TextMeshPro/Resources/Fonts & Materials/LiberationSans SDF.asset";
        private static TMP_FontAsset font;
        private static readonly Color Background = new Color(0.025f, 0.045f, 0.075f, 0.97f);
        private static readonly Color Accent = new Color(0.08f, 0.5f, 0.65f, 1);

        [MenuItem("LastProtocol/Multiplayer/Generate MVP Assets (preserve existing)")]
        public static void Generate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode before setup.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (string.IsNullOrEmpty(SceneManager.GetSceneAt(i).path))
                    throw new InvalidOperationException("Save any untitled scene before setup. No scene will be saved or replaced automatically.");
            string[] directories = { "Art", "Audio", "Materials", "Animations", "Prefabs/Player", "Prefabs/Networking",
                "Scenes/MainMenu", "Scenes/MultiplayerTest", "ScriptableObjects", "Settings", "Tests/EditMode", "Documentation" };
            foreach (string directory in directories) Directory.CreateDirectory(Root + "/" + directory);
            AssetDatabase.Refresh();
            font = GetFont();
            var config = AssetDatabase.LoadAssetAtPath<MultiplayerConfig>(ConfigPath);
            if (config == null)
            {
                AssertAbsent(ConfigPath);
                config = ScriptableObject.CreateInstance<MultiplayerConfig>();
                AssetDatabase.CreateAsset(config, ConfigPath);
            }
            Scene previous = SceneManager.GetActiveScene();
            Scene scratch = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            NetworkPlayerController player;
            NetworkRoomState room;
            try
            {
                SceneManager.SetActiveScene(scratch);
                player = GetPlayerPrefab();
                room = GetRoomPrefab();
            }
            finally
            {
                EditorSceneManager.CloseScene(scratch, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
            try
            {
                NetworkPrefabsList list = GetPrefabList(player, room);
                CreateSceneIfAbsent(MenuPath, () => CreateMenuScene(config, player, room, list));
                CreateSceneIfAbsent(GamePath, CreateGameplayScene);
            }
            finally { if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous); }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Validate();
            Debug.Log("LAST_PROTOCOL MVP assets generated/validated. Existing assets and build settings preserved.");
        }

        private static void AssertAbsent(string path)
        { if (File.Exists(path)) throw new InvalidOperationException("Existing asset has incompatible type; preserved: " + path); }
        private static void CreateSceneIfAbsent(string path, Action populate)
        {
            if (File.Exists(path)) { Debug.Log("Preserved existing scene: " + path); return; }
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                populate();
                if (!EditorSceneManager.SaveScene(scene, path)) throw new IOException("Could not save " + path);
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        private static TMP_FontAsset GetFont()
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (existing == null || existing.atlasTexture == null || existing.material == null)
                throw new InvalidOperationException("Official TMP LiberationSans SDF resources are missing/invalid.");
            return existing;
        }

        private static Material GetMaterial(string name, Color color)
        {
            string path = Root + "/Materials/" + name + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            AssertAbsent(path);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("URP/Lit shader is unavailable.");
            var material = new Material(shader) { name = name };
            material.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static NetworkPlayerController GetPlayerPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPath);
            if (existing != null) return existing.GetComponent<NetworkPlayerController>()
                ?? throw new InvalidOperationException("Existing Player prefab is incompatible; not overwritten.");
            AssertAbsent(PlayerPath);
            GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            try
            {
                capsule.name = "NetworkPlayer";
                capsule.tag = "Player";
                UnityEngine.Object.DestroyImmediate(capsule.GetComponent<CapsuleCollider>());
                var controller = capsule.AddComponent<CharacterController>();
                controller.height = 2; controller.radius = 0.45f; controller.center = Vector3.zero;
                capsule.GetComponent<Renderer>().sharedMaterial = GetMaterial("Player", Color.cyan);
                capsule.AddComponent<NetworkObject>();
                var networkTransform = capsule.AddComponent<NetworkTransform>();
                networkTransform.Interpolate = true;
                NetworkMotionSettings.Configure(networkTransform);
                networkTransform.SyncScaleX = networkTransform.SyncScaleY = networkTransform.SyncScaleZ = false;
                // Default NGO NetworkTransform authority is Server; no owner-authority override.
                var input = capsule.AddComponent<PlayerInput>();
                input.actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
                if (input.actions == null) throw new InvalidOperationException("Existing InputSystem_Actions asset missing.");
                input.defaultActionMap = "Player";
                input.defaultControlScheme = "Keyboard&Mouse";
                input.enabled = false;
                capsule.AddComponent<PlayerMovement>();
                var player = capsule.AddComponent<NetworkPlayerController>();
                var target = new GameObject("Camera Target").transform;
                target.SetParent(capsule.transform, false);
                target.localPosition = new Vector3(0, 0.6f, 0);
                player.cameraTarget = target;
                capsule.AddComponent<PlayerHealth>();
                capsule.AddComponent<PlayerHealthView>().font = GetFont();
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(capsule, PlayerPath);
                return prefab.GetComponent<NetworkPlayerController>();
            }
            finally { UnityEngine.Object.DestroyImmediate(capsule); }
        }

        private static NetworkRoomState GetRoomPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(RoomPath);
            if (existing != null) return existing.GetComponent<NetworkRoomState>()
                ?? throw new InvalidOperationException("Existing Room prefab is incompatible; not overwritten.");
            AssertAbsent(RoomPath);
            var root = new GameObject("NetworkRoomState");
            try
            {
                root.AddComponent<NetworkObject>();
                root.AddComponent<NetworkRoomState>();
                return PrefabUtility.SaveAsPrefabAsset(root, RoomPath).GetComponent<NetworkRoomState>();
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static NetworkPrefabsList GetPrefabList(NetworkPlayerController player, NetworkRoomState room)
        {
            var existing = AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>(PrefabListPath);
            if (existing != null) return existing;
            AssertAbsent(PrefabListPath);
            var list = ScriptableObject.CreateInstance<NetworkPrefabsList>();
            list.Add(new NetworkPrefab { Prefab = player.gameObject });
            list.Add(new NetworkPrefab { Prefab = room.gameObject });
            AssetDatabase.CreateAsset(list, PrefabListPath);
            return list;
        }

        private static void CreateMenuScene(MultiplayerConfig config, NetworkPlayerController player, NetworkRoomState room, NetworkPrefabsList list)
        {
            var root = new GameObject("Multiplayer Runtime");
            var transport = root.AddComponent<UnityTransport>();
            var manager = root.AddComponent<NetworkManager>();
            manager.NetworkConfig.NetworkTransport = transport;
            manager.NetworkConfig.ConnectionApproval = true;
            manager.NetworkConfig.EnableSceneManagement = true;
            manager.NetworkConfig.TickRate = MultiplayerConfig.NetworkTickRate;
            manager.NetworkConfig.ClientConnectionBufferTimeout = Mathf.CeilToInt(config.connectionTimeoutSeconds);
            manager.NetworkConfig.PlayerPrefab = player.gameObject;
            manager.NetworkConfig.Prefabs.NetworkPrefabsLists.Clear();
            manager.NetworkConfig.Prefabs.NetworkPrefabsLists.Add(list);
            var session = root.AddComponent<MultiplayerSession>();
            session.config = config; session.playerPrefab = player; session.roomPrefab = room;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            root.AddComponent<MultiplayerSmokeHarness>();
#endif
            CreateCamera(false);
            CreateUI(false);
        }

        private static void CreateGameplayScene()
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            ground.transform.position = new Vector3(0, -0.5f, 0);
            ground.transform.localScale = new Vector3(40, 1, 40);
            ground.GetComponent<Renderer>().sharedMaterial = GetMaterial("Ground", new Color(0.12f, 0.18f, 0.23f));
            var points = new GameObject("Player Spawn Points").AddComponent<PlayerSpawnPoints>();
            points.points = new Transform[MultiplayerConfig.MaxPlayers];
            for (int i = 0; i < MultiplayerConfig.MaxPlayers; i++)
            {
                var point = new GameObject("Spawn " + (i + 1)).transform;
                point.SetParent(points.transform, false);
                point.position = new Vector3((i % 2) * 4 - 2, 1.1f, (i / 2) * 4 - 2);
                points.points[i] = point;
            }
            var light = new GameObject("Directional Light").AddComponent<Light>();
            light.type = LightType.Directional; light.intensity = 1.5f;
            light.transform.rotation = Quaternion.Euler(50, -30, 0);
            RenderSettings.ambientLight = new Color(0.35f, 0.4f, 0.5f);
            CreateCamera(true);
            CreateUI(true);
        }

        private static void CreateCamera(bool gameplay)
        {
            var camera = new GameObject("Main Camera", typeof(UnityEngine.Camera), typeof(AudioListener));
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(0, 6, -10);
            camera.transform.rotation = Quaternion.Euler(20, 0, 0);
            camera.GetComponent<UnityEngine.Camera>().backgroundColor = new Color(0.02f, 0.04f, 0.08f);
            if (!gameplay) return;
            camera.AddComponent<CinemachineBrain>();
            var rig = new GameObject("Local Third Person Camera");
            var virtualCamera = rig.AddComponent<CinemachineCamera>();
            var follow = rig.AddComponent<CinemachineThirdPersonFollow>();
            follow.CameraDistance = 5;
            follow.Damping = new Vector3(0.08f, 0.08f, 0.08f);
            follow.ShoulderOffset = new Vector3(0.4f, 0.25f, 0);
            follow.AvoidObstacles.Enabled = true;
            follow.AvoidObstacles.CollisionFilter = 1; // Default environment layer.
            follow.AvoidObstacles.IgnoreTag = "Player";
            var driver = rig.AddComponent<LocalPlayerCamera>();
            driver.virtualCamera = virtualCamera; driver.follow = follow;
        }

        private static RectTransform UIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }
        private static TMP_Text Label(Transform parent, string text, float height = 40, float size = 24)
        {
            var rect = UIObject(text, parent);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font; label.text = text; label.fontSize = size; label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
            var layout = rect.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = height; layout.minHeight = height;
            return label;
        }
        private static Button Button(Transform parent, string text, out TMP_Text label)
        {
            var rect = UIObject(text + " Button", parent);
            var image = rect.gameObject.AddComponent<Image>(); image.color = Accent;
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            var colors = button.colors; colors.highlightedColor = new Color(0.7f, 1, 1); colors.disabledColor = new Color(0.25f, 0.3f, 0.35f); button.colors = colors;
            rect.gameObject.AddComponent<LayoutElement>().preferredHeight = 48;
            label = Label(rect, text, 48, 22);
            var child = label.rectTransform; child.anchorMin = Vector2.zero; child.anchorMax = Vector2.one; child.offsetMin = child.offsetMax = Vector2.zero;
            return button;
        }
        private static GameObject Panel(Transform parent, string name)
        {
            var rect = UIObject(name, parent);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(820, 650);
            rect.gameObject.AddComponent<Image>().color = Background;
            var layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(50, 50, 25, 25); layout.spacing = 10;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            return rect.gameObject;
        }

        private static void CreateUI(bool gameplay)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            var root = new GameObject("Multiplayer UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 800); scaler.matchWidthOrHeight = 0.5f;
            var ui = root.AddComponent<MultiplayerRoomUI>();
            if (gameplay)
            {
                var hud = UIObject("Gameplay HUD", root.transform);
                hud.anchorMin = new Vector2(0, 1); hud.anchorMax = Vector2.one;
                hud.pivot = new Vector2(0.5f, 1); hud.sizeDelta = new Vector2(0, 105);
                hud.gameObject.AddComponent<Image>().color = Background;
                var layout = hud.gameObject.AddComponent<VerticalLayoutGroup>(); layout.spacing = 4;
                layout.childControlHeight = layout.childControlWidth = true; layout.childForceExpandHeight = false;
                ui.gameplayPanel = hud.gameObject;
                ui.gameStatusText = Label(hud, "CO-OP TEST", 38, 20);
                ui.gameLeaveButton = Button(hud, "LEAVE SESSION (Esc to unlock cursor)", out _);
                ui.errorPanel = Panel(root.transform, "Error");
                Label(ui.errorPanel.transform, "CONNECTION ENDED", 70, 32);
                ui.errorText = Label(ui.errorPanel.transform, "", 180, 24);
                ui.errorBackButton = Button(ui.errorPanel.transform, "BACK TO MENU", out _);
                ui.errorPanel.SetActive(false);
                return;
            }
            ui.menuPanel = Panel(root.transform, "Main Menu");
            Label(ui.menuPanel.transform, "LAST_PROTOCOL", 110, 48);
            Label(ui.menuPanel.transform, "4-PLAYER CO-OP / RELAY MVP", 55, 22);
            ui.createButton = Button(ui.menuPanel.transform, "CREATE ROOM", out _);
            ui.openJoinButton = Button(ui.menuPanel.transform, "JOIN ROOM", out _);
            ui.quitButton = Button(ui.menuPanel.transform, "QUIT GAME", out _);

            ui.joinPanel = Panel(root.transform, "Join Room");
            Label(ui.joinPanel.transform, "JOIN ROOM", 85, 36);
            Label(ui.joinPanel.transform, "ENTER THE HOST'S JOIN CODE", 50, 22);
            var inputRect = UIObject("Join Code", ui.joinPanel.transform);
            inputRect.gameObject.AddComponent<Image>().color = new Color(0.1f, 0.15f, 0.2f);
            inputRect.gameObject.AddComponent<LayoutElement>().preferredHeight = 60;
            ui.joinCodeInput = inputRect.gameObject.AddComponent<TMP_InputField>();
            var viewport = UIObject("Text Viewport", inputRect);
            viewport.anchorMin = Vector2.zero; viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(15, 5); viewport.offsetMax = new Vector2(-15, -5);
            viewport.gameObject.AddComponent<RectMask2D>();
            var inputText = Label(viewport, "", 50, 30);
            inputText.rectTransform.anchorMin = Vector2.zero; inputText.rectTransform.anchorMax = Vector2.one;
            inputText.rectTransform.offsetMin = inputText.rectTransform.offsetMax = Vector2.zero;
            var placeholder = Label(viewport, "ABC123", 50, 30);
            placeholder.color = Color.gray;
            placeholder.rectTransform.anchorMin = Vector2.zero; placeholder.rectTransform.anchorMax = Vector2.one;
            placeholder.rectTransform.offsetMin = placeholder.rectTransform.offsetMax = Vector2.zero;
            ui.joinCodeInput.textViewport = viewport;
            ui.joinCodeInput.textComponent = (TextMeshProUGUI)inputText;
            ui.joinCodeInput.placeholder = placeholder;
            ui.joinCodeInput.fontAsset = font;
            ui.joinCodeInput.contentType = TMP_InputField.ContentType.Alphanumeric;
            ui.joinCodeInput.characterLimit = 6;
            ui.joinButton = Button(ui.joinPanel.transform, "JOIN", out _);
            ui.backButton = Button(ui.joinPanel.transform, "BACK", out _);

            ui.roomPanel = Panel(root.transform, "Room Lobby");
            Label(ui.roomPanel.transform, "ROOM / LOBBY", 65, 34);
            ui.roomCodeText = Label(ui.roomPanel.transform, "ROOM CODE", 45, 28);
            ui.copyButton = Button(ui.roomPanel.transform, "COPY CODE", out _);
            ui.playerCountText = Label(ui.roomPanel.transform, "PLAYERS", 35, 24);
            ui.playerListText = Label(ui.roomPanel.transform, "", 135, 24);
            ui.readyButton = Button(ui.roomPanel.transform, "READY", out ui.readyText);
            ui.startButton = Button(ui.roomPanel.transform, "START GAME (HOST)", out _);
            ui.leaveButton = Button(ui.roomPanel.transform, "LEAVE ROOM", out _);

            ui.errorPanel = Panel(root.transform, "Error");
            Label(ui.errorPanel.transform, "CONNECTION ERROR", 85, 34);
            ui.errorText = Label(ui.errorPanel.transform, "", 190, 24);
            ui.errorBackButton = Button(ui.errorPanel.transform, "BACK / TRY AGAIN", out _);

            ui.statusText = Label(root.transform, "", 40, 20);
            var statusRect = ui.statusText.rectTransform;
            statusRect.anchorMin = statusRect.anchorMax = new Vector2(0.5f, 0);
            statusRect.pivot = new Vector2(0.5f, 0); statusRect.anchoredPosition = new Vector2(0, 12); statusRect.sizeDelta = new Vector2(1150, 45);
            ui.joinPanel.SetActive(false); ui.roomPanel.SetActive(false); ui.errorPanel.SetActive(false);
        }

        [MenuItem("LastProtocol/Multiplayer/Validate MVP Assets")]
        public static void Validate()
        {
            var config = AssetDatabase.LoadAssetAtPath<MultiplayerConfig>(ConfigPath);
            var player = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPath);
            var room = AssetDatabase.LoadAssetAtPath<GameObject>(RoomPath);
            if (config == null || !config.IsValid || player == null || room == null) throw new InvalidOperationException("MVP assets missing or invalid.");
            if (player.GetComponent<NetworkObject>() == null || player.GetComponent<NetworkTransform>() == null
                || player.GetComponent<PlayerInput>()?.actions == null || player.GetComponent<NetworkPlayerController>()?.cameraTarget == null)
                throw new InvalidOperationException("Player prefab components/references missing.");
            if (room.GetComponent<NetworkObject>() == null || room.GetComponent<NetworkRoomState>() == null)
                throw new InvalidOperationException("Room prefab components missing.");
            foreach (string path in new[] { MenuPath, GamePath })
            {
                if (!File.Exists(path)) throw new InvalidOperationException("Scene missing: " + path);
                Scene loaded = SceneManager.GetSceneByPath(path);
                bool alreadyOpen = loaded.IsValid() && loaded.isLoaded;
                if (!alreadyOpen) loaded = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try
                {
                    var objects = loaded.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Transform>(true)).Select(x => x.gameObject).ToArray();
                    if (objects.Sum(x => x.GetComponents<AudioListener>().Length) != 1) throw new InvalidOperationException("Each scene must have one AudioListener: " + path);
                    if (objects.Any(x => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(x) > 0)) throw new InvalidOperationException("Missing scripts: " + path);
                    if (path == MenuPath)
                    {
                        var managers = objects.Select(x => x.GetComponent<NetworkManager>()).Where(x => x != null).ToArray();
                        if (managers.Length != 1) throw new InvalidOperationException("Menu must have one NetworkManager.");
                        foreach (GameObject prefab in new[] { player, room })
                            if (!managers[0].NetworkConfig.Prefabs.NetworkPrefabsLists.Any(x => x != null && x.Contains(prefab)))
                                throw new InvalidOperationException("Missing serialized network prefab registration: " + prefab.name + ". Use Repair MVP Prefab Registration.");
                    }
                    if (path == GamePath && objects.Select(x => x.GetComponent<PlayerSpawnPoints>()).FirstOrDefault(x => x != null)?.Count != MultiplayerConfig.MaxPlayers)
                        throw new InvalidOperationException("Gameplay needs four spawn points.");
                }
                finally { if (!alreadyOpen) EditorSceneManager.CloseScene(loaded, true); }
            }
            Debug.Log("LAST_PROTOCOL asset validation PASS");
        }

        [MenuItem("LastProtocol/Multiplayer/Add MVP Scenes to Build List")]
        public static void AddBuildScenes()
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            foreach (string path in new[] { MenuPath, GamePath })
            {
                int index = scenes.FindIndex(x => x.path == path);
                if (index < 0) scenes.Add(new EditorBuildSettingsScene(path, true));
                else if (!scenes[index].enabled) scenes[index] = new EditorBuildSettingsScene(path, true);
            }
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("MVP scenes added/enabled. Existing scene order preserved. Start Play Mode from MainMenu.");
        }

        [MenuItem("LastProtocol/Multiplayer/Repair MVP Prefab Registration")]
        public static void RepairPrefabRegistration()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            var player = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPath)?.GetComponent<NetworkPlayerController>();
            var room = AssetDatabase.LoadAssetAtPath<GameObject>(RoomPath)?.GetComponent<NetworkRoomState>();
            if (player == null || room == null) throw new InvalidOperationException("Generate prefabs first.");
            var list = GetPrefabList(player, room);
            Scene scene = SceneManager.GetSceneByPath(MenuPath);
            bool open = scene.IsValid() && scene.isLoaded;
            if (!open) scene = EditorSceneManager.OpenScene(MenuPath, OpenSceneMode.Additive);
            try
            {
                var managers = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<NetworkManager>(true)).ToArray();
                if (managers.Length != 1) throw new InvalidOperationException("MVP menu must have exactly one manager.");
                var lists = managers[0].NetworkConfig.Prefabs.NetworkPrefabsLists;
                lists.RemoveAll(x => x == null);
                // Replace only NGO's automatically generated empty/player-only default list.
                // Preserve user lists with additional content.
                lists.RemoveAll(x => AssetDatabase.GetAssetPath(x) == "Assets/DefaultNetworkPrefabs.asset"
                    && x.PrefabList.All(p => p.Prefab == player.gameObject));
                if (!lists.Contains(list)) lists.Add(list);
                EditorUtility.SetDirty(managers[0]);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!open) EditorSceneManager.SaveScene(scene);
                else Debug.Log("Prefab registration repaired in the open scene; save the scene yourself after review.");
                AssetDatabase.SaveAssets();
            }
            finally { if (!open) EditorSceneManager.CloseScene(scene, true); }
        }

        // Batch entry points operate only on the explicit validation project passed to Unity.
        public static void BatchSetup()
        {
            if (Application.isBatchMode && string.IsNullOrEmpty(SceneManager.GetActiveScene().path))
                EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
            Generate(); AddBuildScenes();
        }
        public static void BatchRepair()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("BatchRepair requires batch mode.");
            if (string.IsNullOrEmpty(SceneManager.GetActiveScene().path))
                EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
            RepairPrefabRegistration(); Validate();
        }
        [MenuItem("LastProtocol/Multiplayer/Build Windows Development MVP")]
        public static void BatchBuild() => BuildWindows("Builds/SmokePlayer");

        [MenuItem("LastProtocol/Player/Build Windows Third-Person Demo")]
        public static void BatchBuildController() => BuildWindows("Builds/ThirdPersonPlayer");

        [MenuItem("LastProtocol/Health/Build Windows Health Demo")]
        public static void BatchBuildHealth()
        {
            PlayerHealthSetupTool.Install();
            BuildWindows("Builds/HealthPlayer");
        }

        [MenuItem("LastProtocol/Combat/Build Windows Combat Demo")]
        public static void BatchBuildCombat()
        {
            CombatSetupTool.Install();
            BuildWindows("Builds/CombatPlayer");
        }

        [MenuItem("LastProtocol/Enemy/Build Windows Enemy Demo")]
        public static void BatchBuildEnemy()
        {
            EnemySetupTool.Install();
            BuildWindows("Builds/EnemyPlayer");
        }

        [MenuItem("LastProtocol/Revive/Build Windows Revive Demo")]
        public static void BatchBuildRevive()
        {
            ReviveSetupTool.Install();
            BuildWindows("Builds/RevivePlayer");
        }

        [MenuItem("LastProtocol/Menu/Build Windows Menu Demo")]
        public static void BatchBuildMenu()
        {
            MenuSetupTool.Install();
            BuildWindows("Builds/MenuPlayer");
        }

        [MenuItem("LastProtocol/Maps/Build Windows Can Tho Demo")]
        public static void BatchBuildCanTho()
        {
            CanThoMapSetupTool.Install();
            BuildWindows("Builds/CanThoPlayer");
        }

        [MenuItem("LastProtocol/Missions/Build Windows Campus Recon")]
        public static void BatchBuildMission()
        {
            MissionSetupTool.Install();
            BuildWindows("Builds/MissionPlayer");
        }

        [MenuItem("LastProtocol/Maps/Build Windows Campus Schematic")]
        public static void BatchBuildCampusSchematic()
        {
            CanThoDetailSetupTool.Apply();
            MissionSetupTool.Install();
            BuildWindows("Builds/CanThoSchematicPlayer");
        }

        [MenuItem("LastProtocol/Maps/Build Windows Campus Architecture")]
        public static void BatchBuildCampusArchitecture()
        {
            CanThoDetailSetupTool.Apply();
            MissionSetupTool.Install();
            BuildWindows("Builds/CanThoArchitecturePlayer");
        }

        [MenuItem("LastProtocol/Maps/Build Windows Campus Neighborhood")]
        public static void BatchBuildCampusNeighborhood()
        {
            CanThoDetailSetupTool.Apply();
            MissionSetupTool.Install();
            BuildWindows("Builds/CanThoNeighborhoodPlayer");
        }

        [MenuItem("LastProtocol/Maps/Build Windows Campus Periphery")]
        public static void BatchBuildCampusPeriphery()
        {
            CanThoDetailSetupTool.Apply();
            MissionSetupTool.Install();
            BuildWindows("Builds/CanThoPeripheralPlayer");
        }

        [MenuItem("LastProtocol/Maps/Build Windows Campus Streetscape")]
        public static void BatchBuildCampusStreetscape()
        {
            CanThoDetailSetupTool.Apply();
            MissionSetupTool.Install();
            BuildWindows("Builds/CanThoStreetscapePlayer");
        }

        [MenuItem("LastProtocol/Maps/Build Windows Campus Building Infill")]
        public static void BatchBuildCampusInfill()
        {
            CanThoDetailSetupTool.Apply();
            MissionSetupTool.Install();
            BuildWindows("Builds/CanThoInfillPlayer");
        }

        [MenuItem("LastProtocol/Maps/Build Windows Campus Vietnam Assets")]
        public static void BatchBuildCampusVietnamAssets()
        {
            CanThoDetailSetupTool.Apply();
            MissionSetupTool.Install();
            BuildWindows("Builds/CanThoVietnamPlayer");
        }

        private static void BuildWindows(string outputDirectory)
        {
            BatchSetup();
            Directory.CreateDirectory(outputDirectory);
            var buildScenes = new List<string> { MenuPath, GamePath };
            if (File.Exists(MenuSetupTool.YardPath)) buildScenes.Add(MenuSetupTool.YardPath);
            if (File.Exists(CanThoMapSetupTool.ScenePath)) buildScenes.Add(CanThoMapSetupTool.ScenePath);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = buildScenes.ToArray(),
                locationPathName = outputDirectory + "/LAST_PROTOCOL.exe", target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development });
            if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Windows build failed: " + report.summary.result);
            Debug.Log("LAST_PROTOCOL Windows development build PASS: " + outputDirectory);
        }
    }
}
