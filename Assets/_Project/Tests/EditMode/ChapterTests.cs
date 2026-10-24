using System.IO;
using System.Linq;
using LastProtocol.Editor;
using NUnit.Framework;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
namespace LastProtocol.Tests
{
    public sealed class ChapterTests
    {
        private MissionDefinition Definition=>AssetDatabase.LoadAssetAtPath<MissionDefinition>(ChapterSetupTool.MissionPath);
        [Test] public void TwelveStagesHaveThirteenAndAHalfMinutesOfActiveOperations()
        {
            var d=Definition;Assert.IsTrue(d.Validate(out string error),error);Assert.AreEqual(12,d.objectives.Length);
            Assert.AreEqual(810,d.objectives.Where(x=>ChapterPlan.Timed(x.kind)).Sum(x=>x.holdSeconds));
            Assert.AreEqual(4,d.objectives.Count(x=>ChapterPlan.Timed(x.kind)));
            Assert.AreEqual(6,ChapterPlan.DeviceIds.Distinct().Count());Assert.AreEqual(11,d.objectives.Where(x=>x.kind==ObjectiveKind.Stations).Sum(x=>x.stationIds.Length));
            Assert.AreEqual(10,d.objectives[^1].holdSeconds);
        }
        [Test] public void TimedWorkNeedsOccupancyAndSecureChipAndCannotFastForward()
        {
            var d=Definition;var s=MissionSnapshot.Begin(d,1);s.ObjectiveIndex=4;s.CompletedStations=31;
            var paused=ChapterPlan.Observe(d,s,0,0,true,.1f);Assert.AreEqual(0,paused.HeldSeconds);
            paused=ChapterPlan.Observe(d,s,1,0,false,.1f);Assert.AreEqual(0,paused.HeldSeconds);
            s=ChapterPlan.Observe(d,s,1,0,true,1000);Assert.AreEqual(.25f,s.HeldSeconds);
            var kept=ChapterPlan.Observe(d,s,0,0,true,.1f);Assert.AreEqual(s.HeldSeconds,kept.HeldSeconds);
            Assert.AreEqual(s.HeldSeconds,ChapterPlan.Observe(d,s,1,0,true,float.NaN).HeldSeconds);
        }
        [Test] public void TwoSignalSitesRequireDistinctOccupants()
        {
            var d=Definition;var s=MissionSnapshot.Begin(d,1);s.ObjectiveIndex=8;
            Assert.AreEqual(0,ChapterPlan.Observe(d,s,4,0,true,.2f).HeldSeconds);
            var both=ChapterPlan.Observe(d,s,1,1,true,.2f);Assert.AreEqual(.2f,both.HeldSeconds);Assert.AreEqual(2,both.Inside);Assert.AreEqual(2,both.Required);
            Assert.Greater(Vector3.Distance(d.objectives[8].center,d.objectives[8].secondaryCenter),d.objectives[8].radius*2);
        }
        [Test] public void ConfirmedTimedStageRestoresAtNextStageWithoutReplayingCredits()
        {
            var d=Definition;var s=MissionSnapshot.Begin(d,1);s.ObjectiveIndex=4;s.CompletedStations=31;s.HeldSeconds=179.9f;
            var next=ChapterPlan.Observe(d,s,1,0,true,.2f);Assert.AreEqual(5,next.ObjectiveIndex);
            var saved=CheckpointRules.Capture(next);Assert.IsTrue(CheckpointRules.Valid(d,saved));
            var retry=CheckpointRules.Restore(d,saved,2);Assert.AreEqual(5,retry.ObjectiveIndex);Assert.AreEqual(31,retry.CompletedStations);Assert.AreEqual(0,retry.HeldSeconds);
            Assert.AreEqual(retry.CompletedStations,MissionRules.CompleteStation(d,retry,DataStation.AdministrationId).CompletedStations);
        }
        [Test] public void WholeChainCompletesOnceAndEveryCheckpointIsValid()
        {
            var d=Definition;var s=MissionSnapshot.Begin(d,1);int advances=0;
            while(s.Phase==MissionPhase.Active)
            {
                var o=d.objectives[s.ObjectiveIndex];int before=s.ObjectiveIndex;
                if(o.kind==ObjectiveKind.Stations)foreach(string id in o.stationIds)s=MissionRules.CompleteStation(d,s,id);
                else if(ChapterPlan.Timed(o.kind))for(int i=0;i<1300&&s.ObjectiveIndex==before;i++)s=ChapterPlan.Observe(d,s,1,1,true,.25f);
                else for(int i=0;i<200&&s.ObjectiveIndex==before;i++)s=MissionRules.Observe(d,s,2,2,.25f);
                Assert.Greater(s.ObjectiveIndex,before);Assert.IsTrue(CheckpointRules.Valid(d,CheckpointRules.Capture(s)));advances++;
            }
            Assert.AreEqual(12,advances);Assert.AreEqual(2047,s.CompletedStations);Assert.AreEqual(12,s.ObjectiveIndex);
            Assert.AreEqual(s,MissionRules.CompleteStation(d,s,ChapterPlan.DeviceIds[^1]));
        }
        [Test] public void AllCheckpointsAndNewTerminalApproachesAreReachableWithoutBakingANewMap()
        {
            var nav=AssetDatabase.LoadAssetAtPath<NavMeshData>(CanThoMapSetupTool.NavigationPath);var instance=NavMesh.AddNavMeshData(nav);
            try
            {
                Assert.IsTrue(NavMesh.SamplePosition(new Vector3(0,0,-40),out var start,1,NavMesh.AllAreas));
                foreach(var o in Definition.objectives)
                {
                    Assert.IsTrue(NavMesh.SamplePosition(o.checkpointPosition,out var hit,1,NavMesh.AllAreas),o.id);
                    var path=new NavMeshPath();Assert.IsTrue(NavMesh.CalculatePath(start.position,hit.position,NavMesh.AllAreas,path));Assert.AreEqual(NavMeshPathStatus.PathComplete,path.status,o.id);
                    var saved=MissionSnapshot.Begin(Definition,1);saved.ObjectiveIndex=System.Array.IndexOf(Definition.objectives,o);
                    for(int slot=0;slot<4;slot++)Assert.IsTrue(NavMesh.SamplePosition(CheckpointRules.Position(Definition,saved,slot),out _,2,NavMesh.AllAreas));
                }
            }
            finally{instance.Remove();}
        }
        [Test] public void ProductionRoomSelectsLongChapterAndKeepsShortRegressionDefinition()
        {
            var room=AssetDatabase.LoadAssetAtPath<GameObject>(MultiplayerSetupTool.RoomPath);
            Assert.AreEqual(Definition,room.GetComponent<NetworkMissionState>().chapterMission);
            Assert.AreEqual(UplinkEncounterDefinition.MissionId,room.GetComponent<NetworkMissionState>().uplinkMission.missionId);
            var op=room.GetComponent<ChapterOperation>();Assert.NotNull(op);Assert.AreEqual(18,op.reinforcementInterval);
            Assert.IsTrue(AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>(MultiplayerSetupTool.PrefabListPath).Contains(op.terminalPrefab.gameObject));
        }
        [Test] public void InstallerIsIdempotent()
        {
            var paths=new[]{ChapterSetupTool.MissionPath,ChapterSetupTool.TerminalPath,MultiplayerSetupTool.RoomPath,MultiplayerSetupTool.PrefabListPath,CanThoMapSetupTool.MenuPath,CanThoMapSetupTool.ScenePath,CanThoMapSetupTool.NavigationPath};
            var bytes=paths.Select(File.ReadAllBytes).ToArray();ChapterSetupTool.Install();for(int i=0;i<paths.Length;i++)CollectionAssert.AreEqual(bytes[i],File.ReadAllBytes(paths[i]),paths[i]);
        }
    }
}
