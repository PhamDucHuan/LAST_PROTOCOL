using System.IO;
using System.Linq;
using LastProtocol.Editor;
using NUnit.Framework;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace LastProtocol.Tests
{
    public sealed class UplinkDefenseTests
    {
        private UplinkEncounterDefinition Definition => AssetDatabase.LoadAssetAtPath<UplinkEncounterDefinition>(UplinkDefenseSetupTool.DefinitionPath);
        private MissionDefinition Mission => AssetDatabase.LoadAssetAtPath<MissionDefinition>(UplinkSetupTool.DefinitionPath);
        [Test] public void OnlyGeneratorCompletionUnlocksExactlyOneWavePerAttempt()
        {
            var mission = MissionSnapshot.Begin(Mission, 1);
            var wave = new UplinkEncounterState { Attempt = 1 };
            Assert.IsFalse(UplinkEncounterRules.ShouldActivate(Mission, mission, wave));
            for (int i = 0; i < 20; i++) mission = MissionRules.Observe(Mission, mission, 2, 2, .1f);
            foreach (string id in UplinkSetupTool.Ids.Take(3)) mission = MissionRules.CompleteStation(Mission, mission, id);
            Assert.IsFalse(UplinkEncounterRules.ShouldActivate(Mission, mission, wave));
            mission = MissionRules.CompleteStation(Mission, mission, DataStation.GeneratorId);
            Assert.IsTrue(UplinkEncounterRules.ShouldActivate(Mission, mission, wave));
            wave.Activations = 1; Assert.IsFalse(UplinkEncounterRules.ShouldActivate(Mission, mission, wave));
            wave.Activations = 0; wave.Phase = UplinkEncounterPhase.Cleared;
            Assert.IsFalse(UplinkEncounterRules.ShouldActivate(Mission, mission, wave));
            wave.Phase = UplinkEncounterPhase.Dormant; wave.Attempt = 2;
            Assert.IsFalse(UplinkEncounterRules.ShouldActivate(Mission, mission, wave));
            wave.Attempt = 1; mission = MissionRules.Fail(mission, MissionFailure.TeamWiped);
            Assert.IsFalse(UplinkEncounterRules.ShouldActivate(Mission, mission, wave));
        }
        [Test] public void PrefabSeparatesMissionRobotsFromRespawningDemoAndIsRegistered()
        {
            Assert.IsTrue(Definition.Validate(out string error),error);
            var demo = AssetDatabase.LoadAssetAtPath<GameObject>(EnemySetupTool.EnemyPath).GetComponent<RobotEnemy>();
            Assert.IsTrue(demo.respawnOnDeath); Assert.IsFalse(demo.IsMissionEnemy); Assert.IsFalse(demo.externallyManagedNavigation);
            var robot = Definition.enemyPrefab; Assert.IsFalse(robot.respawnOnDeath); Assert.IsTrue(robot.IsMissionEnemy);
            Assert.AreEqual(3, Definition.spawnPoints.Length);
            Assert.IsTrue(AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>(MultiplayerSetupTool.PrefabListPath).Contains(robot.gameObject));
            var demoHash = new SerializedObject(demo.GetComponent<NetworkObject>()).FindProperty("GlobalObjectIdHash").longValue;
            var missionHash = new SerializedObject(robot.GetComponent<NetworkObject>()).FindProperty("GlobalObjectIdHash").longValue;
            Assert.AreNotEqual(demoHash, missionHash); Assert.AreNotEqual(0, missionHash);
            var room = AssetDatabase.LoadAssetAtPath<GameObject>(MultiplayerSetupTool.RoomPath);
            Assert.AreEqual(1, room.GetComponents<UplinkEncounter>().Length); Assert.AreEqual(Definition,room.GetComponent<UplinkEncounter>().definition);
        }
        [Test] public void EncounterRejectsOverlapNonFiniteAndUnboundedSpawns()
        {
            var copy = Object.Instantiate(Definition);
            try
            {
                copy.spawnPoints[1] = copy.spawnPoints[0]; Assert.IsFalse(copy.Validate(out _));
                copy.spawnPoints[1] = new Vector3(float.NaN,0,0); Assert.IsFalse(copy.Validate(out _));
                copy.spawnPoints = new Vector3[5]; Assert.IsFalse(copy.Validate(out _));
                copy.spawnPoints = null; Assert.IsFalse(copy.Validate(out _));
            }
            finally { Object.DestroyImmediate(copy); }
        }
        [Test] public void RobotSpawnsAreClearAndCanReachTheOperatorWithoutLongDetours()
        {
            var scene = EditorSceneManager.OpenScene(CanThoMapSetupTool.ScenePath, OpenSceneMode.Additive);
            var nav = NavMesh.AddNavMeshData(Definition.navigation);
            try
            {
                Physics.SyncTransforms();
                Assert.IsTrue(NavMesh.SamplePosition(new Vector3(61,0,-44), out var target,.3f,NavMesh.AllAreas));
                foreach (var point in Definition.spawnPoints)
                {
                    Assert.IsTrue(NavMesh.SamplePosition(point,out var start,.3f,NavMesh.AllAreas));
                    Assert.IsFalse(Physics.CheckCapsule(start.position + Vector3.up*.65f,start.position + Vector3.up*1.6f,.4f,1,QueryTriggerInteraction.Ignore),point.ToString());
                    var path = new NavMeshPath();Assert.IsTrue(NavMesh.CalculatePath(start.position,target.position,NavMesh.AllAreas,path));
                    Assert.AreEqual(NavMeshPathStatus.PathComplete,path.status);
                    float length=0;for(int i=1;i<path.corners.Length;i++)length+=Vector3.Distance(path.corners[i-1],path.corners[i]);
                    Assert.Less(length,25,point.ToString());
                }
            }
            finally { nav.Remove(); EditorSceneManager.CloseScene(scene,true); }
        }
        [Test] public void InstallingAgainPreservesSceneNavigationDemoAndMissionAssets()
        {
            string[] paths = { CanThoMapSetupTool.ScenePath,CanThoMapSetupTool.NavigationPath,EnemySetupTool.EnemyPath,
                UplinkSetupTool.DefinitionPath,MissionSetupTool.DefinitionPath,UplinkDefenseSetupTool.DefinitionPath,
                UplinkDefenseSetupTool.EnemyPath,MultiplayerSetupTool.PrefabListPath,MultiplayerSetupTool.RoomPath,CanThoMapSetupTool.MenuPath };
            var before = System.Array.ConvertAll(paths,File.ReadAllBytes);
            UplinkDefenseSetupTool.Install(); UplinkDefenseSetupTool.Install();
            for(int i=0;i<paths.Length;i++)CollectionAssert.AreEqual(before[i],File.ReadAllBytes(paths[i]),paths[i]);
        }
    }
}
