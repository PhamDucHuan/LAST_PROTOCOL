using System;
using System.IO;
using TMPro;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace LastProtocol.Editor
{
    public static class UplinkDefenseSetupTool
    {
        public const string EnemyPath = "Assets/_Project/Prefabs/Networking/UplinkPatrol.prefab";
        public const string DefinitionPath = "Assets/_Project/ScriptableObjects/Missions/UplinkDefense.asset";
        public static readonly Vector3[] Positions = { new(50,0,-46), new(58,0,-46), new(66,0,-46) };
        public static void Install()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Install in the validation project.");
            var nav = AssetDatabase.LoadAssetAtPath<NavMeshData>(CanThoMapSetupTool.NavigationPath);
            var robot = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPath)?.GetComponent<RobotEnemy>();
            if (robot == null)
            {
                if (File.Exists(EnemyPath)) throw new InvalidOperationException("Existing incompatible enemy preserved.");
                var root = PrefabUtility.LoadPrefabContents(EnemySetupTool.EnemyPath);
                try
                {
                    var enemy = root.GetComponent<RobotEnemy>(); enemy.navigation = nav;
                    enemy.respawnOnDeath = false; enemy.externallyManagedNavigation = true;
                    enemy.encounterId = UplinkEncounterDefinition.WaveId;
                    root.GetComponent<RobotEnemyView>().displayName = "PATROL";
                    robot = PrefabUtility.SaveAsPrefabAsset(root, EnemyPath).GetComponent<RobotEnemy>();
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            var definition = AssetDatabase.LoadAssetAtPath<UplinkEncounterDefinition>(DefinitionPath);
            if (definition == null)
            {
                if (File.Exists(DefinitionPath)) throw new InvalidOperationException("Existing encounter preserved.");
                definition = ScriptableObject.CreateInstance<UplinkEncounterDefinition>();
                definition.enemyPrefab = robot; definition.navigation = nav; definition.spawnPoints = (Vector3[])Positions.Clone();
                AssetDatabase.CreateAsset(definition, DefinitionPath);
            }
            if (!definition.Validate(out string error)) throw new InvalidOperationException(error);
            var list = AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>(MultiplayerSetupTool.PrefabListPath);
            if (!list.Contains(robot.gameObject))
            { list.Add(new NetworkPrefab { Prefab = robot.gameObject }); EditorUtility.SetDirty(list); }
            var room = PrefabUtility.LoadPrefabContents(MultiplayerSetupTool.RoomPath);
            try
            {
                var encounter = room.GetComponent<UplinkEncounter>();
                if (encounter == null)
                {
                    encounter = room.AddComponent<UplinkEncounter>(); encounter.definition = definition;
                    PrefabUtility.SaveAsPrefabAsset(room, MultiplayerSetupTool.RoomPath);
                }
                else if (encounter.definition != definition) throw new InvalidOperationException("Custom encounter configuration preserved.");
            }
            finally { PrefabUtility.UnloadPrefabContents(room); }
            var menu = PrefabUtility.LoadPrefabContents(CanThoMapSetupTool.MenuPath);
            try
            {
                bool changed = false;
                foreach (var text in menu.GetComponentsInChildren<TMP_Text>(true))
                    if (text.text.StartsWith("PROTOTYPE  /  BUILD ", StringComparison.Ordinal))
                    { string value = "PROTOTYPE  /  BUILD " + MultiplayerConfig.ProtocolVersion.ToString("00"); changed |= text.text != value; text.text = value; }
                if (changed) PrefabUtility.SaveAsPrefabAsset(menu, CanThoMapSetupTool.MenuPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(menu); }
            AssetDatabase.SaveAssets();
            Debug.Log("UPLINK_DEFENCE_INSTALL_PASS");
        }
        public static void BatchInstallAndExit()
        {
            Install(); EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var unload = Resources.UnloadUnusedAssets(); int frames = 0;
            void Finish()
            {
                if (!unload.isDone || ++frames < 10) return;
                EditorApplication.update -= Finish; GC.Collect(); EditorApplication.Exit(0);
            }
            EditorApplication.update += Finish;
        }
    }
}
