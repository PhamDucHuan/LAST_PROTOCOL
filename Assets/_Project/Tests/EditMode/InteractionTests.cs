using System.IO;
using System.Linq;
using LastProtocol.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace LastProtocol.Tests
{
    public sealed class InteractionTests
    {
        [Test]
        public void OneGestureCannotSwitchFromReviveToTerminalOrAnotherTarget()
        {
            var gate = new InteractionGate(); Assert.IsTrue(gate.TryHold(InteractionKind.Revive, 4));
            Assert.IsTrue(gate.TryHold(InteractionKind.Revive, 4));
            Assert.IsFalse(gate.TryHold(InteractionKind.DataStation, 5));
            Assert.IsFalse(gate.TryHold(InteractionKind.Revive, 4));
            gate.Release(); Assert.IsTrue(gate.TryHold(InteractionKind.DataStation, 5));
            Assert.IsFalse(gate.TryHold(InteractionKind.DataStation, 6));
        }
        [Test]
        public void CompletionOrInterruptionRequiresReleaseBeforeNewAction()
        {
            var gate = new InteractionGate(); Assert.IsFalse(gate.TryHold(InteractionKind.None, 1));
            Assert.IsTrue(gate.TryHold(InteractionKind.DataStation, 10)); gate.Stop(true);
            Assert.IsFalse(gate.TryHold(InteractionKind.DataStation, 10));
            Assert.IsFalse(gate.TryHold(InteractionKind.Revive, 11));
            gate.Release(); Assert.IsTrue(gate.TryHold(InteractionKind.Revive, 11));
        }
        [Test]
        public void StationIsUniqueReachableAndKeepsThePlayableRouteOpen()
        {
            var scene = EditorSceneManager.OpenScene(CanThoMapSetupTool.ScenePath, OpenSceneMode.Additive);
            try
            {
                var stations = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<DataStation>()).ToArray();
                Assert.That(stations.Count(x => x.stationId == DataStation.AdministrationId), Is.EqualTo(1));
                var station = stations.Single(x => x.stationId == DataStation.AdministrationId);
                Assert.That(station.stationId, Is.EqualTo(DataStation.AdministrationId));
                Assert.That(station.transform.position, Is.EqualTo(DataStationSetupTool.Position));
                Assert.That(station.holdSeconds, Is.EqualTo(3));
                Assert.That(station.GetComponentsInChildren<Collider>().Length, Is.EqualTo(1));
                var nav = NavMesh.AddNavMeshData(AssetDatabase.LoadAssetAtPath<NavMeshData>(CanThoMapSetupTool.NavigationPath));
                try
                {
                    Assert.IsTrue(NavMesh.SamplePosition(new Vector3(30, 0, -26), out var approach, .3f, NavMesh.AllAreas));
                    Assert.IsTrue(NavMesh.SamplePosition(new Vector3(0, 0, -40), out var start, .3f, NavMesh.AllAreas));
                    var path = new NavMeshPath(); NavMesh.CalculatePath(start.position, approach.position, NavMesh.AllAreas, path);
                    Assert.That(path.status, Is.EqualTo(NavMeshPathStatus.PathComplete));
                }
                finally { nav.Remove(); }
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MultiplayerSetupTool.PlayerPath);
                Assert.That(prefab.GetComponents<PlayerInteraction>().Length, Is.EqualTo(1));
                Assert.That(prefab.GetComponents<PlayerRevive>().Length, Is.EqualTo(1));
                Assert.That(prefab.GetComponents<ReviveHUD>().Length, Is.EqualTo(1));
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
