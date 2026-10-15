using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastProtocol.Editor
{
    public static class ReviveSetupTool
    {
        [MenuItem("LastProtocol/Revive/Install Downed and Revive")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            string path = MultiplayerSetupTool.PlayerPath;
            if (PrefabStageUtility.GetCurrentPrefabStage()?.assetPath == path)
                throw new InvalidOperationException("Close the Player prefab editor first.");
            EnemySetupTool.Install();
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MultiplayerSetupTool.Root
                + "/Settings/TextMeshPro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if (font == null) throw new InvalidOperationException("TMP font missing.");
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset.GetComponent<PlayerRevive>() != null && asset.GetComponent<ReviveHUD>()?.font == font)
            { Validate(); return; }
            string backup = "Builds/ReviveBackups/" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");
            Directory.CreateDirectory(backup);
            File.Copy(path, backup + "/NetworkPlayer.prefab");
            File.Copy(path + ".meta", backup + "/NetworkPlayer.prefab.meta");
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (root.GetComponent<PlayerRevive>() == null) root.AddComponent<PlayerRevive>();
                var hud = root.GetComponent<ReviveHUD>();
                if (hud == null) hud = root.AddComponent<ReviveHUD>();
                hud.font = font;
                if (PrefabUtility.SaveAsPrefabAsset(root, path) == null) throw new IOException("Could not save Revive prefab.");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            Validate();
            Debug.Log("Revive installed; prefab backup: " + backup + ". No scenes changed.");
        }
        public static void Validate()
        {
            var player = AssetDatabase.LoadAssetAtPath<GameObject>(MultiplayerSetupTool.PlayerPath);
            if (player == null || player.GetComponents<PlayerRevive>().Length != 1
                || player.GetComponents<ReviveHUD>().Length != 1 || player.GetComponent<ReviveHUD>().font == null)
                throw new InvalidOperationException("Revive prefab components/font missing or duplicated.");
            Debug.Log("LAST_PROTOCOL Revive prefab validation PASS");
        }
        public static void BatchInstall()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Use Revive menu in interactive Editor.");
            Install();
        }
    }
}
