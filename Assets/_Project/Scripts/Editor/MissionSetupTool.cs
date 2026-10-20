using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastProtocol.Editor
{
    public static class MissionSetupTool
    {
        public const string DefinitionPath = "Assets/_Project/ScriptableObjects/Missions/CampusRecon.asset";
        public const string MaterialPath = "Assets/_Project/Materials/Missions/Destination.mat";
        [MenuItem("LastProtocol/Missions/Install Campus Recon")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            if (PrefabStageUtility.GetCurrentPrefabStage()?.assetPath == MultiplayerSetupTool.RoomPath)
                throw new InvalidOperationException("Close the Room prefab editor first. Existing prefab edits are preserved.");
            Directory.CreateDirectory(Path.GetDirectoryName(DefinitionPath));
            Directory.CreateDirectory(Path.GetDirectoryName(MaterialPath));
            AssetDatabase.Refresh();
            var definition = AssetDatabase.LoadAssetAtPath<MissionDefinition>(DefinitionPath);
            if (definition == null)
            {
                if (File.Exists(DefinitionPath)) throw new InvalidOperationException("Existing mission asset is incompatible; preserved.");
                definition = ScriptableObject.CreateInstance<MissionDefinition>();
                definition.missionId = "CTU_00_RECON"; definition.title = "CAMPUS RECON";
                definition.briefing = "Regroup at Gate B, survey the administration building, then locate the Learning Resource Centre uplink.";
                definition.completionText = "Route surveyed. Regroup or explore the campus.";
                definition.objectives = new[]
                {
                    Objective("RECON_GATE_B", "REGROUP AT GATE B", new Vector3(0, 0, -40), 6),
                    Objective("RECON_ADMIN", "REACH THE ADMINISTRATION BUILDING", new Vector3(32, 0, -21), 6),
                    Objective("RECON_LIBRARY", "REACH THE LEARNING RESOURCE CENTRE", new Vector3(57, 0, -42), 6)
                };
                AssetDatabase.CreateAsset(definition, DefinitionPath);
            }
            if (!definition.Validate(out string error)) throw new InvalidOperationException(error);
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                if (File.Exists(MaterialPath)) throw new InvalidOperationException("Existing marker material preserved.");
                var shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null) throw new InvalidOperationException("URP Unlit shader missing.");
                material = new Material(shader) { name = "Mission Destination" };
                material.SetColor("_BaseColor", new Color(0.1f, 0.9f, 1));
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(MultiplayerSetupTool.RoomPath);
            if (existing == null) throw new InvalidOperationException("Generate the multiplayer Room prefab first.");
            var installed = existing.GetComponent<NetworkMissionState>();
            if (installed != null && installed.campusMission == definition && installed.markerMaterial == material)
            { Validate(); return; }
            if (installed != null && installed.campusMission != null && installed.campusMission != definition)
                throw new InvalidOperationException("Room has a custom mission; preserved for manual review.");
            string backup = "Builds/MissionBackups/" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");
            Directory.CreateDirectory(backup);
            File.Copy(MultiplayerSetupTool.RoomPath, backup + "/NetworkRoomState.prefab");
            File.Copy(MultiplayerSetupTool.RoomPath + ".meta", backup + "/NetworkRoomState.prefab.meta");
            var root = PrefabUtility.LoadPrefabContents(MultiplayerSetupTool.RoomPath);
            try
            {
                var mission = root.GetComponent<NetworkMissionState>() ?? root.AddComponent<NetworkMissionState>();
                mission.campusMission = definition; mission.markerMaterial = material;
                if (PrefabUtility.SaveAsPrefabAsset(root, MultiplayerSetupTool.RoomPath) == null)
                    throw new IOException("Mission Room prefab could not be saved.");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets(); Validate();
            Debug.Log("Campus Recon installed. Room backed up at " + backup + "; no scenes modified.");
        }
        private static RendezvousObjective Objective(string id, string title, Vector3 position, float radius) => new()
        { id = id, title = title, center = position, radius = radius, holdSeconds = 2,
            instruction = "Everyone alive inside the cyan ring." };
        public static void Validate()
        {
            var mission = AssetDatabase.LoadAssetAtPath<GameObject>(MultiplayerSetupTool.RoomPath)?.GetComponent<NetworkMissionState>();
            if (mission == null || mission.campusMission == null || mission.markerMaterial == null
                || !mission.campusMission.Validate(out _) || mission.campusMission.levelId != LevelCatalog.CanTho)
                throw new InvalidOperationException("Campus mission configuration is missing or invalid.");
            Debug.Log("LAST_PROTOCOL Campus mission validation PASS");
        }
        public static void BatchInstall() => Install();
    }
}
