using System;
using System.IO;
using System.Linq;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
namespace LastProtocol.Editor
{
    public static class ChapterSetupTool
    {
        public const string MissionPath="Assets/_Project/ScriptableObjects/Missions/CampusRescue.asset";
        public const string TerminalPath="Assets/_Project/Prefabs/Networking/ChapterTerminal.prefab";
        public static void Install()
        {
            if(!Application.isBatchMode)throw new InvalidOperationException("Install only in validation copy");
            var definition=AssetDatabase.LoadAssetAtPath<MissionDefinition>(MissionPath);
            if(definition==null)
            {
                if(File.Exists(MissionPath))throw new InvalidOperationException("Existing chapter preserved");
                definition=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<MissionDefinition>(UplinkSetupTool.DefinitionPath));
                definition.name="CampusRescue";definition.missionId=ChapterPlan.MissionId;definition.title="CAN THO / RESCUE";
                definition.briefing="Restore the grid, synchronize rescue signals and secure extraction. Target 15-20 minutes.";
                definition.completionText="Rescue broadcast secured. The squad and control chip reached extraction.";
                var first=definition.objectives.ToList();
                first[0].checkpointPosition=new(0,0,-40);first[1].checkpointPosition=new(30,0,-27);first[2].checkpointPosition=new(30,0,-27);first[3].checkpointPosition=new(40,0,-35);
                first.Add(Stage("CTU01_UPLOAD","PROTECT THE LIBRARY TRANSFER",ObjectiveKind.HoldZone,new(61,0,-44),180,"One living operator in the ring. Defenders can move and fire.",new(57,0,-48)));
                first.Add(Devices("CTU01_GRID","RECONNECT TWO GRID SWITCHES",0,1,new(65,0,28),new(65,0,28)));
                first.Add(Stage("CTU01_SYNC_POWER","STABILIZE THE CAMPUS GRID",ObjectiveKind.HoldZone,new(14,0,57.72f),180,"Keep one operator inside. Secure the chip if it drops.",new(8,0,49)));
                first.Add(Devices("CTU01_ARCHIVES","SECURE TWO EMERGENCY LOGS",2,3,new(-9,0,102),new(0,0,100)));
                var split=Stage("CTU01_SIGNAL","SYNCHRONIZE TWO SIGNAL SITES",ObjectiveKind.SplitScan,new(-68,0,62.09f),210,"One living player at A and another at B. Chip must be secure.",new(-87,0,26.4f));
                split.secondaryCenter=new(-87,0,26.4f);first.Add(split);
                first.Add(Devices("CTU01_ROUTE","RECONNECT THE RESCUE RADIO ROUTE",4,5,new(-108,0,17),new(-112,0,16)));
                first.Add(Stage("CTU01_BROADCAST","DEFEND THE RESCUE BROADCAST",ObjectiveKind.HoldZone,new(-33,0,-37.5f),240,"Hold the ring under attack. Then regroup for extraction.",new(-33,0,-37.5f)));
                first.Add(Stage("CTU01_EXTRACT","EXTRACT THE WHOLE SQUAD",ObjectiveKind.Rendezvous,new(0,0,-40),10,"Everyone alive inside the ring. Recover the chip before leaving.",new(0,0,-40)));
                definition.objectives=first.ToArray();AssetDatabase.CreateAsset(definition,MissionPath);
            }
            if(!definition.Validate(out string error))throw new InvalidOperationException(error);
            var nav=AssetDatabase.LoadAssetAtPath<NavMeshData>(CanThoMapSetupTool.NavigationPath);var handle=NavMesh.AddNavMeshData(nav);
            try
            {
                foreach(var o in definition.objectives)
                {
                    if(o.kind!=ObjectiveKind.Stations)Check(o.center);Check(o.checkpointPosition);
                    if(o.kind==ObjectiveKind.SplitScan)Check(o.secondaryCenter);
                    if(o.kind==ObjectiveKind.Stations)foreach(var id in o.stationIds){int index=Array.IndexOf(ChapterPlan.DeviceIds,id);if(index>=0)Check(ChapterPlan.DevicePositions[index]+Vector3.back*1.7f);}
                }
            }
            finally{handle.Remove();}
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(TerminalPath);
            if(prefab==null)
            {
                if(File.Exists(TerminalPath))throw new InvalidOperationException("Existing terminal preserved");
                var scene=EditorSceneManager.OpenScene(CanThoMapSetupTool.ScenePath,OpenSceneMode.Single);
                var source=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<DataStation>()).First(x=>x.stationId==DataStation.AdministrationId);
                var root=UnityEngine.Object.Instantiate(source.gameObject);root.name="Chapter terminal";
                root.transform.position=Vector3.zero;var station=root.GetComponent<DataStation>();station.stationId="";station.displayName="TERMINAL";station.completionMessage="Route component secured.";station.holdSeconds=4;
                var obstacle=root.AddComponent<NavMeshObstacle>();obstacle.shape=NavMeshObstacleShape.Box;obstacle.center=new(0,.9f,0);obstacle.size=new(1.1f,1.8f,.9f);obstacle.carving=true;
                prefab=PrefabUtility.SaveAsPrefabAsset(root,TerminalPath);UnityEngine.Object.DestroyImmediate(root);
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            }
            var registry=AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>(MultiplayerSetupTool.PrefabListPath);
            if(!registry.Contains(prefab)){registry.Add(new NetworkPrefab{Prefab=prefab});EditorUtility.SetDirty(registry);}
            var room=PrefabUtility.LoadPrefabContents(MultiplayerSetupTool.RoomPath);
            try
            {
                bool dirty=false;var mission=room.GetComponent<NetworkMissionState>();
                if(mission.chapterMission==null){mission.chapterMission=definition;dirty=true;}
                if(mission.chapterMission!=definition)throw new InvalidOperationException("Custom chapter preserved");
                var operation=room.GetComponent<ChapterOperation>();
                if(operation==null){operation=room.AddComponent<ChapterOperation>();operation.navigation=nav;operation.terminalPrefab=prefab.GetComponent<DataStation>();dirty=true;}
                if(operation.navigation!=nav||operation.terminalPrefab!=prefab.GetComponent<DataStation>())throw new InvalidOperationException("Custom operation preserved");
                if(dirty)PrefabUtility.SaveAsPrefabAsset(room,MultiplayerSetupTool.RoomPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(room);}
            CheckpointChipSetupTool.Install();AssetDatabase.SaveAssets();Debug.Log("CHAPTER_INSTALL_PASS");
        }
        private static void Check(Vector3 point)
        {if(!NavMesh.SamplePosition(point,out _,.8f,NavMesh.AllAreas))throw new InvalidOperationException("Chapter point off navigation: "+point);}
        private static RendezvousObjective Stage(string id,string title,ObjectiveKind kind,Vector3 center,float seconds,string instruction,Vector3 safe)=>new()
        {id=id,title=title,kind=kind,center=center,radius=8,holdSeconds=seconds,instruction=instruction,checkpointPosition=safe,stationIds=Array.Empty<string>()};
        private static RendezvousObjective Devices(string id,string title,int a,int b,Vector3 center,Vector3 safe)=>new()
        {id=id,title=title,kind=ObjectiveKind.Stations,center=center,radius=2.5f,holdSeconds=4,instruction="Hold E at both marked terminals. Each counts once.",checkpointPosition=safe,stationIds=new[]{ChapterPlan.DeviceIds[a],ChapterPlan.DeviceIds[b]}};
        public static void BatchInstallAndExit()
        {
            Install();EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var unload=Resources.UnloadUnusedAssets();int frames=0;
            void Finish(){if(!unload.isDone||++frames<10)return;EditorApplication.update-=Finish;GC.Collect();EditorApplication.Exit(0);}EditorApplication.update+=Finish;
        }
    }
}
