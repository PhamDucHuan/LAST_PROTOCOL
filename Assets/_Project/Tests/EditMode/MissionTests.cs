using System.IO;
using LastProtocol.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace LastProtocol.Tests
{
    public sealed class MissionTests
    {
        private MissionDefinition definition;
        [SetUp] public void SetUp()
        {
            definition = ScriptableObject.CreateInstance<MissionDefinition>();
            definition.missionId = "TEST_RECON"; definition.title = "Test Recon";
            definition.objectives = new[]
            {
                new RendezvousObjective { id = "GATE", title = "Gate", center = Vector3.zero, radius = 6, holdSeconds = 2 },
                new RendezvousObjective { id = "LIBRARY", title = "Library", center = Vector3.right * 10, radius = 6, holdSeconds = 2 }
            };
        }
        [TearDown] public void TearDown() => Object.DestroyImmediate(definition);
        [Test] public void DefinitionRejectsDuplicateIdsAndMalformedGeometry()
        {
            Assert.IsTrue(definition.Validate(out _));
            definition.objectives[1].id = "GATE"; Assert.IsFalse(definition.Validate(out _));
            definition.objectives[1].id = "LIBRARY";
            definition.objectives[0].center = new Vector3(float.NaN, 0, 0); Assert.IsFalse(definition.Validate(out _));
            definition.objectives[0].center = Vector3.zero;
            definition.objectives[0].radius = float.PositiveInfinity; Assert.IsFalse(definition.Validate(out _));
        }
        [TestCase("lowercase")]
        [TestCase("")]
        [TestCase("MISSION WITH SPACES")]
        public void DefinitionRequiresStableBoundedWireIds(string id)
        { definition.missionId = id; Assert.IsFalse(definition.Validate(out _)); }
        [Test] public void DefinitionRejectsAbsentObjectivesAndInvalidHoldTimes()
        {
            definition.objectives[0].holdSeconds = 0; Assert.IsFalse(definition.Validate(out _));
            definition.objectives[0].holdSeconds = 31; Assert.IsFalse(definition.Validate(out _));
            definition.objectives = null; Assert.IsFalse(definition.Validate(out _));
        }
        [Test] public void RallyMustIncludeEntireSquadContinuously()
        {
            var state = MissionSnapshot.Begin(definition, 1);
            for (int i = 0; i < 40; i++) state = MissionRules.Observe(definition, state, 3, 4, 0.1f);
            Assert.AreEqual(0, state.ObjectiveIndex); Assert.AreEqual(0, state.HeldSeconds);
            for (int i = 0; i < 10; i++) state = MissionRules.Observe(definition, state, 4, 4, 0.1f);
            Assert.That(state.HeldSeconds, Is.EqualTo(1).Within(0.001f));
            state = MissionRules.Observe(definition, state, 3, 4, 0.1f);
            Assert.AreEqual(0, state.HeldSeconds);
            for (int i = 0; i < 20; i++) state = MissionRules.Observe(definition, state, 4, 4, 0.1f);
            Assert.AreEqual(1, state.ObjectiveIndex); Assert.AreEqual(MissionPhase.Active, state.Phase);
        }
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void MissionCanFinishAtConfiguredSquadSizeWithoutReplayingObjectives(int count)
        {
            var state = MissionSnapshot.Begin(definition, 3);
            for (int i = 0; i < 40; i++) state = MissionRules.Observe(definition, state, count, count, 0.1f);
            Assert.AreEqual(MissionPhase.Completed, state.Phase); Assert.AreEqual(2, state.ObjectiveIndex);
            Assert.AreEqual(state, MissionRules.Observe(definition, state, count, count, 0.1f));
            Assert.AreEqual(state, MissionRules.Fail(state, MissionFailure.TeamWiped));
        }
        [Test] public void RosterChangeBreaksHoldAndLongFrameCannotInstantlyFinish()
        {
            var state = MissionSnapshot.Begin(definition, 1);
            state = MissionRules.Observe(definition, state, 4, 4, 30);
            Assert.AreEqual(0, state.ObjectiveIndex); Assert.AreEqual(0.25f, state.HeldSeconds);
            state = MissionRules.Observe(definition, state, 3, 3, 0.1f);
            Assert.AreEqual(0, state.HeldSeconds);
        }
        [Test] public void InvalidObservationsAndDifferentMissionCannotAdvance()
        {
            var state = MissionSnapshot.Begin(definition, 1);
            Assert.AreEqual(state, MissionRules.Observe(definition, state, 5, 4, 1));
            Assert.AreEqual(state, MissionRules.Observe(definition, state, 0, 0, 1));
            Assert.AreEqual(state, MissionRules.Observe(definition, state, 2, 2, float.NaN));
            Assert.AreEqual(state, MissionRules.Observe(definition, state, 2, 2, -1));
            definition.missionId = "DIFFERENT";
            Assert.AreEqual(state, MissionRules.Observe(definition, state, 2, 2, 1));
        }
        [Test] public void ZoneRejectsAnotherFloorNonFinitePositionAndOutsideBoundary()
        {
            var zone = definition.objectives[0];
            Assert.IsTrue(zone.Contains(new Vector3(0, 1.1f, 0)));
            Assert.IsFalse(zone.Contains(new Vector3(0, 20, 0)));
            Assert.IsFalse(zone.Contains(new Vector3(6.1f, 1.1f, 0)));
            Assert.IsFalse(zone.Contains(new Vector3(float.NaN, 1.1f, 0)));
        }
        [Test] public void WipeCannotAdvanceAndNewAttemptStartsAtFirstObjective()
        {
            var state = MissionSnapshot.Begin(definition, 1);
            state = MissionRules.Fail(state, MissionFailure.TeamWiped);
            Assert.AreEqual(MissionPhase.Failed, state.Phase);
            Assert.AreEqual(state, MissionRules.Observe(definition, state, 4, 4, 30));
            state = MissionSnapshot.Begin(definition, state.Attempt + 1);
            Assert.AreEqual(2, state.Attempt); Assert.AreEqual(0, state.ObjectiveIndex);
        }
        [Test] public void MissionSetupPreservesScenesGuidAndRunsAgainWithoutChangingAssets()
        {
            MissionSetupTool.Install();
            string[] paths = { MissionSetupTool.DefinitionPath, MissionSetupTool.MaterialPath, MultiplayerSetupTool.RoomPath };
            var before = System.Array.ConvertAll(paths, File.ReadAllBytes);
            string guid = AssetDatabase.AssetPathToGUID(MultiplayerSetupTool.RoomPath);
            var scene = SceneManager.GetActiveScene();
            MissionSetupTool.Install();
            for (int i = 0; i < paths.Length; i++) CollectionAssert.AreEqual(before[i], File.ReadAllBytes(paths[i]));
            Assert.AreEqual(guid, AssetDatabase.AssetPathToGUID(MultiplayerSetupTool.RoomPath));
            Assert.AreEqual(scene, SceneManager.GetActiveScene());
        }
        [Test] public void AuthoredRallyPointsHaveReachableNavigation()
        {
            var mission = AssetDatabase.LoadAssetAtPath<MissionDefinition>(MissionSetupTool.DefinitionPath);
            Assert.IsTrue(mission.Validate(out _));
            var nav = NavMesh.AddNavMeshData(AssetDatabase.LoadAssetAtPath<NavMeshData>(CanThoMapSetupTool.NavigationPath));
            try
            {
                foreach (var zone in mission.objectives)
                    Assert.IsTrue(NavMesh.SamplePosition(zone.center, out _, 0.5f, NavMesh.AllAreas), zone.id);
            }
            finally { nav.Remove(); }
        }
    }
}
