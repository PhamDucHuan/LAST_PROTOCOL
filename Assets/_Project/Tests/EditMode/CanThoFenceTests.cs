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
    public sealed class CanThoFenceTests
    {
        [Test]
        public void VisiblePerimeterAndContinuousCollidersCoverAllSevenSpans()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(CanThoMapSetupTool.ModelPath);
            Assert.IsTrue(model.GetComponentsInChildren<MeshRenderer>().Any(x => x.name.Contains("35_CampusFence")));
            WithCampus(scene =>
            {
                var fences = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<BoxCollider>())
                    .Where(x => x.name.StartsWith("Campus fence ")).ToArray();
                Assert.AreEqual(7, fences.Length);
                // Sample the whole span, including each end, at player torso height.
                foreach (var fence in fences)
                {
                    var bounds = fence.bounds;
                    bool alongX = bounds.size.x > bounds.size.z;
                    var normal = alongX ? Vector3.forward : Vector3.right;
                    for (int i = 0; i <= 20; i++)
                    {
                        var point = bounds.center;
                        point.y = 1.5f;
                        if (alongX) point.x = Mathf.Lerp(bounds.min.x + 0.01f, bounds.max.x - 0.01f, i / 20f);
                        else point.z = Mathf.Lerp(bounds.min.z + 0.01f, bounds.max.z - 0.01f, i / 20f);
                        Assert.IsTrue(fence.Raycast(new Ray(point - normal * 3, normal), out _, 6), fence.name);
                    }
                    Assert.GreaterOrEqual(bounds.max.y, 3.5f, "Fence must be taller than the player jump");
                }
            });
        }

        [Test]
        public void ThreeGateOpeningsAndTheExteriorStreetRemainClear()
        {
            WithCampus(scene =>
            {
                var colliders = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<BoxCollider>()).ToArray();
                foreach (float gate in new[] { -77f, 0f, 77f })
                    foreach (float offset in new[] { -5f, 0f, 5f })
                        Assert.IsFalse(colliders.Any(x => x.Raycast(new Ray(new Vector3(gate + offset, 1.4f, -53), Vector3.forward), out _, 9)), "Obstructed gate " + gate);
                var fences = colliders.Where(x => x.name.StartsWith("Campus fence "));
                Assert.IsFalse(fences.Any(x => x.Raycast(new Ray(new Vector3(-290, 1.5f, -64), Vector3.right), out _, 550)), "Fence must not cross the exterior street");
            });
        }

        [Test]
        public void NavigationUsesOpenGatesAndCannotCrossSolidFrontage()
        {
            var nav = NavMesh.AddNavMeshData(AssetDatabase.LoadAssetAtPath<NavMeshData>(CanThoMapSetupTool.NavigationPath));
            try
            {
                foreach (float gate in new[] { -77f, 0f, 77f })
                {
                    Assert.IsTrue(NavMesh.SamplePosition(new Vector3(gate, 0, -54), out var outside, 0.5f, NavMesh.AllAreas));
                    Assert.IsTrue(NavMesh.SamplePosition(new Vector3(gate, 0, -43), out var inside, 0.5f, NavMesh.AllAreas));
                    var path = new NavMeshPath();
                    Assert.IsTrue(NavMesh.CalculatePath(outside.position, inside.position, NavMesh.AllAreas, path));
                    string route = string.Join(" -> ", path.corners.Select(x => x.ToString()));
                    Assert.AreEqual(NavMeshPathStatus.PathComplete, path.status, "Gate route " + gate + ": " + route);
                    Assert.IsTrue(path.corners.All(x => Mathf.Abs(x.x - gate) < 7), "Route must pass through this gate opening, not detour through another gate: " + route);
                    Debug.Log("Campus gate " + gate + " navigation route: " + route);
                }
                foreach (float x in new[] { -150f, -40f, 35f, 150f })
                {
                    Assert.IsTrue(NavMesh.SamplePosition(new Vector3(x, 0, -54), out var outside, 0.5f, NavMesh.AllAreas));
                    Assert.IsTrue(NavMesh.SamplePosition(new Vector3(x, 0, -44), out var inside, 0.5f, NavMesh.AllAreas));
                    Assert.IsTrue(NavMesh.Raycast(outside.position, inside.position, out _, NavMesh.AllAreas), "AI must not walk through fence " + x);
                }
            }
            finally { nav.Remove(); }
        }

        private static void WithCampus(System.Action<Scene> check)
        {
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(CanThoMapSetupTool.ScenePath, OpenSceneMode.Additive);
            try { Physics.SyncTransforms(); check(scene); }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }
    }
}
