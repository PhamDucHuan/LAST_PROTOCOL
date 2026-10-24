using System.Linq;
using LastProtocol.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace LastProtocol.Tests
{
    public sealed class CanThoModernHouseTests
    {
        [Test]
        public void ReplacementArtUsesAllTwentyOneVariantsAndSharedUrpSurfaces()
        {
            var layout = CanThoMapSetupTool.ReadLayout();
            Assert.AreEqual(13, layout.artRevision);
            Assert.GreaterOrEqual(layout.importedModernHouses.Length, 247);
            CollectionAssert.AreEquivalent(Enumerable.Range(1, 20).Select(i => "ModernHouse" + i.ToString("00")).Concat(new[] { "TownHouse" }), layout.importedModernHouses.Select(h => h.family).Distinct());
            Assert.AreEqual(1351, layout.neighborhoodHouses.Length);
            Assert.IsFalse(layout.collisions.Any(c => c.name.StartsWith("Awning support")), "Superseded awnings must not leave invisible obstacles.");
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(CanThoMapSetupTool.ModelPath);
            var renderers = model.GetComponentsInChildren<MeshRenderer>().Where(r => r.name.StartsWith("69_ModernHouses_")).ToArray();
            Assert.That(renderers.Length, Is.InRange(4, 8));
            var surfaces = renderers.SelectMany(r => r.sharedMaterials).Distinct().ToArray();
            Assert.AreEqual(2, surfaces.Length);
            Assert.IsTrue(surfaces.All(m => m.shader.name == "Universal Render Pipeline/Lit" && m.GetTexture("_BaseMap") != null));
            Assert.IsTrue(renderers.All(r => r.GetComponent<MeshFilter>().sharedMesh.uv.Any(uv => uv != Vector2.zero)));
            Assert.IsTrue(surfaces.Single(m => m.name == "CT11_TownHouse").IsKeywordEnabled("_NORMALMAP"));
        }

        [Test]
        public void AllReplacementHouseFrontagesHaveCompleteNavigationPaths()
        {
            var layout = CanThoMapSetupTool.ReadLayout();
            var nav = NavMesh.AddNavMeshData(AssetDatabase.LoadAssetAtPath<NavMeshData>(CanThoMapSetupTool.NavigationPath));
            try
            {
                Assert.IsTrue(NavMesh.SamplePosition(new Vector3(0, 0, -40), out var start, .5f, NavMesh.AllAreas));
                foreach (var h in layout.importedModernHouses)
                {
                    Assert.IsFalse(NavMesh.SamplePosition(h.position, out _, .4f, NavMesh.AllAreas), h.name + " interior");
                    var point = h.position + h.front * (h.size.z * .5f + 2);
                    Assert.IsTrue(NavMesh.SamplePosition(point, out var target, 2, NavMesh.AllAreas), h.name);
                    var path = new NavMeshPath();
                    Assert.IsTrue(NavMesh.CalculatePath(start.position, target.position, NavMesh.AllAreas, path), h.name);
                    Assert.AreEqual(NavMeshPathStatus.PathComplete, path.status, h.name);
                }
            }
            finally { nav.Remove(); }
        }
    }
}
