using System;
using System.IO;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace LastProtocol.Editor
{
    public static class EnemyAISetupTool
    {
        public const string Folder = "Assets/_Project/ScriptableObjects/EnemyAI";
        public const string EncounterPath = Folder + "/CanThoTacticalEncounter.asset";
        public static string ProfilePath(EnemyRole role) => Folder + "/" + role + ".asset";
        public static string PrefabPath(EnemyRole role) => "Assets/_Project/Prefabs/Networking/Tactical" + role + ".prefab";
        public static void Install()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Install in validation project.");
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/_Project/ScriptableObjects", "EnemyAI");
            var roster = new RobotEnemy[6];
            foreach (EnemyRole role in Enum.GetValues(typeof(EnemyRole)))
            {
                var profile = AssetDatabase.LoadAssetAtPath<EnemyAIProfile>(ProfilePath(role));
                if (profile == null)
                {
                    if (File.Exists(ProfilePath(role))) throw new InvalidOperationException("Incompatible existing profile preserved.");
                    profile = ScriptableObject.CreateInstance<EnemyAIProfile>(); profile.profileId = "ROBOT_" + role.ToString().ToUpperInvariant(); profile.role = role;
                    profile.attack = role == EnemyRole.Support ? EnemyAttack.Repair : role == EnemyRole.Controller ? EnemyAttack.AreaDenial
                        : role == EnemyRole.Suppressor ? EnemyAttack.Rifle : EnemyAttack.Melee;
                    profile.attackRange = role == EnemyRole.Suppressor ? 14 : role == EnemyRole.Controller ? 12 : 1.8f;
                    profile.speed = role == EnemyRole.Flanker ? 3.6f : 3;
                    profile.windup = role == EnemyRole.Suppressor ? 1 : role == EnemyRole.Controller ? 1.5f : .8f;
                    profile.damage = role == EnemyRole.Suppressor ? 6 : 8;
                    profile.cooldown = role == EnemyRole.Controller ? 5 : 1.8f;
                    AssetDatabase.CreateAsset(profile, ProfilePath(role));
                }
                if (!profile.Validate(out string error)) throw new InvalidOperationException(error);
                var robot = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(role))?.GetComponent<RobotEnemy>();
                if (robot == null)
                {
                    if (File.Exists(PrefabPath(role))) throw new InvalidOperationException("Existing custom enemy preserved.");
                    var root = PrefabUtility.LoadPrefabContents(UplinkDefenseSetupTool.EnemyPath);
                    try
                    {
                        root.AddComponent<EnemyBrain>().profile = profile; root.name = "Tactical " + role;
                        root.GetComponent<RobotEnemyView>().displayName = role.ToString();
                        robot = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath(role)).GetComponent<RobotEnemy>();
                    }
                    finally { PrefabUtility.UnloadPrefabContents(root); }
                }
                if (robot.GetComponent<EnemyBrain>()?.profile != profile) throw new InvalidOperationException("Custom enemy profile preserved.");
                roster[(int)role] = robot;
                var list = AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>(MultiplayerSetupTool.PrefabListPath);
                if (!list.Contains(robot.gameObject)) { list.Add(new NetworkPrefab { Prefab = robot.gameObject }); EditorUtility.SetDirty(list); }
            }
            var encounter = AssetDatabase.LoadAssetAtPath<EnemyEncounterProfile>(EncounterPath);
            if (encounter == null)
            { encounter = ScriptableObject.CreateInstance<EnemyEncounterProfile>(); encounter.roster = roster; AssetDatabase.CreateAsset(encounter, EncounterPath); }
            if (!encounter.Validate(out string issue)) throw new InvalidOperationException(issue);
            var room = PrefabUtility.LoadPrefabContents(MultiplayerSetupTool.RoomPath);
            try
            {
                bool changed = false; var director = room.GetComponent<EnemyDirector>();
                if (director == null) { director = room.AddComponent<EnemyDirector>(); director.profile = encounter; changed = true; }
                if (director.profile != encounter) throw new InvalidOperationException("Custom director preserved.");
                var uplink = room.GetComponent<UplinkEncounter>();
                if (uplink.tacticalProfile == null) { uplink.tacticalProfile = encounter; changed = true; }
                if (uplink.tacticalProfile != encounter) throw new InvalidOperationException("Custom tactical encounter preserved.");
                if (changed) PrefabUtility.SaveAsPrefabAsset(room, MultiplayerSetupTool.RoomPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(room); }
            CheckpointChipSetupTool.Install(); AssetDatabase.SaveAssets(); Debug.Log("ENEMY_AI_INSTALL_PASS");
        }
        public static void BatchInstallAndExit()
        {
            Install(); EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var unload = Resources.UnloadUnusedAssets(); int frames = 0;
            void Finish() { if (!unload.isDone || ++frames < 10) return; EditorApplication.update -= Finish; GC.Collect(); EditorApplication.Exit(0); }
            EditorApplication.update += Finish;
        }
    }
}
