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
    public sealed class CanThoVietnamAssetTests
    {
        [Test]
        public void AllFourSourceFamiliesAreVisibleWithPortableUrpSurfaces()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(CanThoMapSetupTool.ModelPath);
            var renderers = model.GetComponentsInChildren<MeshRenderer>().Where(r => r.name.StartsWith("68_VietnamImported")).ToArray();
            Assert.Greater(renderers.Length, 8);
            foreach (string family in new[] { "HoiAn", "Asian", "Pho", "PolygonTree" })
            {
                var materials = renderers.SelectMany(r => r.sharedMaterials).Where(m => m.name.StartsWith("CT10_" + family + "_")).ToArray();
                Assert.IsNotEmpty(materials, family);
                Assert.IsTrue(materials.All(m => m.shader.name == "Universal Render Pipeline/Lit"));
                Assert.IsTrue(materials.Any(m => m.GetTexture("_BaseMap") != null), family);
            }
            Assert.Less(model.GetComponentsInChildren<MeshRenderer>().Length, 500);
            Assert.Less(model.GetComponentsInChildren<MeshFilter>().Sum(f => (long)f.sharedMesh.triangles.Length / 3), 600000);
        }

        [Test]
        public void ImportedBuildingsRestOnExistingLandAndHaveReachableFrontages()
        {
            var layout = CanThoMapSetupTool.ReadLayout();
            Assert.AreEqual(12, layout.artRevision);
            Assert.AreEqual(new Vector3(940, 40, 840), layout.bounds.size);
            var houses = layout.importedVietnamAssets.Where(x => !string.IsNullOrEmpty(x.replaces)).ToArray();
            Assert.AreEqual(4, houses.Length);
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(CanThoMapSetupTool.ScenePath, OpenSceneMode.Additive);
            var nav = NavMesh.AddNavMeshData(AssetDatabase.LoadAssetAtPath<NavMeshData>(CanThoMapSetupTool.NavigationPath));
            try
            {
                Physics.SyncTransforms();
                var boxes = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<BoxCollider>()).ToArray();
                var soil = boxes.Single(x => x.name == "Campus soil");
                Assert.IsTrue(NavMesh.SamplePosition(new Vector3(0, 0, -40), out var start, .5f, NavMesh.AllAreas));
                foreach (var house in houses)
                {
                    var box = boxes.Single(x => x.name == house.replaces);
                    Assert.AreEqual(house.size, box.size);
                    Assert.IsTrue(soil.Raycast(new Ray(house.position + Vector3.up * 2, Vector3.down), out _, 3));
                    Assert.IsFalse(NavMesh.SamplePosition(house.position, out _, .4f, NavMesh.AllAreas), house.name);
                    var front = house.position + house.front * (house.size.z * .5f + 2);
                    Assert.IsTrue(NavMesh.SamplePosition(front, out var target, 2f, NavMesh.AllAreas), house.name);
                    var path = new NavMeshPath();
                    Assert.IsTrue(NavMesh.CalculatePath(start.position, target.position, NavMesh.AllAreas, path));
                    Assert.AreEqual(NavMeshPathStatus.PathComplete, path.status, house.name);
                }
            }
            finally
            {
                nav.Remove(); EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }
    }
}
