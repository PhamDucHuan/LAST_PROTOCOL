using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using TMPro;

namespace LastProtocol.Editor
{
    // Updates existing collider objects without rebuilding user-authored scene roots.
    public static class CanThoRoadClearanceTool
    {
        public static void BatchApply()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Run in isolated validation project.");
            var layout = CanThoMapSetupTool.ReadLayout();
            CanThoMapSetupTool.ConfigureModel(layout);
            var scene = EditorSceneManager.OpenScene(CanThoMapSetupTool.ScenePath, OpenSceneMode.Single);
            var collisionRoot = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Transform>())
                .Single(x => x.name == "Simple Environment Colliders");
            if (collisionRoot.childCount != layout.collisions.Length) throw new InvalidOperationException("Collider count differs; preserve scene for review.");
            var sources = new List<NavMeshBuildSource>();
            int changed = 0;
            for (int i = 0; i < layout.collisions.Length; i++)
            {
                var entry = layout.collisions[i];
                var target = collisionRoot.GetChild(i);
                if (target.name != entry.name) throw new InvalidOperationException("Collider order differs: " + i);
                var box = target.GetComponent<BoxCollider>();
                if (box == null) throw new InvalidOperationException("Missing box: " + target.name);
                if (Vector3.Distance(target.position, entry.position) > .0001f || Vector3.Distance(box.size, entry.size) > .0001f)
                {
                    if (!(entry.name.Contains("trunk") || entry.name == "Turtle auditorium body" || entry.name.StartsWith("Turtle Hall ")))
                        throw new InvalidOperationException("Unexpected collider edit: " + target.name);
                    target.SetPositionAndRotation(entry.position, Quaternion.Euler(entry.euler));
                    box.size = entry.size; changed++;
                }
                sources.Add(new NavMeshBuildSource { shape = NavMeshBuildSourceShape.Box, size = entry.size,
                    transform = Matrix4x4.TRS(entry.position, Quaternion.Euler(entry.euler), Vector3.one), area = entry.name == "Campus soil" ? 0 : 1 });
            }
            for (int slot = 0; slot < MultiplayerConfig.MaxPlayers; slot++)
                sources.Add(new NavMeshBuildSource { shape = NavMeshBuildSourceShape.Box, size = new Vector3(1.1f, 2, 1.1f),
                    transform = Matrix4x4.Translate(new Vector3(-9 + slot * 6, 1, 10)), area = 1 });
            var settings = NavMesh.GetSettingsByIndex(0); settings.agentRadius = .4f; settings.agentHeight = 2; settings.agentClimb = .4f;
            settings.overrideVoxelSize = true; settings.voxelSize = .2f;
            var nav = AssetDatabase.LoadAssetAtPath<NavMeshData>(CanThoMapSetupTool.NavigationPath);
            if (!NavMeshBuilder.UpdateNavMeshData(nav, settings, sources, new Bounds(layout.bounds.center, layout.bounds.size + new Vector3(4, 8, 4))))
                throw new InvalidOperationException("Navigation bake failed.");
            EditorUtility.SetDirty(nav);
            EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            var menu = PrefabUtility.LoadPrefabContents(CanThoMapSetupTool.MenuPath);
            try
            {
                foreach (var text in menu.GetComponentsInChildren<TMP_Text>(true))
                    if (text.text.StartsWith("PROTOTYPE  /  BUILD ", StringComparison.Ordinal))
                        text.text = "PROTOTYPE  /  BUILD " + MultiplayerConfig.ProtocolVersion.ToString("00");
                PrefabUtility.SaveAsPrefabAsset(menu, CanThoMapSetupTool.MenuPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(menu); }
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Debug.Log("CANTHO_TARGETED_ROAD_CLEARANCE_PASS changedColliders=" + changed);
        }
    }
}
