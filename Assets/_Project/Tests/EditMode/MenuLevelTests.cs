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
    public sealed class MenuLevelTests
    {
        [TestCase(-1)] [TestCase(LevelCatalog.Count)] [TestCase(int.MaxValue)]
        public void UnknownLevelCannotResolveToAScene(int id)
        { Assert.IsFalse(LevelCatalog.IsValid(id)); Assert.IsNull(LevelCatalog.Get(id)); }
        [Test]
        public void PlayableLevelsHaveStableUniqueSceneNames()
        {
            var levels = Enumerable.Range(0, LevelCatalog.Count).Select(LevelCatalog.Get).ToArray();
            Assert.AreEqual(LevelCatalog.Count, levels.Length); Assert.AreEqual(LevelCatalog.Count, levels.Select(x => x.SceneName).Distinct().Count());
            foreach (var level in levels) Assert.IsTrue(File.Exists(level.ScenePath), level.ScenePath);
        }
        [Test]
        public void UnspawnedRoomRejectsLevelChanges()
        {
            var go = new GameObject("Room test", typeof(Unity.Netcode.NetworkObject));
            try { Assert.IsFalse(go.AddComponent<NetworkRoomState>().SelectLevelServer(1)); }
            finally { Object.DestroyImmediate(go); }
        }
        [Test]
        public void MenuInstallerPreservesOriginalScenesAndIsIdempotent()
        {
            var paths = new[] { MultiplayerSetupTool.MenuPath, MultiplayerSetupTool.GamePath, "Assets/Scenes/SampleScene.unity" };
            var before = paths.Select(File.ReadAllBytes).ToArray();
            var active = SceneManager.GetActiveScene();
            MenuSetupTool.Install();
            var generated = new[] { MenuSetupTool.MenuPrefabPath, MenuSetupTool.YardPath, MenuSetupTool.YardNavigationPath, MultiplayerSetupTool.ConfigPath };
            var bytes = generated.Select(File.ReadAllBytes).ToArray();
            var build = EditorBuildSettings.scenes.Select(x => x.path + x.enabled).ToArray();
            MenuSetupTool.Install();
            for (int i = 0; i < paths.Length; i++) CollectionAssert.AreEqual(before[i], File.ReadAllBytes(paths[i]));
            for (int i = 0; i < generated.Length; i++) CollectionAssert.AreEqual(bytes[i], File.ReadAllBytes(generated[i]));
            CollectionAssert.AreEqual(build, EditorBuildSettings.scenes.Select(x => x.path + x.enabled));
            Assert.AreEqual(active, SceneManager.GetActiveScene());
        }
        [Test]
        public void YardHasFourSpawnsNoManagerAndCoverNavigation()
        {
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(MenuSetupTool.YardPath, OpenSceneMode.Additive);
            NavMeshDataInstance instance = default;
            try
            {
                var roots = scene.GetRootGameObjects();
                Assert.AreEqual(0, roots.Sum(x => x.GetComponentsInChildren<Unity.Netcode.NetworkManager>(true).Length));
                Assert.AreEqual(4, roots.SelectMany(x => x.GetComponentsInChildren<PlayerSpawnPoints>()).Single().Count);
                var environment = roots.SelectMany(x => x.GetComponentsInChildren<LevelEnvironment>()).Single();
                Assert.AreEqual(LevelCatalog.Containment, environment.levelId);
                instance = NavMesh.AddNavMeshData(environment.enemyNavigation);
                Assert.IsTrue(instance.valid);
                Assert.IsTrue(NavMesh.SamplePosition(new Vector3(0, 0, 15), out _, 1, NavMesh.AllAreas));
                Assert.IsFalse(NavMesh.SamplePosition(new Vector3(-6, 0, 7), out _, 0.2f, NavMesh.AllAreas));
                var path = new NavMeshPath();
                Assert.IsTrue(NavMesh.CalculatePath(new Vector3(0, 0, -2), new Vector3(0, 0, 15), NavMesh.AllAreas, path));
                Assert.AreEqual(NavMeshPathStatus.PathComplete, path.status);
            }
            finally { if (instance.valid) instance.Remove(); EditorSceneManager.CloseScene(scene, true); if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous); }
        }
    }
}
