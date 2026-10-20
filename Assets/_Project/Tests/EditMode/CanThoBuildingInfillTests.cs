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
    public sealed class CanThoBuildingInfillTests
    {
        [Test]
        public void AddedBuildingsStayOnOriginalGroundAndHaveDistinctColliders()
        {
            var layout = CanThoMapSetupTool.ReadLayout();
            Assert.AreEqual(9, layout.artRevision);
            Assert.AreEqual(1100, layout.infillBuildings.Length);
            Assert.AreEqual(1351, layout.neighborhoodHouses.Length);
            Assert.AreEqual(new Vector3(-90, 10, 30), layout.bounds.center);
            Assert.AreEqual(new Vector3(940, 40, 840), layout.bounds.size);
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(CanThoMapSetupTool.ScenePath, OpenSceneMode.Additive);
            try
            {
                Physics.SyncTransforms();
                var boxes = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<BoxCollider>()).ToArray();
                var ground = boxes.Single(c => c.name == "Campus soil");
                Assert.AreEqual(new Vector3(940, .5f, 840), ground.size);
                Assert.AreEqual(new Vector3(-90, -.25f, 30), ground.transform.position);
                foreach (var house in layout.infillBuildings)
                {
                    var collider = boxes.Single(c => c.name == house.name);
                    Assert.AreEqual(house.size, collider.size);
                    Assert.Less(Quaternion.Angle(Quaternion.Euler(house.euler), collider.transform.rotation), .01f, house.name);
                    foreach (float x in new[] { -.5f, .5f })
                        foreach (float z in new[] { -.5f, .5f })
                        {
                            var corner = house.position + Quaternion.Euler(house.euler) * new Vector3(x * house.size.x, 0, z * house.size.z);
                            Assert.IsTrue(ground.Raycast(new Ray(corner + Vector3.up * 2, Vector3.down), out _, 3), house.name);
                        }
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }

        [Test]
        public void AddedCourtyardsConnectToCampusAndInteriorsStayBlocked()
        {
            var layout = CanThoMapSetupTool.ReadLayout();
            Assert.GreaterOrEqual(layout.infillAccessChecks.Length, 60);
            var nav = NavMesh.AddNavMeshData(AssetDatabase.LoadAssetAtPath<NavMeshData>(CanThoMapSetupTool.NavigationPath));
            try
            {
                Assert.IsTrue(NavMesh.SamplePosition(new Vector3(0, 0, -40), out var start, .5f, NavMesh.AllAreas));
                foreach (var point in layout.infillAccessChecks)
                {
                    Assert.IsTrue(NavMesh.SamplePosition(point, out var target, .6f, NavMesh.AllAreas), point.ToString());
                    var path = new NavMeshPath();
                    Assert.IsTrue(NavMesh.CalculatePath(start.position, target.position, NavMesh.AllAreas, path));
                    Assert.AreEqual(NavMeshPathStatus.PathComplete, path.status, point.ToString());
                }
                foreach (var house in layout.infillBuildings)
                    Assert.IsFalse(NavMesh.SamplePosition(house.position, out _, .5f, NavMesh.AllAreas), house.name);
            }
            finally { nav.Remove(); }
        }

        [Test]
        public void FacadeAtlasHasUVsAndItsNormalTextureImportsCorrectly()
        {
            const string root = "Assets/_Project/Art/Environments/CanThoCampus/Textures/";
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/CanThoCampus/CT9_FacadeAtlas.mat");
            Assert.NotNull(material);
            Assert.AreEqual("Universal Render Pipeline/Lit", material.shader.name);
            Assert.AreEqual(2048, material.GetTexture("_BaseMap").width);
            Assert.NotNull(material.GetTexture("_BumpMap"));
            Assert.IsTrue(material.IsKeywordEnabled("_NORMALMAP"));
            var importer = (TextureImporter)AssetImporter.GetAtPath(root + "CT9_FacadeAtlas_Normal.png");
            Assert.AreEqual(TextureImporterType.NormalMap, importer.textureType);
            Assert.IsFalse(importer.sRGBTexture);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(CanThoMapSetupTool.ModelPath);
            var renderers = model.GetComponentsInChildren<MeshRenderer>().Where(r => r.sharedMaterial == material).ToArray();
            Assert.That(renderers.Length, Is.InRange(1, 4));
            Assert.IsTrue(renderers.All(r => r.GetComponent<MeshFilter>().sharedMesh.uv.Any(uv => uv != Vector2.zero)));
        }
    }
}
