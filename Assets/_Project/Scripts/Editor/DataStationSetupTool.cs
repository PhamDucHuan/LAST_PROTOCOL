using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

namespace LastProtocol.Editor
{
    public static class DataStationSetupTool
    {
        public static readonly Vector3 Position = new(30, 0, -24);
        public const string RootName = "Administration Data Station / CTU_ADMIN_DATA_01";
        public const string Materials = "Assets/_Project/Materials/Interactions";
        public static void BatchInstall()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Install in the isolated validation copy.");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MultiplayerSetupTool.PlayerPath);
            if (prefab.GetComponent<PlayerInteraction>() == null)
            {
                var player = PrefabUtility.LoadPrefabContents(MultiplayerSetupTool.PlayerPath);
                try { player.AddComponent<PlayerInteraction>(); PrefabUtility.SaveAsPrefabAsset(player, MultiplayerSetupTool.PlayerPath); }
                finally { PrefabUtility.UnloadPrefabContents(player); }
            }
            var scene = EditorSceneManager.OpenScene(CanThoMapSetupTool.ScenePath, OpenSceneMode.Single);
            if (!scene.GetRootGameObjects().Any(x => x.GetComponent<DataStation>() != null))
            {
                var root = new GameObject(RootName); root.transform.position = Position;
                root.AddComponent<NetworkObject>(); root.AddComponent<DataStation>();
                var collider = root.AddComponent<BoxCollider>(); collider.center = new Vector3(0, .9f, 0); collider.size = new Vector3(1.1f, 1.8f, .9f);
                var metal = Material("DataTerminalMetal", new Color(.12f, .17f, .21f));
                var screen = Material("DataTerminalScreen", new Color(.05f, .8f, .95f), true);
                Part(root, "Foot", new Vector3(0, .12f, 0), new Vector3(1.1f, .24f, .8f), metal);
                Part(root, "Pedestal", new Vector3(0, .7f, .08f), new Vector3(.48f, 1, .48f), metal);
                Part(root, "Display frame", new Vector3(0, 1.35f, 0), new Vector3(1, .7f, .22f), metal);
                Part(root, "Cyan display", new Vector3(0, 1.36f, -.12f), new Vector3(.84f, .51f, .025f), screen);
                Part(root, "Status strip", new Vector3(0, .72f, -.175f), new Vector3(.12f, .65f, .025f), screen);
                var label = new GameObject("Data terminal label", typeof(TextMeshPro)).GetComponent<TextMeshPro>();
                label.transform.SetParent(root.transform, false); label.transform.localPosition = new Vector3(0, 1.38f, -.15f);
                label.font = prefab.GetComponent<ReviveHUD>().font; label.fontSize = 1.1f; label.alignment = TextAlignmentOptions.Center;
                label.rectTransform.sizeDelta = new Vector2(.8f, .45f); label.color = new Color(.01f, .06f, .08f);
                label.text = "ADMIN DATA\n[ E ]";
                Bake(collider);
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Station scene save failed.");
            }
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
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Debug.Log("ADMIN_DATA_STATION_INSTALL_PASS");
        }
        private static Material Material(string name, Color color, bool unlit = false)
        {
            string path = Materials + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path); if (material != null) return material;
            Directory.CreateDirectory(Materials);
            material = new Material(Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit")) { name = name };
            material.SetColor("_BaseColor", color); AssetDatabase.CreateAsset(material, path); return material;
        }
        public static void BatchCapture()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Use validation batch.");
            var args = Environment.GetCommandLineArgs(); int index = Array.IndexOf(args, "--lp-station-image");
            if (index < 0 || index + 1 >= args.Length) throw new ArgumentException("Missing station capture path.");
            EditorSceneManager.OpenScene(CanThoMapSetupTool.ScenePath, OpenSceneMode.Single);
            var go = new GameObject("Station capture camera", typeof(Camera)); var camera = go.GetComponent<Camera>();
            camera.enabled = false; camera.transform.position = new Vector3(33, 2.7f, -28.5f);
            camera.transform.LookAt(Position + Vector3.up); camera.fieldOfView = 55; camera.farClipPlane = 600;
            var texture = new RenderTexture(1600, 1000, 24); texture.Create();
            for (int i = 0; i < 4; i++) RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = texture });
            var previous = RenderTexture.active; RenderTexture.active = texture;
            var pixels = new Texture2D(1600, 1000, TextureFormat.RGB24, false); pixels.ReadPixels(new Rect(0, 0, 1600, 1000), 0, 0); pixels.Apply();
            File.WriteAllBytes(args[index + 1], pixels.EncodeToPNG()); RenderTexture.active = previous;
            UnityEngine.Object.DestroyImmediate(pixels); texture.Release(); UnityEngine.Object.DestroyImmediate(texture); UnityEngine.Object.DestroyImmediate(go);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Debug.Log("ADMIN_DATA_STATION_CAPTURE_PASS");
        }
        internal static void Part(GameObject root, string name, Vector3 position, Vector3 size, Material material)
        {
            var part = GameObject.CreatePrimitive(PrimitiveType.Cube); part.name = name;
            part.transform.SetParent(root.transform, false); part.transform.localPosition = position; part.transform.localScale = size;
            UnityEngine.Object.DestroyImmediate(part.GetComponent<Collider>()); part.GetComponent<Renderer>().sharedMaterial = material;
        }
        internal static void Bake(BoxCollider station)
        {
            var layout = CanThoMapSetupTool.ReadLayout();
            var sources = layout.collisions.Select(x => new NavMeshBuildSource { shape = NavMeshBuildSourceShape.Box, size = x.size,
                transform = Matrix4x4.TRS(x.position, Quaternion.Euler(x.euler), Vector3.one), area = x.name == "Campus soil" ? 0 : 1 }).ToList();
            foreach (var device in UnityEngine.Object.FindObjectsByType<DataStation>())
            {
                var box = device.GetComponent<BoxCollider>();
                sources.Add(new NavMeshBuildSource { shape = NavMeshBuildSourceShape.Box, size = box.size,
                    transform = device.transform.localToWorldMatrix * Matrix4x4.Translate(box.center), area = 1 });
            }
            for (int slot = 0; slot < MultiplayerConfig.MaxPlayers; slot++) sources.Add(new NavMeshBuildSource { shape = NavMeshBuildSourceShape.Box,
                size = new Vector3(1.1f, 2, 1.1f), transform = Matrix4x4.Translate(new Vector3(-9 + slot * 6, 1, 10)), area = 1 });
            var settings = NavMesh.GetSettingsByIndex(0); settings.agentRadius = .4f; settings.agentHeight = 2; settings.agentClimb = .4f;
            settings.overrideVoxelSize = true; settings.voxelSize = .2f;
            var nav = AssetDatabase.LoadAssetAtPath<NavMeshData>(CanThoMapSetupTool.NavigationPath);
            if (!NavMeshBuilder.UpdateNavMeshData(nav, settings, sources, new Bounds(layout.bounds.center, layout.bounds.size + new Vector3(4, 8, 4))))
                throw new InvalidOperationException("Station navigation bake failed.");
            EditorUtility.SetDirty(nav);
        }
    }
}
