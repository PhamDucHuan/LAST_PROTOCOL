using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastProtocol.Editor
{
    public static class PlayerHealthSetupTool
    {
        [MenuItem("LastProtocol/Health/Install Health on Player Prefab")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            string path = MultiplayerSetupTool.PlayerPath;
            if (PrefabStageUtility.GetCurrentPrefabStage()?.assetPath == path)
                throw new InvalidOperationException("Close the Player prefab editor before installing Health.");
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MultiplayerSetupTool.Root
                + "/Settings/TextMeshPro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if (font == null) throw new InvalidOperationException("Existing TMP font missing.");
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null) throw new InvalidOperationException("Generate MVP Player prefab first.");
            if (asset.GetComponent<PlayerHealth>() != null && asset.GetComponent<PlayerHealthView>()?.font == font)
            { Validate(); return; }
            string backup = "Builds/HealthBackups/" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");
            Directory.CreateDirectory(backup);
            File.Copy(path, backup + "/NetworkPlayer.prefab");
            File.Copy(path + ".meta", backup + "/NetworkPlayer.prefab.meta");
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (root.GetComponent<PlayerHealth>() == null) root.AddComponent<PlayerHealth>();
                var view = root.GetComponent<PlayerHealthView>();
                if (view == null) view = root.AddComponent<PlayerHealthView>();
                view.font = font;
                if (PrefabUtility.SaveAsPrefabAsset(root, path) == null) throw new IOException("Could not save Health prefab.");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            Validate();
            Debug.Log("Health installed; original prefab backup: " + backup + ". No scenes changed.");
        }
        public static void Validate()
        {
            var player = AssetDatabase.LoadAssetAtPath<GameObject>(MultiplayerSetupTool.PlayerPath);
            if (player == null || player.GetComponents<PlayerHealth>().Length != 1
                || player.GetComponents<PlayerHealthView>().Length != 1 || player.GetComponent<PlayerHealthView>().font == null)
                throw new InvalidOperationException("Player Health prefab missing/duplicated components or font.");
            Debug.Log("LAST_PROTOCOL Health prefab validation PASS");
        }
        public static void BatchInstall()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Use the Health menu for interactive setup.");
            Install();
        }
    }
}
