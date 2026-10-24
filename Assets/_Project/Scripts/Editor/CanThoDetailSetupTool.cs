using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace LastProtocol.Editor
{
    public static class CanThoDetailSetupTool
    {
        public const string ProfilePath = "Assets/_Project/Settings/CanThoFuturePostProcessing.asset";
        public const string SkyPath = "Assets/_Project/Materials/CanThoCampus/CanThoFutureSky.mat";
        public const string ReflectionPath = "Assets/_Project/Settings/CanThoSkyReflection.cubemap";

        [MenuItem("LastProtocol/Maps/Apply Can Tho Detailed Art Revision")]
        public static void Apply()
        { ApplyRevision(false); }
        private static void ApplyRevision(bool rebuildOwnedGroups)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            var layout = CanThoMapSetupTool.ReadLayout();
            if (layout.artRevision < 2 || layout.artRevision > 13) throw new InvalidOperationException("Expected supported detailed art revision 2 to 13.");
            if (layout.artRevision >= 3 && (layout.playableRoute == null || layout.playableRoute.Length < 6))
                throw new InvalidOperationException("Campus playable route is missing.");
            if (Application.isBatchMode && string.IsNullOrEmpty(SceneManager.GetActiveScene().path))
                EditorSceneManager.OpenScene(MultiplayerSetupTool.MenuPath, OpenSceneMode.Single);
            var previous = SceneManager.GetActiveScene();
            var scene = SceneManager.GetSceneByPath(CanThoMapSetupTool.ScenePath);
            bool alreadyOpen = scene.IsValid() && scene.isLoaded;
            if (alreadyOpen && scene.isDirty) throw new InvalidOperationException("Save pending campus changes before applying detailed art.");
            CanThoMapSetupTool.ConfigureModel(layout);
            if (!alreadyOpen) scene = EditorSceneManager.OpenScene(CanThoMapSetupTool.ScenePath, OpenSceneMode.Additive);
            try
            {
                var environment = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<LevelEnvironment>()).Single();
                if (environment.artRevision == layout.artRevision && !rebuildOwnedGroups)
                {
                    if (!File.Exists(ReflectionPath))
                    {
                        var lightingBackup = "Builds/CanThoBackups/" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") + "-Lighting";
                        Directory.CreateDirectory(lightingBackup); File.Copy(CanThoMapSetupTool.ScenePath, lightingBackup + "/CanThoCampus.unity");
                        File.Copy(ProfilePath, lightingBackup + "/CanThoFuturePostProcessing.asset");
                        SceneManager.SetActiveScene(scene); ImproveLighting(scene);
                        EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
                    }
                    Validate(scene); return;
                }
                var backup = "Builds/CanThoBackups/" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") + "-DetailedScene";
                Directory.CreateDirectory(backup);
                foreach (string path in new[] { CanThoMapSetupTool.ScenePath, CanThoMapSetupTool.NavigationPath, CanThoMapSetupTool.MenuPath })
                { File.Copy(path, backup + "/" + Path.GetFileName(path)); File.Copy(path + ".meta", backup + "/" + Path.GetFileName(path) + ".meta"); }
                SceneManager.SetActiveScene(scene);
                var root = environment.transform;
                var oldModels = root.Cast<Transform>().Where(x => PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(x.gameObject) == CanThoMapSetupTool.ModelPath).ToArray();
                if (oldModels.Length != 1) throw new InvalidOperationException("Expected one owned imported model; scene preserved for review.");
                var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(CanThoMapSetupTool.ModelPath), scene);
                model.transform.SetParent(root, false);
                foreach (var renderer in model.GetComponentsInChildren<MeshRenderer>())
                    GameObjectUtility.SetStaticEditorFlags(renderer.gameObject, StaticEditorFlags.BatchingStatic);
                UnityEngine.Object.DestroyImmediate(oldModels[0].gameObject);
                var oldCollision = root.Find("Simple Environment Colliders");
                if (oldCollision == null) throw new InvalidOperationException("Owned collider group missing.");
                UnityEngine.Object.DestroyImmediate(oldCollision.gameObject);
                var collisionRoot = new GameObject("Simple Environment Colliders"); collisionRoot.transform.SetParent(root, false);
                var sources = new List<NavMeshBuildSource>();
                foreach (var box in layout.collisions)
                {
                    var go = new GameObject(box.name); go.transform.SetParent(collisionRoot.transform, false); go.transform.position = box.position;
                    go.transform.rotation = Quaternion.Euler(box.euler);
                    go.AddComponent<BoxCollider>().size = box.size;
                    sources.Add(new NavMeshBuildSource { shape = NavMeshBuildSourceShape.Box, size = box.size,
                        transform = Matrix4x4.TRS(box.position, Quaternion.Euler(box.euler), Vector3.one), area = box.name == "Campus soil" ? 0 : 1 });
                }
                for (int slot = 0; slot < MultiplayerConfig.MaxPlayers; slot++) sources.Add(new NavMeshBuildSource
                { shape = NavMeshBuildSourceShape.Box, size = new Vector3(1.1f, 2, 1.1f), transform = Matrix4x4.Translate(new Vector3(-9 + slot * 6, 1, 10)), area = 1 });
                var settings = NavMesh.GetSettingsByIndex(0); settings.agentRadius = 0.4f; settings.agentHeight = 2; settings.agentClimb = 0.4f;
                settings.overrideVoxelSize = true; settings.voxelSize = 0.2f;
                var bounds = new Bounds(layout.bounds.center, layout.bounds.size + new Vector3(4, 8, 4));
                if (!NavMeshBuilder.UpdateNavMeshData(environment.enemyNavigation, settings, sources, bounds)) throw new InvalidOperationException("Detailed navigation bake failed.");
                EditorUtility.SetDirty(environment.enemyNavigation);
                var profile = CreateProfile();
                var volume = root.GetComponent<Volume>() ?? root.gameObject.AddComponent<Volume>();
                volume.isGlobal = true; volume.priority = 20; volume.sharedProfile = profile;
                foreach (var camera in scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<UnityEngine.Camera>()))
                {
                    var data = camera.GetUniversalAdditionalCameraData(); data.renderPostProcessing = true;
                    data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing; data.antialiasingQuality = AntialiasingQuality.High;
                    camera.allowHDR = true;
                    if (layout.artRevision >= 3) camera.nearClipPlane = 0.08f;
                }
                if (layout.artRevision >= 3)
                {
                    foreach (var follow in scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<CinemachineThirdPersonFollow>()))
                    {
                        follow.AvoidObstacles.Enabled = true; follow.AvoidObstacles.CollisionFilter = 1;
                        follow.AvoidObstacles.IgnoreTag = "Player"; follow.AvoidObstacles.CameraRadius = 0.28f;
                        follow.AvoidObstacles.DampingIntoCollision = 0; follow.AvoidObstacles.DampingFromCollision = 0.5f;
                    }
                    var previousRoute = root.Cast<Transform>().SingleOrDefault(x => x.name == "Playable Campus Route");
                    if (previousRoute != null) UnityEngine.Object.DestroyImmediate(previousRoute.gameObject);
                    var routeRoot = new GameObject("Playable Campus Route"); routeRoot.transform.SetParent(root, false);
                    for (int i = 0; i < layout.playableRoute.Length; i++)
                    {
                        var point = new GameObject((i + 1).ToString("00") + " - Route waypoint");
                        point.transform.SetParent(routeRoot.transform, false); point.transform.position = layout.playableRoute[i];
                    }
                    var markers = root.Cast<Transform>().SingleOrDefault(x => x.name == "Landmark Markers / Editable");
                    var oldNeighborhood = root.Find("Playable Neighborhood Route");
                    var oldProbe = root.Find("Neighborhood Camera Probe");
                    if (oldProbe != null) UnityEngine.Object.DestroyImmediate(oldProbe.gameObject);
                    if (layout.artRevision >= 7)
                    {
                        var probe = new GameObject("Neighborhood Camera Probe"); probe.transform.SetParent(root, false);
                        probe.transform.SetPositionAndRotation(layout.neighborhoodCameraProbe, Quaternion.Euler(layout.neighborhoodCameraEuler));
                    }
                    if (oldNeighborhood != null) UnityEngine.Object.DestroyImmediate(oldNeighborhood.gameObject);
                    if (layout.neighborhoodRoute != null && layout.neighborhoodRoute.Length > 1)
                    {
                        var neighborhoodRoot = new GameObject("Playable Neighborhood Route"); neighborhoodRoot.transform.SetParent(root, false);
                        for (int i = 0; i < layout.neighborhoodRoute.Length; i++)
                        {
                            var point = new GameObject((i + 1).ToString("00") + " - Neighborhood waypoint");
                            point.transform.SetParent(neighborhoodRoot.transform, false); point.transform.position = layout.neighborhoodRoute[i];
                        }
                    }
                    if (markers != null)
                    {
                        foreach (var child in markers.Cast<Transform>().ToArray()) UnityEngine.Object.DestroyImmediate(child.gameObject);
                        foreach (var landmark in layout.landmarks)
                        {
                            var marker = new GameObject(landmark.title);
                            marker.transform.SetParent(markers, false); marker.transform.position = landmark.approach;
                        }
                    }
                }
                foreach (var light in scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Light>()))
                { light.intensity = 1.65f; light.color = new Color(1, 0.9f, 0.77f); light.transform.rotation = Quaternion.Euler(38, -40, 0); }
                RenderSettings.ambientSkyColor = new Color(0.34f, 0.45f, 0.55f); RenderSettings.ambientEquatorColor = new Color(0.27f, 0.33f, 0.35f);
                RenderSettings.ambientGroundColor = new Color(0.14f, 0.18f, 0.18f); RenderSettings.fogColor = new Color(0.37f, 0.48f, 0.55f);
                RenderSettings.fogStartDistance = 180; RenderSettings.fogEndDistance = 850;
                var sky = AssetDatabase.LoadAssetAtPath<Material>(SkyPath);
                if (sky == null)
                {
                    sky = new Material(Shader.Find("Skybox/Procedural")); sky.SetColor("_SkyTint", new Color(0.32f, 0.43f, 0.54f));
                    sky.SetColor("_GroundColor", new Color(0.12f, 0.17f, 0.19f)); sky.SetFloat("_Exposure", 0.9f); AssetDatabase.CreateAsset(sky, SkyPath);
                }
                RenderSettings.skybox = sky;
                environment.artRevision = layout.artRevision; EditorUtility.SetDirty(environment);
                ImproveLighting(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save detailed campus scene.");
                UpdateMenuVersion(); AssetDatabase.SaveAssets(); Validate(scene);
                Debug.Log("LAST_PROTOCOL Can Tho detailed art revision " + layout.artRevision + " PASS; backup=" + backup);
            }
            finally
            {
                if (!alreadyOpen) EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }
        private static VolumeProfile CreateProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if (profile != null) return profile;
            profile = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(profile, ProfilePath);
            var bloom = profile.Add<Bloom>(true); bloom.intensity.Override(0.35f); bloom.threshold.Override(1.1f); bloom.scatter.Override(0.6f);
            var tone = profile.Add<Tonemapping>(true); tone.mode.Override(TonemappingMode.ACES);
            var color = profile.Add<ColorAdjustments>(true); color.contrast.Override(7); color.saturation.Override(-5);
            foreach (var component in profile.components) AssetDatabase.AddObjectToAsset(component, profile);
            EditorUtility.SetDirty(profile); return profile;
        }
        private static void ImproveLighting(Scene scene)
        {
            // Original procedural sky reflection keeps metallic/glass surfaces readable
            // without requiring an external HDRI or a runtime reflection bake.
            var cube = AssetDatabase.LoadAssetAtPath<Cubemap>(ReflectionPath);
            if (cube == null)
            {
                const int size = 32;
                cube = new Cubemap(size, TextureFormat.RGBAHalf, true) { name = "Can Tho Procedural Sky Reflection" };
                foreach (CubemapFace face in new[] { CubemapFace.PositiveX, CubemapFace.NegativeX, CubemapFace.PositiveY, CubemapFace.NegativeY, CubemapFace.PositiveZ, CubemapFace.NegativeZ })
                {
                    var pixels = new Color[size * size];
                    for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
                    {
                        float t = face == CubemapFace.PositiveY ? 1 : face == CubemapFace.NegativeY ? 0 : (y + 0.5f) / size;
                        pixels[y * size + x] = Color.Lerp(new Color(0.15f, 0.21f, 0.21f), new Color(0.52f, 0.7f, 0.88f), t);
                    }
                    cube.SetPixels(pixels, face);
                }
                cube.Apply(true, false); AssetDatabase.CreateAsset(cube, ReflectionPath);
            }
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom; RenderSettings.customReflectionTexture = cube; RenderSettings.reflectionIntensity = 1;
            RenderSettings.ambientSkyColor = new Color(0.43f, 0.55f, 0.65f); RenderSettings.ambientEquatorColor = new Color(0.36f, 0.43f, 0.45f);
            RenderSettings.ambientGroundColor = new Color(0.2f, 0.25f, 0.25f);
            foreach (var light in scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Light>()))
            { light.intensity = 2.1f; light.shadows = LightShadows.Soft; light.shadowStrength = 0.75f; }
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if (profile.TryGet<ColorAdjustments>(out var color)) { color.postExposure.Override(0.55f); EditorUtility.SetDirty(color); }
            EditorUtility.SetDirty(profile);
        }
        private static void UpdateMenuVersion()
        {
            var menu = PrefabUtility.LoadPrefabContents(CanThoMapSetupTool.MenuPath);
            try
            {
                foreach (var text in menu.GetComponentsInChildren<TMP_Text>(true))
                    if (text.text.StartsWith("PROTOTYPE  /  BUILD ", StringComparison.Ordinal)) text.text = "PROTOTYPE  /  BUILD " + MultiplayerConfig.ProtocolVersion.ToString("00");
                PrefabUtility.SaveAsPrefabAsset(menu, CanThoMapSetupTool.MenuPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(menu); }
        }
        private static void Validate(Scene scene)
        {
            var roots = scene.GetRootGameObjects();
            var environment = roots.SelectMany(x => x.GetComponentsInChildren<LevelEnvironment>()).Single();
            if (environment.artRevision != CanThoMapSetupTool.ReadLayout().artRevision || environment.GetComponent<Volume>()?.sharedProfile == null)
                throw new InvalidOperationException("Detailed environment profile/revision missing.");
            if (roots.SelectMany(x => x.GetComponentsInChildren<Collider>()).Count() < CanThoMapSetupTool.ReadLayout().collisions.Length)
                throw new InvalidOperationException("Detailed collision layout incomplete.");
            CanThoMapSetupTool.Validate();
        }
        public static void BatchApply() { if (!Application.isBatchMode) throw new InvalidOperationException("Use the Maps menu interactively."); Apply(); }
        // Explicit batch refresh for a corrected layout; normal Apply remains idempotent.
        public static void BatchRefresh()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Refresh is for the validation batch workflow.");
            ApplyRevision(true);
        }
    }
}
