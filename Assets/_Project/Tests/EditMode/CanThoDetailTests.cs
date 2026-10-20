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
    public sealed class CanThoDetailTests
    {
        [Test]
        public void DetailedArtHasNativeMaterialsAndNewObstacleIsExcludedFromNavigation()
        {
            Assert.That(CanThoMapSetupTool.ReadLayout().artRevision, Is.InRange(2, 9));
            Assert.NotNull(AssetDatabase.LoadMainAssetAtPath(CanThoDetailSetupTool.ProfilePath));
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(CanThoMapSetupTool.ModelPath);
            var renderers = model.GetComponentsInChildren<MeshRenderer>();
            Assert.IsTrue(renderers.Any(x => x.name.Contains("03_LearningResource")));
            Assert.IsTrue(renderers.Any(x => x.name.Contains("33_Transit")));
            Assert.IsTrue(renderers.All(x => x.sharedMaterials.All(m => m != null && (m.name.StartsWith("CT2_") || m.name.StartsWith("CT3_") || m.name.StartsWith("CT4_") || m.name.StartsWith("CT5_") || m.name.StartsWith("CT9_")) && m.shader.name == "Universal Render Pipeline/Lit")));
            var nav = NavMesh.AddNavMeshData(AssetDatabase.LoadAssetAtPath<NavMeshData>(CanThoMapSetupTool.NavigationPath));
            try { Assert.IsFalse(NavMesh.SamplePosition(new Vector3(75, 0, -25), out _, 0.5f, NavMesh.AllAreas), "Data mast must block AI navigation"); }
            finally { nav.Remove(); }
        }
        [Test]
        public void CurvedLibraryFacadeBlocksPlayersBeforeTheyEnterTheVisibleGlass()
        {
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(CanThoMapSetupTool.ScenePath, OpenSceneMode.Additive);
            try
            {
                Physics.SyncTransforms();
                var body = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<BoxCollider>()).Single(x => x.name == "TRUNG TÂM HỌC LIỆU");
                Assert.IsTrue(body.Raycast(new Ray(new Vector3(57, 1.5f, -42), Vector3.forward), out var hit, 10));
                Assert.That(hit.distance, Is.InRange(3f, 4f), "Collider must include the curved front, not only the old rectangular body");
            }
            finally { EditorSceneManager.CloseScene(scene, true); if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous); }
        }
        [Test]
        public void ApplyingDetailedRevisionAgainPreservesSceneAndNavigation()
        {
            if (Application.isBatchMode && string.IsNullOrEmpty(SceneManager.GetActiveScene().path)) EditorSceneManager.OpenScene(MultiplayerSetupTool.MenuPath);
            var paths = new[] { CanThoMapSetupTool.ScenePath, CanThoMapSetupTool.NavigationPath, CanThoMapSetupTool.MenuPath };
            var before = paths.Select(File.ReadAllBytes).ToArray();
            var active = SceneManager.GetActiveScene();
            CanThoDetailSetupTool.Apply(); CanThoDetailSetupTool.Apply();
            for (int i = 0; i < paths.Length; i++) CollectionAssert.AreEqual(before[i], File.ReadAllBytes(paths[i]));
            Assert.AreEqual(active, SceneManager.GetActiveScene());
        }
    }
}
