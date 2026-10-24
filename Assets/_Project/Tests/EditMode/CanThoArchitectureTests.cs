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
            Assert.That(layout.artRevision, Is.InRange(5, 12));
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

        [Test]
        public void EntireTurtleHallIncludingRoofKeepsCampusRoadsAndSidewalksClear()
        {
            WithCampus(scene =>
            {
                var renderers = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<MeshRenderer>())
                    .Where(x => x.name.Contains("24_TurtleAuditorium")).ToArray();
                Assert.That(renderers.Length, Is.GreaterThan(0));
                var footprint = renderers[0].bounds;
                foreach (var renderer in renderers) footprint.Encapsulate(renderer.bounds);
                var colliders = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<BoxCollider>())
                    .Where(x => x.name == "Turtle auditorium body" || x.name.StartsWith("Turtle Hall ")).ToArray();
                Assert.That(colliders.Length, Is.EqualTo(2), "Imported hall body and roof proxies replace the old porch columns");
                foreach (var road in CanThoMapSetupTool.ReadLayout().roads)
                {
                    int steps = Mathf.CeilToInt(Vector3.Distance(road.a, road.b) * 4);
                    for (int i = 0; i <= steps; i++)
                    {
                        var point = Vector3.Lerp(road.a, road.b, (float)i / steps);
                        Assert.That(FlatDistance(footprint, point), Is.GreaterThanOrEqualTo(road.width * .5f + 3),
                            "Hall roof must leave the 2 m sidewalk plus 1 m setback: " + road.name);
                        foreach (var collider in colliders)
                            Assert.That(FlatDistance(collider.bounds, point), Is.GreaterThanOrEqualTo(road.width * .5f + 2),
                                "Hall collider obstructs sidewalk: " + road.name);
                    }
                }
                Assert.That(FlatDistance(footprint, new Vector3(-22, 0, 22)), Is.GreaterThanOrEqualTo(9.7f),
                    "Hall roof must clear the existing roundabout");
            });
        }

        [Test]
        public void EducationCrescentLaneBesideTurtleHallHasContinuousNavigation()
        {
            var nav = NavMesh.AddNavMeshData(AssetDatabase.LoadAssetAtPath<NavMeshData>(CanThoMapSetupTool.NavigationPath));
            try
            {
                var road = CanThoMapSetupTool.ReadLayout().roads.Single(x => x.name == "Education crescent" && x.a.x == -51.52f);
                var direction = (road.b - road.a).normalized;
                var inward = new Vector3(-direction.z, 0, direction.x);
                Vector3 previous = default;
                for (int i = 0; i <= 20; i++)
                {
                    var point = Vector3.Lerp(road.a, road.b, i / 20f) + inward * 2.2f;
                    Assert.IsTrue(NavMesh.SamplePosition(point, out var hit, .3f, NavMesh.AllAreas), "Lane blocked at " + point);
                    if (i > 0)
                    {
                        var path = new NavMeshPath();
                        Assert.IsTrue(NavMesh.CalculatePath(previous, hit.position, NavMesh.AllAreas, path));
                        Assert.That(path.status, Is.EqualTo(NavMeshPathStatus.PathComplete));
                        float length = 0;
                        for (int j = 1; j < path.corners.Length; j++) length += Vector3.Distance(path.corners[j - 1], path.corners[j]);
                        Assert.That(length, Is.LessThanOrEqualTo(Vector3.Distance(previous, hit.position) + .5f));
                    }
                    previous = hit.position;
                }
            }
            finally { nav.Remove(); }
        }

        [Test]
        public void TreeTrunksStayOutsideAllRoadLanesExceptPlantedRoundaboutIslands()
        {
            var layout = CanThoMapSetupTool.ReadLayout();
            var segments = layout.roads.Select(x => (x.a, x.b, x.width)).ToList();
            foreach (var road in layout.peripheralRoads.Concat(layout.localLanes))
                for (int i = 1; i < road.points.Length; i++) segments.Add((road.points[i - 1], road.points[i], road.width));
            WithCampus(scene =>
            {
                foreach (var trunk in scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<BoxCollider>())
                    .Where(x => x.name.Contains("trunk")))
                {
                    var point = trunk.bounds.center; point.y = 0;
                    if (Vector3.Distance(point, new Vector3(-22, 0, 22)) < 3.6f ||
                        Vector3.Distance(point, new Vector3(64.32f, 0, 27.16f)) < 2.1f) continue;
                    foreach (var (a, b, width) in segments)
                    {
                        var line = b - a;
                        var t = Mathf.Clamp01(Vector3.Dot(point - a, line) / line.sqrMagnitude);
                        float clearance = Vector3.Distance(point, a + line * t) - width * .5f
                            - Mathf.Max(trunk.bounds.size.x, trunk.bounds.size.z) * .5f;
                        Assert.That(clearance, Is.GreaterThanOrEqualTo(.99f), trunk.name + " obstructs road near " + point);
                    }
                }
            });
        }

        private static float FlatDistance(Bounds bounds, Vector3 point)
        {
            point.y = bounds.center.y;
            return Vector3.Distance(bounds.ClosestPoint(point), point);
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
