using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LastProtocol.Editor
{
    public static class CheckpointChipSetupTool
    {
        public static void Install()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Install in the validation project.");
            var room = PrefabUtility.LoadPrefabContents(MultiplayerSetupTool.RoomPath);
            try
            {
                if (room.GetComponent<NetworkChip>() == null)
                { room.AddComponent<NetworkChip>(); PrefabUtility.SaveAsPrefabAsset(room, MultiplayerSetupTool.RoomPath); }
            }
            finally { PrefabUtility.UnloadPrefabContents(room); }
            var menu = PrefabUtility.LoadPrefabContents(CanThoMapSetupTool.MenuPath);
            try
            {
                bool changed = false;
                foreach (var text in menu.GetComponentsInChildren<TMP_Text>(true))
                    if (text.text.StartsWith("PROTOTYPE  /  BUILD ", StringComparison.Ordinal))
                    {
                        string value = "PROTOTYPE  /  BUILD " + MultiplayerConfig.ProtocolVersion.ToString("00");
                        changed |= text.text != value; text.text = value;
                    }
                if (changed) PrefabUtility.SaveAsPrefabAsset(menu, CanThoMapSetupTool.MenuPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(menu); }
            AssetDatabase.SaveAssets(); Debug.Log("CHECKPOINT_CHIP_INSTALL_PASS");
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
