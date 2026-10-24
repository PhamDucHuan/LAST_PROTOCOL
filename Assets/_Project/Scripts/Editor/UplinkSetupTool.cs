using System;
using System.IO;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LastProtocol.Editor
{
    public static class UplinkSetupTool
    {
        public const string DefinitionPath = "Assets/_Project/ScriptableObjects/Missions/RestoreUplink.asset";
        public static readonly string[] Ids = { DataStation.AdministrationId, "CTU_ADMIN_DATA_02", "CTU_ADMIN_DATA_03", DataStation.GeneratorId, DataStation.UplinkId };
        public static readonly Vector3[] Positions = { new(30, 0, -24), new(24, 0, -24), new(36, 0, -24), new(42, 0, -32), new(61, 0, -42) };
        public static void BatchInstallAndExit()
        {
            BatchInstall();
            var unload = Resources.UnloadUnusedAssets(); int frames = 0;
            void Finish()
            {
                if (!unload.isDone || ++frames < 10) return;
                EditorApplication.update -= Finish; GC.Collect(); EditorApplication.Exit(0);
            }
            EditorApplication.update += Finish;
        }
        public static void BatchInstall()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Install in the isolated validation project.");
            DataStationSetupTool.BatchInstall();
            MissionSetupTool.Install();
            var definition = AssetDatabase.LoadAssetAtPath<MissionDefinition>(DefinitionPath);
            if (definition == null)
            {
                if (File.Exists(DefinitionPath)) throw new InvalidOperationException("Existing mission preserved.");
                definition = ScriptableObject.CreateInstance<MissionDefinition>();
                definition.schemaVersion = 1; definition.missionId = "CTU_01_RESTORE_UPLINK";
                definition.title = "RESTORE COMMUNICATIONS";
                definition.briefing = "Rally at Gate B, secure three local records, power the auxiliary generator and restore the library uplink.";
                definition.completionText = "Local rescue channel restored. Await the next operation.";
                definition.objectives = new[]
                {
                    new RendezvousObjective { id = "CTU01_RALLY", title = "RALLY AT GATE B", center = new Vector3(0,0,-40),
                        radius = 6, holdSeconds = 2, instruction = "Everyone alive inside the cyan ring." },
                    Devices("CTU01_DATA", "SECURE THREE LOCAL RECORDS", Positions[0], Ids.Take(3).ToArray(), "Administration forecourt | Each terminal counts once."),
                    Devices("CTU01_POWER", "START THE AUXILIARY GENERATOR", Positions[3], new[] { Ids[3] }, "Power is required before the library uplink can start."),
                    Devices("CTU01_COMMS", "RESTORE THE LIBRARY UPLINK", Positions[4], new[] { Ids[4] }, "Hold E to restore the local rescue channel.")
                };
                AssetDatabase.CreateAsset(definition, DefinitionPath);
            }
            if (!definition.Validate(out string error)) throw new InvalidOperationException(error);
            var room = PrefabUtility.LoadPrefabContents(MultiplayerSetupTool.RoomPath);
            try
            {
                var mission = room.GetComponent<NetworkMissionState>();
                if (mission.uplinkMission != null && mission.uplinkMission != definition) throw new InvalidOperationException("Custom uplink mission preserved.");
                if (mission.uplinkMission != definition)
                { mission.uplinkMission = definition; PrefabUtility.SaveAsPrefabAsset(room, MultiplayerSetupTool.RoomPath); }
            }
            finally { PrefabUtility.UnloadPrefabContents(room); }
            var scene = EditorSceneManager.OpenScene(CanThoMapSetupTool.ScenePath, OpenSceneMode.Single);
            bool changed = false;
            var metal = AssetDatabase.LoadAssetAtPath<Material>(DataStationSetupTool.Materials + "/DataTerminalMetal.mat");
            var screen = AssetDatabase.LoadAssetAtPath<Material>(DataStationSetupTool.Materials + "/DataTerminalScreen.mat");
            var font = AssetDatabase.LoadAssetAtPath<GameObject>(MultiplayerSetupTool.PlayerPath).GetComponent<ReviveHUD>().font;
            for (int i = 0; i < Ids.Length; i++)
            {
                string completion = i == 1 ? "Auxiliary generator located on the library route."
                    : i == 2 ? "Library uplink requires auxiliary power."
                    : i == 3 ? "Auxiliary supply connected. Proceed to the library."
                    : "Local rescue channel restored.";
                var matches = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<DataStation>()).Where(x => x.stationId == Ids[i]).ToArray();
                if (matches.Length > 1) throw new InvalidOperationException("Duplicate mission device: " + Ids[i]);
                if (matches.Length == 1)
                {
                    if (i == 4 && matches[0].transform.position != Positions[i])
                    { matches[0].transform.position = Positions[i]; EditorUtility.SetDirty(matches[0].transform); changed = true; }
                    if (i > 0 && matches[0].completionMessage != completion)
                    { matches[0].completionMessage = completion; EditorUtility.SetDirty(matches[0]); changed = true; }
                    continue;
                }
                changed = true;
                var root = new GameObject("Mission device / " + Ids[i]); root.transform.position = Positions[i];
                root.AddComponent<NetworkObject>(); var station = root.AddComponent<DataStation>(); station.stationId = Ids[i];
                station.displayName = i < 3 ? "ADMIN 0" + (i + 1) : i == 3 ? "AUX POWER" : "UPLINK";
                station.completionMessage = completion;
                station.holdSeconds = i < 3 ? 3 : i == 3 ? 4 : 5;
                var box = root.AddComponent<BoxCollider>(); box.center = new Vector3(0, .9f, 0); box.size = new Vector3(1.1f, 1.8f, .9f);
                DataStationSetupTool.Part(root, "Foot", new Vector3(0,.12f,0), new Vector3(1.1f,.24f,.8f), metal);
                DataStationSetupTool.Part(root, "Equipment body", new Vector3(0,.75f,.08f), new Vector3(i == 3 ? 1.05f : .5f,1.2f,.65f), metal);
                DataStationSetupTool.Part(root, "Display frame", new Vector3(0,1.35f,0), new Vector3(1,.7f,.22f), metal);
                DataStationSetupTool.Part(root, "Cyan display", new Vector3(0,1.36f,-.12f), new Vector3(.84f,.51f,.025f), screen);
                DataStationSetupTool.Part(root, "Status strip", new Vector3(0,.72f,-.375f), new Vector3(.12f,.65f,.025f), screen);
                if (i == 3)
                    for (int fin = 0; fin < 6; fin++) DataStationSetupTool.Part(root, "Generator cooling fin", new Vector3(.36f,.4f + fin * .09f,-.28f), new Vector3(.4f,.035f,.1f), metal);
                if (i == 4)
                {
                    DataStationSetupTool.Part(root, "Radio mast", new Vector3(0,2.05f,.25f), new Vector3(.09f,2.2f,.09f), metal);
                    DataStationSetupTool.Part(root, "Radio array", new Vector3(0,2.9f,.25f), new Vector3(1.15f,.12f,.12f), screen);
                    box.center = new Vector3(0,1.6f,0); box.size = new Vector3(1.2f,3.2f,.9f);
                }
                var label = new GameObject("Device label", typeof(TextMeshPro)).GetComponent<TextMeshPro>();
                label.transform.SetParent(root.transform, false); label.transform.localPosition = new Vector3(0,1.38f,-.15f);
                label.font = font; label.fontSize = .85f; label.alignment = TextAlignmentOptions.Center;
                label.rectTransform.sizeDelta = new Vector2(.8f,.45f); label.color = new Color(.01f,.06f,.08f);
                label.text = station.displayName + "\n[ E ]";
            }
            if (changed)
            {
                DataStationSetupTool.Bake(null);
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Uplink scene save failed.");
            }
            AssetDatabase.SaveAssets();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Debug.Log("RESTORE_UPLINK_INSTALL_PASS");
        }
        private static RendezvousObjective Devices(string id, string title, Vector3 center, string[] ids, string instruction) => new()
        { id = id, title = title, kind = ObjectiveKind.Stations, stationIds = ids, center = center, radius = 2.5f, holdSeconds = 3, instruction = instruction };
    }
}
