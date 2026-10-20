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
    public sealed class CanThoArchitectureTests
    {
        [Test]
        public void TiledSurfacesHaveImportedColorAndLinearNormalMapsWithMetricUVs()
        {
            var layout = CanThoMapSetupTool.ReadLayout();
            Assert.That(layout.artRevision, Is.InRange(5, 9));
            var textured = layout.materials.Where(x => !string.IsNullOrEmpty(x.normalMap)).ToArray();
            Assert.That(textured.Length, Is.GreaterThanOrEqualTo(8));
            foreach (var entry in textured)
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/CanThoCampus/" + entry.name + ".mat");
                Assert.NotNull(material.GetTexture("_BaseMap"), entry.name);
                Assert.NotNull(material.GetTexture("_BumpMap"), entry.name);
                Assert.IsTrue(material.IsKeywordEnabled("_NORMALMAP"), entry.name);
                var normal = (TextureImporter)AssetImporter.GetAtPath(CanThoMapSetupTool.ArtRoot + "/" + entry.normalMap);
                Assert.AreEqual(TextureImporterType.NormalMap, normal.textureType);
                Assert.IsFalse(normal.sRGBTexture);
                Assert.IsTrue(((TextureImporter)AssetImporter.GetAtPath(CanThoMapSetupTool.ArtRoot + "/" + entry.baseMap)).sRGBTexture);
            }
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(CanThoMapSetupTool.ModelPath);
            var wall = model.GetComponentsInChildren<MeshFilter>().First(x => x.name.Contains("02_Administration") && x.name.Contains("CT5_Plaster"));
            Assert.AreEqual(wall.sharedMesh.vertexCount, wall.sharedMesh.uv.Length);
            Assert.Greater(wall.sharedMesh.uv.Max(x => x.y) - wall.sharedMesh.uv.Min(x => x.y), 5,
                "Large walls need repeated metre-scaled UVs, not one stretched tile");
        }

        [Test]
        public void LeafClustersUseCutoutDoubleSidedGeometryRatherThanOpaqueRectangles()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/CanThoCampus/CT5_LeafCluster.mat");
            Assert.AreEqual(1, material.GetFloat("_AlphaClip"));
            Assert.AreEqual(0, material.GetFloat("_Cull"));
            Assert.IsTrue(material.IsKeywordEnabled("_ALPHATEST_ON"));
            var texture = material.GetTexture("_BaseMap") as Texture2D;
            Assert.NotNull(texture);
            Assert.IsTrue(((TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture))).DoesSourceTextureHaveAlpha());
        }

        [Test]
        public void PhotoBasedGatesStayLowAndKeepTheThreeOpenPassages()
        {
            WithCampus(scene =>
            {
                var boxes = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<BoxCollider>()).ToArray();
                Assert.IsFalse(boxes.Any(x => x.name == "Gate lintel"), "Real entrance is open overhead");
                foreach (var gate in CanThoMapSetupTool.ReadLayout().gates)
                {
                    var pylons = boxes.Where(x => x.name == "Gate " + gate.id + " pylon").ToArray();
                    Assert.AreEqual(2, pylons.Length);
                    Assert.IsTrue(pylons.All(x => x.bounds.max.y <= 3.5f));
                    Assert.IsFalse(Physics.Raycast(gate.position + Vector3.up * 1.4f - Vector3.forward * 4,
                        Vector3.forward, 8, 1, QueryTriggerInteraction.Ignore), "Gate obstructed: " + gate.id);
                }
            });
        }

        [Test]
        public void TurtleHallAndResearchEntrancesRemainReachableWithNewArchitecture()
        {
            var nav = NavMesh.AddNavMeshData(AssetDatabase.LoadAssetAtPath<NavMeshData>(CanThoMapSetupTool.NavigationPath));
            try
            {
                Assert.IsTrue(NavMesh.SamplePosition(new Vector3(0, 0, -40), out var start, .5f, NavMesh.AllAreas));
                foreach (string id in new[] { "24_TurtleAuditorium", "17_Research_RLC", "07_ICT" })
                {
                    var point = CanThoMapSetupTool.ReadLayout().landmarks.Single(x => x.id == id).approach;
                    Assert.IsTrue(NavMesh.SamplePosition(point, out var target, .5f, NavMesh.AllAreas), id);
                    var path = new NavMeshPath(); NavMesh.CalculatePath(start.position, target.position, NavMesh.AllAreas, path);
                    Assert.AreEqual(NavMeshPathStatus.PathComplete, path.status, id);
                }
            }
            finally { nav.Remove(); }
        }

        private static void WithCampus(System.Action<Scene> check)
        {
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(CanThoMapSetupTool.ScenePath, OpenSceneMode.Additive);
            try { Physics.SyncTransforms(); check(scene); }
            finally { EditorSceneManager.CloseScene(scene, true); if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous); }
        }
    }
}
