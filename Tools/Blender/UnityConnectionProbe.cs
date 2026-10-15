// Copied ONLY into the disposable validation project by Test-BlenderUnity.ps1.
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LastProtocol.EditorValidation
{
    public static class BlenderConnectionProbe
    {
        [Serializable] private class Observation
        {
            public string name;
            public Vector3 center, size;
            public int vertices, uvCount, materialSlots;
        }
        [Serializable] private class Report
        {
            public string result, unityVersion, error;
            public string[] checks;
            public Observation[] objects;
        }
        public static void Run()
        {
            var args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "--lp-blender-report");
            if (index < 0 || index + 1 >= args.Length) throw new ArgumentException("Missing report path");
            string reportPath = args[index + 1];
            var checks = new System.Collections.Generic.List<string>();
            var report = new Report { unityVersion = Application.unityVersion, result = "FAIL" };
            GameObject instance = null;
            try
            {
                const string path = "Assets/_BlenderBridgeProbe/ConnectionProbe.fbx";
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                Require(importer != null, "FBX recognized by Unity ModelImporter", checks);
                importer.isReadable = true;
                importer.SaveAndReimport();
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Require(model != null, "FBX imported as model", checks);
                instance = UnityEngine.Object.Instantiate(model);
                var renderers = instance.GetComponentsInChildren<MeshRenderer>();
                Require(renderers.Length == 4, "All four meshes imported", checks);
                report.objects = renderers.Select(r =>
                {
                    var mesh = r.GetComponent<MeshFilter>().sharedMesh;
                    return new Observation { name = r.name, center = r.bounds.center, size = r.bounds.size,
                        vertices = mesh.vertexCount, uvCount = mesh.uv.Length, materialSlots = r.sharedMaterials.Length };
                }).ToArray();
                foreach (var obj in report.objects)
                {
                    Require(obj.vertices > 0 && obj.uvCount == obj.vertices, obj.name + ": vertices and UV preserved", checks);
                    Require(obj.materialSlots > 0, obj.name + ": material slot preserved", checks);
                }
                var meter = renderers.Single(r => r.name == "MeterCube");
                var tall = renderers.Single(r => r.name == "TallBox");
                var ground = renderers.Single(r => r.name == "Ground");
                Require(Near(meter.bounds.size, Vector3.one), "1 Blender metre = 1 Unity metre", checks);
                Require(Near(tall.bounds.size, new Vector3(1, 3, 2)), "Blender Z-up converted to Unity Y-up", checks);
                Require(Near(ground.bounds.size, new Vector3(8, 0.2f, 6)), "Ground dimensions preserved", checks);
                Require(Mathf.Abs(ground.bounds.max.y) < 0.01f && Mathf.Abs(tall.bounds.min.y) < 0.01f,
                    "Floor and mesh bases at Unity Y=0", checks);
                var collider = ground.gameObject.AddComponent<MeshCollider>();
                collider.sharedMesh = ground.GetComponent<MeshFilter>().sharedMesh;
                Physics.SyncTransforms();
                Require(collider.Raycast(new Ray(new Vector3(0, 3, 0), Vector3.down), out var hit, 4)
                    && Mathf.Abs(hit.point.y) < 0.01f, "Imported mesh supports ground collision", checks);
                report.result = "PASS";
                Debug.Log("LAST_PROTOCOL_BLENDER_UNITY_PASS " + checks.Count + " checks");
            }
            catch (Exception ex) { report.error = ex.ToString(); Debug.LogException(ex); }
            finally
            {
                if (instance != null) UnityEngine.Object.DestroyImmediate(instance);
                report.checks = checks.ToArray();
                File.WriteAllText(reportPath, JsonUtility.ToJson(report, true));
            }
            EditorApplication.Exit(report.result == "PASS" ? 0 : 1);
        }
        private static bool Near(Vector3 a, Vector3 b) => (a - b).sqrMagnitude < 0.0001f;
        private static void Require(bool condition, string name, System.Collections.Generic.List<string> checks)
        {
            if (!condition) throw new InvalidOperationException(name);
            checks.Add(name);
        }
    }
}
