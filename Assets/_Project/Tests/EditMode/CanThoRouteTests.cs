using System.Linq;
using LastProtocol.Editor;
using NUnit.Framework;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace LastProtocol.Tests
{
    public sealed class CanThoRouteTests
    {
        [Test]
        public void RouteConnectsTheGateAdministrationAndLibraryWithReasonablePaths()
        {
            var layout = CanThoMapSetupTool.ReadLayout();
            Assert.That(layout.artRevision, Is.InRange(3, 10));
            Assert.That(layout.playableRoute.Length, Is.GreaterThanOrEqualTo(6));
            var nav = NavMesh.AddNavMeshData(AssetDatabase.LoadAssetAtPath<NavMeshData>(CanThoMapSetupTool.NavigationPath));
            try
            {
                for (int i = 1; i < layout.playableRoute.Length; i++)
                {
                    Assert.IsTrue(NavMesh.SamplePosition(layout.playableRoute[i-1], out var a, .5f, NavMesh.AllAreas));
                    Assert.IsTrue(NavMesh.SamplePosition(layout.playableRoute[i], out var b, .5f, NavMesh.AllAreas));
                    var path = new NavMeshPath();
                    Assert.IsTrue(NavMesh.CalculatePath(a.position, b.position, NavMesh.AllAreas, path));
                    Assert.AreEqual(NavMeshPathStatus.PathComplete, path.status, "Route leg " + i);
                    float distance = 0;
                    for (int j = 1; j < path.corners.Length; j++) distance += Vector3.Distance(path.corners[j-1], path.corners[j]);
                    Assert.LessOrEqual(distance, Vector3.Distance(a.position, b.position) * 1.5f + 3, "Unexpected long detour on leg " + i);
                }
            }
            finally { nav.Remove(); }
        }

        [Test]
        public void PlayerSizedControllerCanWalkTheEntireRouteWithoutEnteringScenery()
        {
            WithCampus(scene =>
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MultiplayerSetupTool.PlayerPath).GetComponent<CharacterController>();
                var go = new GameObject("Route collision test capsule");
                try
                {
                    go.transform.position = CanThoMapSetupTool.ReadLayout().playableRoute[0] + Vector3.up * (prefab.height * .5f - prefab.center.y + .05f);
                    var controller = go.AddComponent<CharacterController>();
                    controller.height = prefab.height; controller.radius = prefab.radius; controller.center = prefab.center;
                    controller.stepOffset = prefab.stepOffset; controller.skinWidth = prefab.skinWidth;
                    controller.slopeLimit = prefab.slopeLimit; controller.minMoveDistance = prefab.minMoveDistance;
                    foreach (var point in CanThoMapSetupTool.ReadLayout().playableRoute.Skip(1))
                    {
                        for (int step = 0; step < 600; step++)
                        {
                            var direction = point - go.transform.position; direction.y = 0;
                            if (direction.magnitude < .12f) break;
                            controller.Move(Vector3.ClampMagnitude(direction, .12f) + Vector3.down * .1f);
                        }
                        var remaining = point - go.transform.position; remaining.y = 0;
                        Assert.Less(remaining.magnitude, .2f, "Controller stuck before " + point + "; actual=" + go.transform.position);
                        Assert.That(go.transform.position.y + controller.center.y - controller.height * .5f, Is.InRange(-.15f, .5f), "Route must have continuous support");
                    }
                }
                finally { Object.DestroyImmediate(go); }
            });
        }

        [Test]
        public void CinemachinePullsInBeforeAdministrationLibraryAndFence()
        {
            WithCampus(scene =>
            {
                var rig = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<CinemachineCamera>()).Single();
                var follow = rig.GetComponent<CinemachineThirdPersonFollow>();
                Assert.IsTrue(follow.AvoidObstacles.Enabled);
                Assert.AreEqual(.28f, follow.AvoidObstacles.CameraRadius, .001f);
                var target = new GameObject("Route camera probe"); var previous = rig.Follow;
                try
                {
                    rig.Follow = target.transform;
                    foreach (var probe in new[] { new Vector4(32,1.6f,-15,180), new Vector4(57,1.6f,-40,180), new Vector4(35,1.6f,-47,0) })
                    {
                        target.transform.SetPositionAndRotation(new Vector3(probe.x,probe.y,probe.z), Quaternion.Euler(0,probe.w,0));
                        rig.PreviousStateIsValid = false; Physics.SyncTransforms();
                        rig.InternalUpdateCameraState(Vector3.up, -1);
                        var position = rig.State.GetFinalPosition();
                        Assert.IsNotNull(follow.CurrentObstacle, "Probe must actually encounter a wall: " + probe);
                        Assert.IsFalse(Physics.CheckSphere(position, .2f, 1, QueryTriggerInteraction.Ignore), "Camera inside scenery at " + position);
                        Assert.Less(Vector3.Distance(position, target.transform.position), follow.CameraDistance, "Camera should pull closer");
                    }
                }
                finally { rig.Follow = previous; Object.DestroyImmediate(target); }
            });
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
