using System;
using System.IO;
using TMPro;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LastProtocol.Editor
{
    public static class CombatSetupTool
    {
        public const string TargetPath = MultiplayerSetupTool.Root + "/Prefabs/Networking/TrainingTarget.prefab";
        private const string TracerPath = MultiplayerSetupTool.Root + "/Materials/ShotTracer.mat";
        [MenuItem("LastProtocol/Combat/Install Hitscan and Training Targets")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            if (PrefabStageUtility.GetCurrentPrefabStage() != null) throw new InvalidOperationException("Close Prefab Mode first.");
            // A fresh batch Editor starts in an untitled scene. Open a saved scene only
            // in that isolated validation run; never save/replace an interactive scene.
            if (Application.isBatchMode && string.IsNullOrEmpty(SceneManager.GetActiveScene().path))
                EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (string.IsNullOrEmpty(SceneManager.GetSceneAt(i).path))
                    throw new InvalidOperationException("Save untitled scenes before Combat setup. No scene was changed.");
            PlayerHealthSetupTool.Install();
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MultiplayerSetupTool.Root
                + "/Settings/TextMeshPro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            var target = GetTarget(font);
            var tracer = AssetDatabase.LoadAssetAtPath<Material>(TracerPath);
            if (tracer == null)
            {
                if (File.Exists(TracerPath)) throw new InvalidOperationException("Existing tracer asset is incompatible; preserved.");
                var shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null) throw new InvalidOperationException("URP/Unlit missing.");
                tracer = new Material(shader); tracer.SetColor("_BaseColor", new Color(1, 0.7f, 0.1f));
                AssetDatabase.CreateAsset(tracer, TracerPath);
            }
            var player = AssetDatabase.LoadAssetAtPath<GameObject>(MultiplayerSetupTool.PlayerPath);
            var weapon = player.GetComponent<NetworkHitscanWeapon>();
            var hud = player.GetComponent<WeaponHUD>();
            bool needsPlayer = weapon == null || hud == null || weapon.trainingTargetPrefab != target
                || weapon.tracerMaterial != tracer || hud.font != font;
            var list = AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>(MultiplayerSetupTool.PrefabListPath);
            if (list == null) throw new InvalidOperationException("Existing serialized network prefab list missing.");
            bool needsList = !list.Contains(target.gameObject);
            if (needsPlayer || needsList)
            {
                string backup = "Builds/CombatBackups/" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");
                Directory.CreateDirectory(backup);
                foreach (string path in new[] { MultiplayerSetupTool.PlayerPath, MultiplayerSetupTool.PrefabListPath })
                {
                    File.Copy(path, backup + "/" + Path.GetFileName(path));
                    File.Copy(path + ".meta", backup + "/" + Path.GetFileName(path) + ".meta");
                }
                if (needsPlayer)
                {
                    var root = PrefabUtility.LoadPrefabContents(MultiplayerSetupTool.PlayerPath);
                    try
                    {
                        weapon = root.GetComponent<NetworkHitscanWeapon>();
                        if (weapon == null) weapon = root.AddComponent<NetworkHitscanWeapon>();
                        hud = root.GetComponent<WeaponHUD>();
                        if (hud == null) hud = root.AddComponent<WeaponHUD>();
                        weapon.trainingTargetPrefab = target; weapon.tracerMaterial = tracer; hud.font = font;
                        if (PrefabUtility.SaveAsPrefabAsset(root, MultiplayerSetupTool.PlayerPath) == null)
                            throw new IOException("Could not save Player prefab.");
                    }
                    finally { PrefabUtility.UnloadPrefabContents(root); }
                }
                if (needsList)
                {
                    list.Add(new NetworkPrefab { Prefab = target.gameObject });
                    EditorUtility.SetDirty(list); AssetDatabase.SaveAssets();
                }
                Debug.Log("Combat prefab/list backup: " + backup);
            }
            Validate();
        }
        private static TrainingTarget GetTarget(TMP_FontAsset font)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(TargetPath);
            if (existing != null) return existing.GetComponent<TrainingTarget>()
                ?? throw new InvalidOperationException("Existing target is incompatible; preserved.");
            if (File.Exists(TargetPath)) throw new InvalidOperationException("Existing target asset preserved.");
            var previous = SceneManager.GetActiveScene();
            var scratch = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scratch);
                var root = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                root.name = "TrainingTarget";
                root.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                    MultiplayerSetupTool.Root + "/Materials/Player.mat");
                root.AddComponent<NetworkObject>();
                root.AddComponent<TrainingTarget>().font = font;
                return PrefabUtility.SaveAsPrefabAsset(root, TargetPath).GetComponent<TrainingTarget>();
            }
            finally
            {
                EditorSceneManager.CloseScene(scratch, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }
        public static void Validate()
        {
            PlayerHealthSetupTool.Validate();
            var player = AssetDatabase.LoadAssetAtPath<GameObject>(MultiplayerSetupTool.PlayerPath);
            var weapon = player.GetComponent<NetworkHitscanWeapon>();
            var target = AssetDatabase.LoadAssetAtPath<GameObject>(TargetPath);
            var list = AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>(MultiplayerSetupTool.PrefabListPath);
            if (player.GetComponents<NetworkHitscanWeapon>().Length != 1 || player.GetComponents<WeaponHUD>().Length != 1
                || weapon.tracerMaterial == null || weapon.trainingTargetPrefab == null || player.GetComponent<WeaponHUD>().font == null
                || target == null || target.GetComponent<TrainingTarget>() == null || target.GetComponent<NetworkObject>() == null
                || target.GetComponent<Collider>() == null || list == null || !list.Contains(target))
                throw new InvalidOperationException("Combat prefab/registration invalid.");
            Debug.Log("LAST_PROTOCOL Combat assets validation PASS");
        }
        public static void BatchInstall()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Use Combat menu for interactive setup.");
            Install();
        }
    }
}
