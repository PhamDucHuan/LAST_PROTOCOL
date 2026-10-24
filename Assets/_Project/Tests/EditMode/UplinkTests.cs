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
    public sealed class UplinkTests
    {
        private MissionDefinition Definition => AssetDatabase.LoadAssetAtPath<MissionDefinition>(UplinkSetupTool.DefinitionPath);
        private MissionSnapshot DataPhase()
        {
            var state = MissionSnapshot.Begin(Definition, 1);
            for (int i = 0; i < 20; i++) state = MissionRules.Observe(Definition, state, 2, 2, .1f);
            Assert.AreEqual(1, state.ObjectiveIndex); return state;
        }
        [Test] public void WrongPhaseUnknownAndDuplicateDevicesCannotAdvanceOrReplicateCredit()
        {
            var definition = Definition; Assert.IsTrue(definition.Validate(out _));
            var state = MissionSnapshot.Begin(definition, 1);
            Assert.AreEqual(state, MissionRules.CompleteStation(definition, state, DataStation.AdministrationId));
            state = DataPhase();
            foreach (string id in new[] { "MISSING", DataStation.GeneratorId, DataStation.UplinkId })
                Assert.AreEqual(state, MissionRules.CompleteStation(definition, state, id));
            state = MissionRules.CompleteStation(definition, state, UplinkSetupTool.Ids[2]);
            Assert.AreEqual(1, MissionRules.Collected(definition, state, 1));
            Assert.AreEqual(state, MissionRules.CompleteStation(definition, state, UplinkSetupTool.Ids[2]));
            Assert.AreEqual(state, MissionRules.Observe(definition, state, 2, 2, 30));
            state = MissionRules.CompleteStation(definition, state, UplinkSetupTool.Ids[0]);
            Assert.AreEqual(1, state.ObjectiveIndex);
            state = MissionRules.CompleteStation(definition, state, UplinkSetupTool.Ids[1]);
            Assert.AreEqual(2, state.ObjectiveIndex);
            Assert.AreEqual(state, MissionRules.CompleteStation(definition, state, DataStation.UplinkId));
            state = MissionRules.CompleteStation(definition, state, DataStation.GeneratorId);
            Assert.AreEqual(3, state.ObjectiveIndex);
            state = MissionRules.CompleteStation(definition, state, DataStation.UplinkId);
            Assert.AreEqual(31, state.CompletedStations); Assert.AreEqual(MissionPhase.Completed, state.Phase);
            foreach (string id in UplinkSetupTool.Ids) Assert.AreEqual(state, MissionRules.CompleteStation(definition, state, id));
        }
        [Test] public void FailureAndRestartKeepLinearDependenciesAndClearCollectedIds()
        {
            var state = DataPhase(); state = MissionRules.CompleteStation(Definition, state, DataStation.AdministrationId);
            Assert.AreEqual(ObjectiveStatus.Completed, MissionRules.Status(state, 0));
            Assert.AreEqual(ObjectiveStatus.Available, MissionRules.Status(state, 1));
            Assert.AreEqual(ObjectiveStatus.Active, MissionRules.Status(state, 1, true));
            Assert.AreEqual(ObjectiveStatus.Locked, MissionRules.Status(state, 2));
            state = MissionRules.Fail(state, MissionFailure.TeamWiped);
            Assert.AreEqual(ObjectiveStatus.Failed, MissionRules.Status(state, 1));
            Assert.AreEqual(state, MissionRules.CompleteStation(Definition, state, UplinkSetupTool.Ids[1]));
            state = MissionSnapshot.Begin(Definition, state.Attempt + 1);
            Assert.AreEqual(0, state.CompletedStations); Assert.AreEqual(0, state.ObjectiveIndex); Assert.AreEqual(2, state.Attempt);
            Assert.IsFalse(MissionRules.CanUseStation(Definition, state, DataStation.GeneratorId));
            state.SchemaVersion = 999;
            Assert.AreEqual(state, MissionRules.Observe(Definition, state, 2, 2, 30));
        }
        [Test] public void NewSchemaRejectsDuplicateDeviceIdsAcrossObjectivesAndPreservesLegacyRecon()
        {
            var copy = Object.Instantiate(Definition);
            try
            {
                copy.objectives[2].stationIds[0] = DataStation.AdministrationId;
                Assert.IsFalse(copy.Validate(out _));
                copy.objectives[2].stationIds[0] = DataStation.GeneratorId;
                copy.schemaVersion = 0; Assert.IsFalse(copy.Validate(out _));
                var recon = AssetDatabase.LoadAssetAtPath<MissionDefinition>(MissionSetupTool.DefinitionPath);
                Assert.IsTrue(recon.Validate(out _)); Assert.AreEqual(0, recon.schemaVersion);
                Assert.IsTrue(recon.objectives.All(x => x.kind == ObjectiveKind.Rendezvous));
            }
            finally { Object.DestroyImmediate(copy); }
        }
        [Test] public void FiveDevicesHaveUniqueSceneNetworkIdsAndReachableClearApproaches()
        {
            var scene = EditorSceneManager.OpenScene(CanThoMapSetupTool.ScenePath, OpenSceneMode.Additive);
            var nav = NavMesh.AddNavMeshData(AssetDatabase.LoadAssetAtPath<NavMeshData>(CanThoMapSetupTool.NavigationPath));
            try
            {
                var nodes = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<DataStation>()).ToArray();
                Assert.AreEqual(5, nodes.Length); Assert.AreEqual(5, nodes.Select(x => x.stationId).Distinct().Count());
                Assert.AreEqual(5, nodes.Select(x => new SerializedObject(x.GetComponent<Unity.Netcode.NetworkObject>()).FindProperty("GlobalObjectIdHash").longValue).Distinct().Count());
                Assert.IsTrue(NavMesh.SamplePosition(new Vector3(0,0,-40), out var start, .3f, NavMesh.AllAreas));
                foreach (var node in nodes)
                {
                    Assert.IsTrue(Definition.StationIndex(node.stationId) >= 0);
                    Assert.AreEqual(1, node.GetComponentsInChildren<Collider>().Length);
                    Assert.IsTrue(NavMesh.SamplePosition(node.transform.position + Vector3.back * 2, out var approach, .3f, NavMesh.AllAreas), node.stationId);
                    var path = new NavMeshPath(); NavMesh.CalculatePath(start.position, approach.position, NavMesh.AllAreas, path);
                    Assert.AreEqual(NavMeshPathStatus.PathComplete, path.status, node.stationId);
                    foreach (var road in CanThoMapSetupTool.ReadLayout().roads)
                    {
                        Vector2 a = new(road.a.x, road.a.z), b = new(road.b.x, road.b.z), p = new(node.transform.position.x,node.transform.position.z);
                        Vector2 v = b-a; float t = Mathf.Clamp01(Vector2.Dot(p-a,v)/v.sqrMagnitude);
                        Assert.Greater(Vector2.Distance(p,a+t*v), road.width*.5f + .8f, node.stationId + " encroaches road " + road.name);
                    }
                }
            }
            finally { nav.Remove(); EditorSceneManager.CloseScene(scene,true); }
        }
    }
}
