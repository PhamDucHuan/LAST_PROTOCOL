using System.IO;
using System.Linq;
using LastProtocol.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace LastProtocol.Tests
{
    public sealed class CanThoMapTests
    {
        [SetUp]
        public void OpenSavedSceneInBatchFixture()
        {
            if (Application.isBatchMode && string.IsNullOrEmpty(SceneManager.GetActiveScene().path))
                EditorSceneManager.OpenScene(MultiplayerSetupTool.MenuPath, OpenSceneMode.Single);
        }
        [Test]
        public void CampusNavigationConnectsGatesLandmarksAndEnemyWithoutWalkingThroughBuildings()
        {
            var data = AssetDatabase.LoadAssetAtPath<NavMeshData>(CanThoMapSetupTool.NavigationPath);
            Assert.NotNull(data);
            var instance = NavMesh.AddNavMeshData(data);
            try
            {
                Assert.IsTrue(NavMesh.SamplePosition(new Vector3(0, 0, 15), out var home, 0.5f, NavMesh.AllAreas), "Enemy home");
                foreach (var point in CanThoMapSetupTool.ReadLayout().navigationChecks)
                {
                    Assert.IsTrue(NavMesh.SamplePosition(point, out var target, 1, NavMesh.AllAreas), point.ToString());
                    var path = new NavMeshPath();
                    Assert.IsTrue(NavMesh.CalculatePath(home.position, target.position, NavMesh.AllAreas, path), point.ToString());
                    Assert.AreEqual(NavMeshPathStatus.PathComplete, path.status, point.ToString());
                }
                Assert.IsFalse(NavMesh.SamplePosition(new Vector3(28, 0, -2), out _, 1, NavMesh.AllAreas), "Administration interior must be blocked");
                Assert.IsFalse(NavMesh.SamplePosition(new Vector3(-80, 0, 205), out _, 1, NavMesh.AllAreas), "Research interior must be blocked");
            }
            finally { instance.Remove(); }
        }

        [Test]
        public void CampusSceneHasCollisionFourSafeSpawnsAndNoExtraNetworkManager()
        {
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(CanThoMapSetupTool.ScenePath, OpenSceneMode.Additive);
            try
            {
                var roots = scene.GetRootGameObjects();
                Assert.AreEqual(0, roots.Sum(x => x.GetComponentsInChildren<Unity.Netcode.NetworkManager>(true).Length));
                Assert.AreEqual(1, roots.Sum(x => x.GetComponentsInChildren<AudioListener>(true).Length));
                var spawns = roots.SelectMany(x => x.GetComponentsInChildren<PlayerSpawnPoints>()).Single();
                Assert.AreEqual(MultiplayerConfig.MaxPlayers, spawns.Count);
                var colliders = roots.SelectMany(x => x.GetComponentsInChildren<BoxCollider>()).ToArray();
                Physics.SyncTransforms();
                foreach (var point in spawns.points)
                {
                    Assert.IsTrue(colliders.Any(x => x.Raycast(new Ray(point.position, Vector3.down), out _, 3)), "Spawn needs ground");
                    Assert.IsFalse(colliders.Any(x => x.bounds.Contains(point.position)), "Spawn inside solid geometry");
                }
                var body = colliders.Single(x => x.name == "NHÀ ĐIỀU HÀNH");
                Assert.IsTrue(body.Raycast(new Ray(new Vector3(28, 1.5f, -30), Vector3.forward), out _, 30), "Building blocks gunfire/player");
                Assert.AreEqual(LevelCatalog.CanTho, roots.SelectMany(x => x.GetComponentsInChildren<LevelEnvironment>()).Single().levelId);
                foreach (var transform in roots.SelectMany(x => x.GetComponentsInChildren<Transform>(true)))
                    Assert.AreEqual(0, GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject), transform.name);
            }
            finally { EditorSceneManager.CloseScene(scene, true); if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous); }
        }

        [Test]
        public void CampusInstallerPreservesExistingMapsAndCanBeRunTwice()
        {
            var paths = new[] { MultiplayerSetupTool.MenuPath, MultiplayerSetupTool.GamePath, MenuSetupTool.YardPath,
                CanThoMapSetupTool.ScenePath, CanThoMapSetupTool.MenuPath, CanThoMapSetupTool.NavigationPath,
                "ProjectSettings/EditorBuildSettings.asset", MultiplayerSetupTool.ConfigPath };
            var before = paths.Select(File.ReadAllBytes).ToArray();
            var active = SceneManager.GetActiveScene();
            CanThoMapSetupTool.Install(); CanThoMapSetupTool.Install();
            for (int i = 0; i < paths.Length; i++) CollectionAssert.AreEqual(before[i], File.ReadAllBytes(paths[i]), paths[i]);
            Assert.AreEqual(active, SceneManager.GetActiveScene());
            Assert.AreEqual(LevelCatalog.Count, AssetDatabase.LoadAssetAtPath<MultiplayerConfig>(MultiplayerSetupTool.ConfigPath).menuPrefab.levelButtons.Length);
            CanThoMapSetupTool.Validate();
        }
    }
}
