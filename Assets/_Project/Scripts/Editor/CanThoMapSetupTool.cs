using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace LastProtocol.Editor
{
    // Creates new assets only. Existing maps, imported art and customized scenes are preserved.
    public static class CanThoMapSetupTool
    {
        public const string ArtRoot = "Assets/_Project/Art/Environments/CanThoCampus";
        public const string ModelPath = ArtRoot + "/CanThoCampus.fbx";
        public const string LayoutPath = ArtRoot + "/CanThoCampus.layout.json";
        public const string ScenePath = "Assets/_Project/Scenes/CanThoCampus/CanThoCampus.unity";
        public const string NavigationPath = "Assets/_Project/Settings/CanThoCampusNavigation.asset";
        public const string MenuPath = "Assets/_Project/Prefabs/UI/CanThoOperationsMenu.prefab";
        private const string MaterialsPath = "Assets/_Project/Materials/CanThoCampus";

        [Serializable] public sealed class Palette { public string name; public Color color; public float roughness, metallic, emission; }
        [Serializable] public sealed class CollisionBox { public string name; public Vector3 position, size; }
        [Serializable] public sealed class Landmark { public string id, title; public Vector3 position, approach; }
        [Serializable] public sealed class LayoutBounds { public Vector3 center, size; }
        [Serializable] public sealed class Layout
        {
            public int version;
            public int artRevision;
            public string title, note;
            public LayoutBounds bounds;
            public Palette[] materials;
            public CollisionBox[] collisions;
            public Landmark[] landmarks;
            public Vector3[] navigationChecks;
        }

        public static Layout ReadLayout()
        {
            var layout = JsonUtility.FromJson<Layout>(File.ReadAllText(LayoutPath));
            if (layout == null || layout.version != 1 || layout.collisions == null || layout.collisions.Length < 4
                || layout.materials == null || layout.landmarks == null || layout.navigationChecks == null)
                throw new InvalidOperationException("Can Tho layout is missing or incompatible.");
            return layout;
        }

        [MenuItem("LastProtocol/Maps/Install Can Tho Campus")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode before installing the map.");
            if (Application.isBatchMode && string.IsNullOrEmpty(SceneManager.GetActiveScene().path))
                EditorSceneManager.OpenScene(MultiplayerSetupTool.MenuPath, OpenSceneMode.Single);
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (string.IsNullOrEmpty(SceneManager.GetSceneAt(i).path))
                    throw new InvalidOperationException("Save the untitled scene before installation. No current scene will be changed.");
            if (!File.Exists(ModelPath) || !File.Exists(LayoutPath)) throw new FileNotFoundException("Import the Blender FBX and layout into " + ArtRoot);
            var layout = ReadLayout();
            Directory.CreateDirectory(MaterialsPath);
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            AssetDatabase.Refresh();
            // Baseline features already exist. Do not regenerate or replace their authored scenes/prefabs.
            ReviveSetupTool.Validate();
            ConfigureModel(layout);
            if (!File.Exists(ScenePath)) CreateScene(layout);
            else if (AssetDatabase.LoadAssetAtPath<NavMeshData>(NavigationPath) == null)
                throw new InvalidOperationException("Existing Can Tho scene has no navigation asset; preserved for manual review.");
            var menu = AssetDatabase.LoadAssetAtPath<GameObject>(MenuPath);
            if (menu == null)
            {
                if (File.Exists(MenuPath)) throw new InvalidOperationException("Existing menu asset preserved: " + MenuPath);
                menu = MenuSetupTool.CreateMenuAtPath(MenuPath);
            }
            if (menu.GetComponent<MenuFrontEnd>()?.levelButtons.Length != LevelCatalog.Count)
                throw new InvalidOperationException("Existing Can Tho menu is incompatible; preserved.");
            var config = AssetDatabase.LoadAssetAtPath<MultiplayerConfig>(MultiplayerSetupTool.ConfigPath);
            if (config.menuPrefab != menu.GetComponent<MenuFrontEnd>())
            {
                Backup(MultiplayerSetupTool.ConfigPath);
                config.menuPrefab = menu.GetComponent<MenuFrontEnd>(); EditorUtility.SetDirty(config);
            }
            var scenes = EditorBuildSettings.scenes.ToList();
            if (!scenes.Any(x => x.path == ScenePath && x.enabled))
            {
                Backup("ProjectSettings/EditorBuildSettings.asset");
                int index = scenes.FindIndex(x => x.path == ScenePath);
                if (index < 0) scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
                else scenes[index] = new EditorBuildSettingsScene(ScenePath, true);
                EditorBuildSettings.scenes = scenes.ToArray();
            }
            AssetDatabase.SaveAssets(); Validate();
            Debug.Log("LAST_PROTOCOL Can Tho Campus installation PASS. Existing scenes preserved.");
        }

        private static void Backup(string path)
        {
            string dir = "Builds/CanThoBackups/" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");
            Directory.CreateDirectory(dir); File.Copy(path, dir + "/" + Path.GetFileName(path));
            if (File.Exists(path + ".meta")) File.Copy(path + ".meta", dir + "/" + Path.GetFileName(path) + ".meta");
        }
        internal static void ConfigureModel(Layout layout)
        {
            var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            if (importer == null) throw new InvalidOperationException("FBX importer unavailable.");
            bool changed = false;
            foreach (var entry in layout.materials)
            {
                string path = MaterialsPath + "/" + entry.name + ".mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    if (File.Exists(path)) throw new InvalidOperationException("Incompatible material preserved: " + path);
                    material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = entry.name };
                    material.SetColor("_BaseColor", entry.color); material.SetFloat("_Metallic", entry.metallic);
                    material.SetFloat("_Smoothness", 1 - entry.roughness);
                    if (entry.emission > 0)
                    { material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", entry.color * entry.emission); }
                    AssetDatabase.CreateAsset(material, path);
                }
                var identifier = new AssetImporter.SourceAssetIdentifier(typeof(Material), entry.name);
                if (!importer.GetExternalObjectMap().TryGetValue(identifier, out var mapped) || mapped != material)
                { importer.AddRemap(identifier, material); changed = true; }
            }
            if (importer.importCameras) { importer.importCameras = false; changed = true; }
            if (importer.importLights) { importer.importLights = false; changed = true; }
            if (importer.importAnimation) { importer.importAnimation = false; changed = true; }
            if (importer.addCollider) { importer.addCollider = false; changed = true; }
            if (changed) importer.SaveAndReimport();
        }

        private static void CreateScene(Layout layout)
        {
            if (File.Exists(NavigationPath)) throw new InvalidOperationException("Orphan navigation asset preserved. Review it before installation.");
            if (!AssetDatabase.CopyAsset(MultiplayerSetupTool.GamePath, ScenePath)) throw new IOException("Could not copy gameplay scene.");
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                var ground = scene.GetRootGameObjects().Single(x => x.name == "Ground");
                UnityEngine.Object.DestroyImmediate(ground);
                var root = new GameObject("Can Tho Campus / Blender Environment");
                var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath), scene);
                model.transform.SetParent(root.transform, false);
                foreach (var renderer in model.GetComponentsInChildren<MeshRenderer>())
                {
                    GameObjectUtility.SetStaticEditorFlags(renderer.gameObject, StaticEditorFlags.BatchingStatic);
                    renderer.shadowCastingMode = ShadowCastingMode.On;
                }
                var colliders = new GameObject("Simple Environment Colliders"); colliders.transform.SetParent(root.transform, false);
                var sources = new List<NavMeshBuildSource>();
                foreach (var box in layout.collisions)
                {
                    var go = new GameObject(box.name); go.transform.SetParent(colliders.transform, false);
                    go.transform.position = box.position;
                    go.AddComponent<BoxCollider>().size = box.size;
                    sources.Add(new NavMeshBuildSource { shape = NavMeshBuildSourceShape.Box, size = box.size,
                        transform = Matrix4x4.Translate(box.position), area = box.name == "Campus soil" ? 0 : 1 });
                }
                // Training targets are spawned at runtime. Reserve them in static navigation.
                for (int i = 0; i < MultiplayerConfig.MaxPlayers; i++) sources.Add(new NavMeshBuildSource
                { shape = NavMeshBuildSourceShape.Box, size = new Vector3(1.1f, 2, 1.1f),
                    transform = Matrix4x4.Translate(new Vector3(-9 + i * 6, 1, 10)), area = 1 });
                var settings = NavMesh.GetSettingsByIndex(0);
                settings.agentRadius = 0.4f; settings.agentHeight = 2; settings.agentClimb = 0.4f;
                settings.overrideVoxelSize = true; settings.voxelSize = 0.2f;
                var navBounds = new Bounds(layout.bounds.center, layout.bounds.size + new Vector3(4, 8, 4));
                var data = NavMeshBuilder.BuildNavMeshData(settings, sources, navBounds, Vector3.zero, Quaternion.identity);
                if (data == null) throw new InvalidOperationException("Can Tho navigation bake failed.");
                AssetDatabase.CreateAsset(data, NavigationPath);
                var environment = root.AddComponent<LevelEnvironment>(); environment.levelId = LevelCatalog.CanTho; environment.enemyNavigation = data;
                var points = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<PlayerSpawnPoints>()).Single();
                for (int i = 0; i < points.Count; i++) points.points[i].position = new Vector3((i % 2) * 4 - 2, 1.1f, -35 + (i / 2) * 4);
                var markers = new GameObject("Landmark Markers / Editable"); markers.transform.SetParent(root.transform, false);
                foreach (var landmark in layout.landmarks)
                { var marker = new GameObject(landmark.title); marker.transform.SetParent(markers.transform, false); marker.transform.position = landmark.approach; }
                foreach (var light in scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Light>()))
                { light.color = new Color(1, 0.92f, 0.77f); light.intensity = 2; light.transform.rotation = Quaternion.Euler(48, -35, 0); }
                foreach (var camera in scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<UnityEngine.Camera>())) camera.farClipPlane = 850;
                RenderSettings.ambientMode = AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = new Color(0.52f, 0.65f, 0.73f);
                RenderSettings.ambientEquatorColor = new Color(0.4f, 0.45f, 0.4f);
                RenderSettings.ambientGroundColor = new Color(0.23f, 0.25f, 0.2f);
                RenderSettings.fog = true; RenderSettings.fogColor = new Color(0.7f, 0.79f, 0.8f);
                RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogStartDistance = 140; RenderSettings.fogEndDistance = 700;
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save Can Tho scene.");
            }
            finally { EditorSceneManager.CloseScene(scene, true); if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous); }
        }

        public static void Validate()
        {
            if (!File.Exists(ScenePath) || AssetDatabase.LoadAssetAtPath<NavMeshData>(NavigationPath) == null)
                throw new InvalidOperationException("Can Tho scene/navigation missing.");
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (model == null || model.GetComponentsInChildren<MeshRenderer>().Length < 20)
                throw new InvalidOperationException("Campus model import failed.");
            foreach (var renderer in model.GetComponentsInChildren<MeshRenderer>())
                if (renderer.sharedMaterials.Any(x => x == null || x.shader == null || x.shader.name != "Universal Render Pipeline/Lit"))
                    throw new InvalidOperationException("Campus material remap invalid: " + renderer.name);
            MenuSetupTool.Validate();
        }
        public static void BatchInstall() { if (!Application.isBatchMode) throw new InvalidOperationException("Use the Maps menu interactively."); Install(); }

        // Run only in the isolated validation Editor, with graphics enabled.
        public static void BatchCapture()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Batch capture is for the validation copy.");
            var args = Environment.GetCommandLineArgs(); int index = Array.IndexOf(args, "--lp-map-captures");
            if (index < 0 || index + 1 >= args.Length) throw new ArgumentException("Missing capture directory.");
            string directory = Path.GetFullPath(args[index + 1]); Directory.CreateDirectory(directory);
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var go = new GameObject("Validation Capture Camera"); var camera = go.AddComponent<UnityEngine.Camera>(); camera.enabled = false;
            var captureData = camera.GetUniversalAdditionalCameraData(); captureData.renderPostProcessing = true;
            captureData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing; captureData.antialiasingQuality = AntialiasingQuality.High;
            camera.allowHDR = true;
            camera.farClipPlane = 1600; camera.clearFlags = CameraClearFlags.Skybox;
            var positions = new[] { new Vector3(370, 410, -350), new Vector3(24, 7, -77), new Vector3(-64, 6, -19), new Vector3(82, 7, -42), new Vector3(-8, 7, -40), new Vector3(30, 3, -67) };
            var targets = new[] { new Vector3(-20, 0, 105), new Vector3(0, 5, -40), new Vector3(-45, 8, 12), new Vector3(57, 8, -25), new Vector3(-33, 12, -21), new Vector3(36, 2.2f, -49.5f) };
            var names = new[] { "Unity-CanTho-Overview", "Unity-CanTho-GateB", "Unity-CanTho-TurtleHall", "Unity-CanTho-Library", "Unity-CanTho-ATL", "Unity-CanTho-FrontFence" };
            for (int i = 0; i < positions.Length; i++)
            {
                // Overview is an inspection view, so remove distance fog temporarily.
                RenderSettings.fog = i != 0;
                camera.transform.position = positions[i]; camera.transform.LookAt(targets[i]);
                camera.orthographic = i == 0; camera.orthographicSize = 240; camera.fieldOfView = 65;
                var texture = new RenderTexture(1600, 1000, 24); texture.Create();
                for (int frame = 0; frame < 4; frame++) RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = texture });
                var previous = RenderTexture.active; RenderTexture.active = texture;
                var pixels = new Texture2D(texture.width, texture.height, TextureFormat.RGB24, false);
                pixels.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0); pixels.Apply();
                File.WriteAllBytes(Path.Combine(directory, names[i] + ".png"), pixels.EncodeToPNG());
                RenderTexture.active = previous; UnityEngine.Object.DestroyImmediate(pixels); texture.Release(); UnityEngine.Object.DestroyImmediate(texture);
            }
            UnityEngine.Object.DestroyImmediate(go);
            Debug.Log("LAST_PROTOCOL Can Tho Unity captures saved: " + directory);
        }
    }
}
