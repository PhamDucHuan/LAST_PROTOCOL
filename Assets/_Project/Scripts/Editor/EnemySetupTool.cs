using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace LastProtocol.Editor
{
    public static class EnemySetupTool
    {
        public const string EnemyPath = MultiplayerSetupTool.Root + "/Prefabs/Networking/RobotEnemy.prefab";
        public const string NavigationPath = MultiplayerSetupTool.Root + "/Settings/EnemyTestNavigation.asset";
        [MenuItem("LastProtocol/Enemy/Install Robot Enemy Demo")]
        public static void Install()
        {
            CombatSetupTool.Install();
            var navigation = GetNavigation();
            var enemy = GetEnemy(navigation);
            var player = AssetDatabase.LoadAssetAtPath<GameObject>(MultiplayerSetupTool.PlayerPath);
            var list = AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>(MultiplayerSetupTool.PrefabListPath);
            bool needsPlayer = player.GetComponent<NetworkHitscanWeapon>().enemyPrefab != enemy;
            bool needsList = !list.Contains(enemy.gameObject);
            if (needsPlayer || needsList)
            {
                string backup = "Builds/EnemyBackups/" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");
                Directory.CreateDirectory(backup);
                foreach (string path in new[] { MultiplayerSetupTool.PlayerPath, MultiplayerSetupTool.PrefabListPath })
                {
                    File.Copy(path, backup + "/" + Path.GetFileName(path));
                    File.Copy(path + ".meta", backup + "/" + Path.GetFileName(path) + ".meta");
                }
                if (needsPlayer)
                {
                    var root = PrefabUtility.LoadPrefabContents(MultiplayerSetupTool.PlayerPath);
                    try
                    {
                        root.GetComponent<NetworkHitscanWeapon>().enemyPrefab = enemy;
                        if (PrefabUtility.SaveAsPrefabAsset(root, MultiplayerSetupTool.PlayerPath) == null)
                            throw new IOException("Could not save Enemy reference on Player.");
                    }
                    finally { PrefabUtility.UnloadPrefabContents(root); }
                }
                if (needsList)
                { list.Add(new NetworkPrefab { Prefab = enemy.gameObject }); EditorUtility.SetDirty(list); AssetDatabase.SaveAssets(); }
                Debug.Log("Enemy integration backup: " + backup);
            }
            Validate();
        }
        private static NavMeshData GetNavigation()
        {
            var existing = AssetDatabase.LoadAssetAtPath<NavMeshData>(NavigationPath);
            if (existing != null) return existing;
            if (File.Exists(NavigationPath)) throw new InvalidOperationException("Existing navigation asset incompatible; preserved.");
            Scene scene = SceneManager.GetSceneByPath(MultiplayerSetupTool.GamePath);
            bool open = scene.IsValid() && scene.isLoaded;
            if (!open) scene = EditorSceneManager.OpenScene(MultiplayerSetupTool.GamePath, OpenSceneMode.Additive);
            try
            {
                var ground = scene.GetRootGameObjects().FirstOrDefault(x => x.name == "Ground")?.GetComponent<BoxCollider>();
                if (ground == null) throw new InvalidOperationException("Demo navigation expects the existing BoxCollider Ground.");
                var sources = new List<NavMeshBuildSource>();
                sources.Add(new NavMeshBuildSource { shape = NavMeshBuildSourceShape.Box, size = ground.size,
                    transform = ground.transform.localToWorldMatrix * Matrix4x4.Translate(ground.center), area = 0 });
                // Match the four static training pylons spawned by TrainingTarget. Do not bake moving Players.
                for (int i = 0; i < MultiplayerConfig.MaxPlayers; i++)
                    sources.Add(new NavMeshBuildSource { shape = NavMeshBuildSourceShape.Box,
                        size = new Vector3(1.1f, 2, 1.1f), transform = Matrix4x4.TRS(new Vector3(-9 + 6 * i, 1, 10), Quaternion.identity, Vector3.one), area = 1 });
                var settings = NavMesh.GetSettingsByIndex(0);
                settings.agentRadius = 0.4f; settings.agentHeight = 2; settings.agentClimb = 0.4f; settings.agentSlope = 45;
                settings.overrideVoxelSize = true; settings.voxelSize = 0.13f;
                Bounds bounds = ground.bounds; bounds.Expand(new Vector3(2, 6, 2));
                var data = NavMeshBuilder.BuildNavMeshData(settings, sources, bounds, Vector3.zero, Quaternion.identity);
                if (data == null) throw new InvalidOperationException("Enemy NavMesh bake failed.");
                AssetDatabase.CreateAsset(data, NavigationPath);
                return data;
            }
            finally { if (!open) EditorSceneManager.CloseScene(scene, true); }
        }
        private static RobotEnemy GetEnemy(NavMeshData navigation)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPath);
            if (existing != null) return existing.GetComponent<RobotEnemy>()
                ?? throw new InvalidOperationException("Existing Enemy prefab incompatible; preserved.");
            if (File.Exists(EnemyPath)) throw new InvalidOperationException("Existing Enemy asset preserved.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(MultiplayerSetupTool.Root + "/Materials/Player.mat");
            var previous = SceneManager.GetActiveScene();
            var scratch = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scratch);
                var root = new GameObject("RobotEnemy");
                root.AddComponent<NetworkObject>();
                var sync = root.AddComponent<NetworkTransform>(); sync.Interpolate = true;
                NetworkMotionSettings.Configure(sync);
                sync.SyncScaleX = sync.SyncScaleY = sync.SyncScaleZ = false;
                var collider = root.AddComponent<CapsuleCollider>(); collider.height = 2; collider.radius = 0.45f; collider.center = Vector3.up;
                var agent = root.AddComponent<NavMeshAgent>(); agent.enabled = false;
                agent.agentTypeID = NavMesh.GetSettingsByIndex(0).agentTypeID; agent.radius = 0.4f; agent.height = 2;
                var enemy = root.AddComponent<RobotEnemy>(); enemy.navigation = navigation;
                var view = root.AddComponent<RobotEnemyView>();
                view.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MultiplayerSetupTool.Root
                    + "/Settings/TextMeshPro/Resources/Fonts & Materials/LiberationSans SDF.asset");
                view.visual = new GameObject("Robot Visual").transform; view.visual.SetParent(root.transform, false);
                Part(view.visual, "Body", new Vector3(0, 1.15f, 0), new Vector3(0.8f, 0.9f, 0.6f), material);
                Part(view.visual, "Head", new Vector3(0, 1.85f, 0), new Vector3(0.55f, 0.45f, 0.5f), material);
                foreach (int side in new[] { -1, 1 })
                {
                    Part(view.visual, "Arm", new Vector3(side * 0.6f, 1.1f, 0), new Vector3(0.25f, 0.8f, 0.3f), material);
                    Part(view.visual, "Leg", new Vector3(side * 0.23f, 0.35f, 0), new Vector3(0.3f, 0.7f, 0.35f), material);
                }
                return PrefabUtility.SaveAsPrefabAsset(root, EnemyPath).GetComponent<RobotEnemy>();
            }
            finally
            {
                EditorSceneManager.CloseScene(scratch, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }
        private static void Part(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            var part = GameObject.CreatePrimitive(PrimitiveType.Cube); part.name = name;
            UnityEngine.Object.DestroyImmediate(part.GetComponent<Collider>());
            part.transform.SetParent(parent, false); part.transform.localPosition = position; part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = material;
        }
        public static void Validate()
        {
            CombatSetupTool.Validate();
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPath);
            var player = AssetDatabase.LoadAssetAtPath<GameObject>(MultiplayerSetupTool.PlayerPath);
            var list = AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>(MultiplayerSetupTool.PrefabListPath);
            if (root == null || root.GetComponents<RobotEnemy>().Length != 1 || root.GetComponent<NetworkTransform>() == null
                || root.GetComponent<NavMeshAgent>() == null || root.GetComponent<NavMeshAgent>().enabled
                || root.GetComponent<RobotEnemy>().navigation == null || root.GetComponent<RobotEnemyView>()?.font == null
                || root.GetComponent<RobotEnemyView>().visual == null || !list.Contains(root)
                || player.GetComponent<NetworkHitscanWeapon>().enemyPrefab != root.GetComponent<RobotEnemy>())
                throw new InvalidOperationException("Enemy prefab/navigation/registration invalid.");
            Debug.Log("LAST_PROTOCOL Enemy assets validation PASS");
        }
        public static void BatchInstall()
        { if (!Application.isBatchMode) throw new InvalidOperationException("Use Enemy menu for interactive setup."); Install(); }
    }
}
