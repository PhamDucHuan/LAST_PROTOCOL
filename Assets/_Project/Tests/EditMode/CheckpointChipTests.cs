using System.Linq;
using LastProtocol.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace LastProtocol.Tests
{
    public sealed class CheckpointChipTests
    {
        private MissionDefinition Definition => AssetDatabase.LoadAssetAtPath<MissionDefinition>(UplinkSetupTool.DefinitionPath);
        private MissionSnapshot Data()
        {
            var s = MissionSnapshot.Begin(Definition, 1);
            for (int i = 0; i < 20; i++) s = MissionRules.Observe(Definition, s, 2, 2, .1f);
            return s;
        }
        [Test] public void ConfirmedRecordsSurviveRepeatedFailuresWithoutDuplicateCredit()
        {
            var s = MissionRules.CompleteStation(Definition, Data(), DataStation.AdministrationId);
            var checkpoint = CheckpointRules.Capture(s);
            for (uint attempt = 2; attempt < 20; attempt++)
            {
                s = MissionRules.Fail(s, MissionFailure.TeamWiped);
                Assert.AreEqual(MissionPhase.Failed, s.Phase);
                s = CheckpointRules.Restore(Definition, checkpoint, attempt);
                Assert.AreEqual(1, s.CompletedStations); Assert.AreEqual(1, s.ObjectiveIndex);
                Assert.AreEqual(attempt, s.Attempt); Assert.AreEqual(MissionFailure.None, s.Failure);
                Assert.AreEqual(s, MissionRules.CompleteStation(Definition, s, DataStation.AdministrationId));
                Assert.AreEqual(1, MissionRules.Collected(Definition, s, 1));
            }
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)]
        public void RestoreKeepsDependenciesAndCompletedEndingAtEveryCommit(int completed)
        {
            var s = Data();
            foreach (string id in UplinkSetupTool.Ids.Take(completed)) s = MissionRules.CompleteStation(Definition, s, id);
            var saved = CheckpointRules.Capture(s); var restored = CheckpointRules.Restore(Definition, saved, 2);
            Assert.AreEqual(s.CompletedStations, restored.CompletedStations);
            Assert.AreEqual(s.ObjectiveIndex, restored.ObjectiveIndex); Assert.AreEqual(s.Phase, restored.Phase);
            foreach (string id in UplinkSetupTool.Ids.Take(completed))
            {
                Assert.IsTrue(CheckpointRules.StationCompleted(Definition, restored, id));
                Assert.AreEqual(restored, MissionRules.CompleteStation(Definition, restored, id));
            }
            if (completed < 4) Assert.IsFalse(MissionRules.CanUseStation(Definition, restored, DataStation.UplinkId));
        }
        [Test] public void CheckpointDiscardsPartialHoldsAndRejectsInvalidOrOldRestore()
        {
            var s = MissionSnapshot.Begin(Definition, 1); s.Inside = s.Required = 2; s.HeldSeconds = 1.3f;
            var saved = CheckpointRules.Capture(s);
            Assert.AreEqual(0, saved.HeldSeconds); Assert.AreEqual(0, saved.Inside); Assert.AreEqual(0, saved.Required);
            Assert.AreEqual(default(MissionSnapshot), CheckpointRules.Restore(Definition, saved, 1));
            s.SchemaVersion = 100; Assert.IsFalse(CheckpointRules.Valid(Definition, s));
            s = MissionRules.Fail(saved, MissionFailure.TeamWiped); Assert.IsFalse(CheckpointRules.Valid(Definition, s));
            s = saved; s.ObjectiveIndex = 16; Assert.IsFalse(CheckpointRules.Valid(Definition, s));
        }
        [Test] public void RecoveryCannotBecomeReviveOrDrillOnOneGesture()
        {
            var gate = new InteractionGate(); Assert.IsTrue(gate.TryHold(InteractionKind.RecoverChip, 7));
            Assert.IsFalse(gate.TryHold(InteractionKind.Revive, 3)); Assert.IsTrue(gate.RequiresRelease);
            gate.Release(); Assert.IsTrue(gate.TryHold(InteractionKind.RelayDrill, 7)); gate.Stop(true);
            Assert.IsFalse(gate.TryHold(InteractionKind.RecoverChip, 7));
            gate.Release(); Assert.IsTrue(gate.TryHold(InteractionKind.RecoverChip, 7));
            Assert.IsFalse(gate.TryHold(InteractionKind.DataStation, 8));
        }
        [Test] public void CheckpointRejectsUnknownCreditAndBrokenDependencies()
        {
            var s = Data(); s.CompletedStations = 1u << 31; Assert.IsFalse(CheckpointRules.Valid(Definition, s));
            s = Data(); s.CompletedStations = 8; Assert.IsFalse(CheckpointRules.Valid(Definition, s));
            s = Data(); s.ObjectiveIndex = 3; Assert.IsFalse(CheckpointRules.Valid(Definition, s));
            s = Data(); s.CompletedStations = 7; Assert.IsFalse(CheckpointRules.Valid(Definition, s));
        }
        [Test] public void CanonicalRoomHasOneChipAndExistingPlayerRemainsOneSharedInteraction()
        {
            var room = AssetDatabase.LoadAssetAtPath<GameObject>(MultiplayerSetupTool.RoomPath);
            Assert.AreEqual(1, room.GetComponents<NetworkChip>().Length);
            var chip = room.GetComponent<NetworkChip>(); Assert.AreEqual(0, chip.initialCarrierSlot);
            Assert.AreEqual(2, chip.recoverySeconds); Assert.AreEqual(1.5f, chip.drillSeconds);
            var player = AssetDatabase.LoadAssetAtPath<GameObject>(MultiplayerSetupTool.PlayerPath);
            Assert.AreEqual(1, player.GetComponents<PlayerInteraction>().Length); Assert.AreEqual(0, player.GetComponents<NetworkChip>().Length);
        }
        [Test] public void EveryCheckpointSpawnAndDrillIsReachableAndClearForFourPlayers()
        {
            var scene = EditorSceneManager.OpenScene(CanThoMapSetupTool.ScenePath, OpenSceneMode.Additive);
            var nav = NavMesh.AddNavMeshData(AssetDatabase.LoadAssetAtPath<NavMeshData>(CanThoMapSetupTool.NavigationPath));
            try
            {
                Physics.SyncTransforms();
                Assert.IsTrue(NavMesh.SamplePosition(new Vector3(0, 0, -40), out var start, .3f, NavMesh.AllAreas));
                for (int objective = 1; objective <= 4; objective++) for (int slot = 0; slot < 4; slot++)
                {
                    var s = Data(); s.ObjectiveIndex = objective;
                    Vector3 pos = CheckpointRules.Position(s, slot), ground = new(pos.x, 0, pos.z);
                    Assert.IsTrue(NavMesh.SamplePosition(ground, out var hit, .35f, NavMesh.AllAreas), $"CP {objective} slot {slot}");
                    var path = new NavMeshPath(); NavMesh.CalculatePath(start.position, hit.position, NavMesh.AllAreas, path);
                    Assert.AreEqual(NavMeshPathStatus.PathComplete, path.status);
                    var blockers = Physics.OverlapCapsule(pos + Vector3.down * .55f, pos + Vector3.up * .55f, .45f,
                        Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore).Where(x => x.bounds.max.y > .3f).ToArray();
                    Assert.IsEmpty(blockers, $"CP {objective} slot {slot} blocked: " + string.Join(",", blockers.Select(x => x.name)));
                }
            }
            finally { nav.Remove(); EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
