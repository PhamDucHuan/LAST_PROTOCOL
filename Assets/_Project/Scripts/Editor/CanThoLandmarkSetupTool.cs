using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace LastProtocol.Editor
{
    // Targeted art import: retain unrelated scene objects and their serialized IDs.
    public static class CanThoLandmarkSetupTool
    {
        static bool OldTarget(string name) => name == "NHÀ HỌC A3" || name == "Gym" || name == "Turtle auditorium body" || name.StartsWith("Turtle Hall ");
        static bool NewTarget(string name) => name.StartsWith("Sketchfab A3 wing ") || name == "Sketchfab Sports hall" || name == "Turtle auditorium body" || name == "Turtle Hall roof proxy";
        static bool Matches(Transform t, CanThoMapSetupTool.CollisionBox entry) => t.name == entry.name &&
            Vector3.Distance(t.position,entry.position)<.0001f && Vector3.Distance(t.GetComponent<BoxCollider>().size,entry.size)<.0001f &&
            Quaternion.Angle(t.rotation,Quaternion.Euler(entry.euler))<.001f;
        static string Output()
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"--lp-landmark-output");
            if(i<0 || i+1>=args.Length)throw new ArgumentException("Missing output directory.");
            var path=Path.GetFullPath(args[i+1]);Directory.CreateDirectory(path);return path;
        }
        static Dictionary<Component,string> Snapshot(Scene scene, Transform excluded) => scene.GetRootGameObjects()
            .SelectMany(x=>x.GetComponentsInChildren<Component>(true)).Where(x=>x!=null && !x.transform.IsChildOf(excluded))
            .ToDictionary(x=>x,x=>EditorJsonUtility.ToJson(x));
        public static void BatchApply()
        {
            if(!Application.isBatchMode || !Application.dataPath.Replace('\\','/').Contains("/Builds/ValidationProject/"))
                throw new InvalidOperationException("Apply in the isolated validation project only.");
            var layout=CanThoMapSetupTool.ReadLayout();if(layout.artRevision!=12)throw new InvalidOperationException("Expected landmark revision 12.");
            CanThoMapSetupTool.ConfigureModel(layout);
            var scene=EditorSceneManager.OpenScene(CanThoMapSetupTool.ScenePath,OpenSceneMode.Single);
            var root=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<Transform>()).Single(x=>x.name=="Simple Environment Colliders");
            var environment=root.GetComponentInParent<LevelEnvironment>();
            var before=Snapshot(scene,root);
            var available=root.Cast<Transform>().ToList();var ordered=new List<Transform>();int created=0,removed=0;
            foreach(var entry in layout.collisions)
            {
                var target=available.FirstOrDefault(x=>Matches(x,entry));
                if(target==null && NewTarget(entry.name))
                {
                    target=available.FirstOrDefault(x=>x.name==entry.name);
                    if(target==null){var go=new GameObject(entry.name);target=go.transform;target.SetParent(root,false);go.AddComponent<BoxCollider>();created++;}
                    target.SetPositionAndRotation(entry.position,Quaternion.Euler(entry.euler));target.GetComponent<BoxCollider>().size=entry.size;
                }
                if(target==null)throw new InvalidOperationException("Unrelated collider changed: "+entry.name);
                available.Remove(target);ordered.Add(target);
            }
            foreach(var obsolete in available)
            {
                if(!OldTarget(obsolete.name))throw new InvalidOperationException("Unrelated collider removed: "+obsolete.name);
                UnityEngine.Object.DestroyImmediate(obsolete.gameObject);removed++;
            }
            for(int i=0;i<ordered.Count;i++)ordered[i].SetSiblingIndex(i);
            var after=Snapshot(scene,root);
            if(before.Count!=after.Count || before.Any(x=>!after.TryGetValue(x.Key,out var value)||value!=x.Value))
                throw new InvalidOperationException("Unrelated scene component changed during import.");
            environment.artRevision=12;EditorUtility.SetDirty(environment);
            var sources=layout.collisions.Select(x=>new NavMeshBuildSource{shape=NavMeshBuildSourceShape.Box,size=x.size,
                transform=Matrix4x4.TRS(x.position,Quaternion.Euler(x.euler),Vector3.one),area=x.name=="Campus soil"?0:1}).ToList();
            for(int slot=0;slot<MultiplayerConfig.MaxPlayers;slot++)sources.Add(new NavMeshBuildSource{shape=NavMeshBuildSourceShape.Box,size=new Vector3(1.1f,2,1.1f),transform=Matrix4x4.Translate(new Vector3(-9+slot*6,1,10)),area=1});
            var settings=NavMesh.GetSettingsByIndex(0);settings.agentRadius=.4f;settings.agentHeight=2;settings.agentClimb=.4f;settings.overrideVoxelSize=true;settings.voxelSize=.2f;
            var nav=AssetDatabase.LoadAssetAtPath<NavMeshData>(CanThoMapSetupTool.NavigationPath);
            if(!NavMeshBuilder.UpdateNavMeshData(nav,settings,sources,new Bounds(layout.bounds.center,layout.bounds.size+new Vector3(4,8,4))))throw new InvalidOperationException("NavMesh bake failed.");
            EditorUtility.SetDirty(nav);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();CanThoMapSetupTool.Validate();
            File.WriteAllText(Path.Combine(Output(),"SceneImport.json"),JsonUtility.ToJson(new ImportReport{passed=true,unchangedSceneComponents=before.Count,createdColliders=created,removedColliders=removed,totalColliders=ordered.Count},true));
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            Debug.Log("SKETCHFAB_LANDMARK_UNITY_IMPORT_PASS");
        }
        [Serializable] class ImportReport {public bool passed;public int unchangedSceneComponents,createdColliders,removedColliders,totalColliders;}
        public static void BatchBuild()
        {
            if(!Application.isBatchMode)throw new InvalidOperationException("Batch build only.");
            var directory=Path.GetFullPath("Builds/LandmarkPlayer");Directory.CreateDirectory(directory);
            var scenes=new[]{MultiplayerSetupTool.MenuPath,MultiplayerSetupTool.GamePath,MenuSetupTool.YardPath,CanThoMapSetupTool.ScenePath};
            var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=scenes,locationPathName=Path.Combine(directory,"LAST_PROTOCOL.exe"),target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            if(result.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Landmark player build failed.");
            File.Copy("Assets/_Project/Documentation/CAN_THO_SKETCHFAB_LANDMARKS.md",Path.Combine(directory,"CANTHO_MODEL_CREDITS.txt"),true);
            Debug.Log("SKETCHFAB_LANDMARK_BUILD_PASS "+directory);
        }
        public static void BatchCapture()
        {
            if(!Application.isBatchMode)throw new InvalidOperationException("Batch capture only.");
            EditorSceneManager.OpenScene(CanThoMapSetupTool.ScenePath,OpenSceneMode.Single);
            var go=new GameObject("Landmark inspection camera");var camera=go.AddComponent<UnityEngine.Camera>();camera.enabled=false;camera.farClipPlane=1100;camera.nearClipPlane=.1f;camera.fieldOfView=60;
            RenderSettings.fog=false;
            var eyes=new[]{new Vector3(-23,20,-20),new Vector3(123,18,-53),new Vector3(109,27,93),new Vector3(-45.5f,75,13.5f),new Vector3(98,75,-21.5f),new Vector3(70,100,130)};
            var targets=new[]{new Vector3(-45.5f,3,12.5f),new Vector3(96,2,-18.5f),new Vector3(70,5,130),new Vector3(-45.5f,0,13.51f),new Vector3(98,0,-21.49f),new Vector3(70,0,130.01f)};
            var names=new[]{"HoiTruongRua","NhaHocA3","NhaThiDauTDTT","HoiTruongRua-Plot","NhaHocA3-Plot","NhaThiDauTDTT-Plot"};
            for(int i=0;i<eyes.Length;i++)
            {
                camera.transform.position=eyes[i];camera.transform.LookAt(targets[i]);camera.orthographic=i>=3;camera.orthographicSize=i==5?32:25;
                var texture=new RenderTexture(1500,1000,24);texture.Create();
                for(int frame=0;frame<4;frame++)RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=texture});
                var previous=RenderTexture.active;RenderTexture.active=texture;var pixels=new Texture2D(1500,1000,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1500,1000),0,0);pixels.Apply();
                File.WriteAllBytes(Path.Combine(Output(),names[i]+".png"),pixels.EncodeToPNG());RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(pixels);texture.Release();UnityEngine.Object.DestroyImmediate(texture);
            }
            UnityEngine.Object.DestroyImmediate(go);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);Debug.Log("SKETCHFAB_LANDMARK_CAPTURE_PASS");
        }
        public static void BatchCaptureAndExit()
        {
            BatchCapture();
            var unload=Resources.UnloadUnusedAssets();double earliestExit=Math.Max(EditorApplication.timeSinceStartup+3,120);
            void Finish(){if(!unload.isDone || EditorApplication.isUpdating || EditorApplication.timeSinceStartup<earliestExit)return;EditorApplication.update-=Finish;GC.Collect();EditorApplication.Exit(0);}
            EditorApplication.update+=Finish;
        }
        public static void BatchBuildAndCaptureAndExit()
        {
            BatchBuild();BatchCaptureAndExit();
        }
    }
}
