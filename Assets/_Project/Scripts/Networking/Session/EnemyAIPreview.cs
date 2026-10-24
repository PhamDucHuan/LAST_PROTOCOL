#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
namespace LastProtocol
{
    public static class EnemyAIPreview
    {
        [System.Serializable] private sealed class Report { public bool complete; public List<string> passed=new(),failures=new(); }
        public static IEnumerator Capture(string directory,string name,Vector3? focus=null)
        {
            Directory.CreateDirectory(directory);var report=new Report();
            var go=new GameObject("AI validation camera",typeof(Camera));var camera=go.GetComponent<Camera>();camera.enabled=false;
            camera.transform.position=new Vector3(58,8,-53);camera.transform.LookAt(new Vector3(58,1,-43));camera.fieldOfView=65;camera.farClipPlane=500;
            if(focus.HasValue){camera.transform.position=focus.Value+new Vector3(0,10,-15);camera.transform.LookAt(focus.Value+Vector3.up);}
            var rotations=new Dictionary<Transform,Quaternion>();
            foreach(var c in Object.FindObjectsByType<Canvas>())if(c.renderMode==RenderMode.WorldSpace&&c.GetComponentInParent<RobotEnemy>()!=null){rotations[c.transform]=c.transform.rotation;c.transform.rotation=camera.transform.rotation;}
            Save(camera,Path.Combine(directory,name+"-World.png"),report);
            foreach(var pair in rotations)if(pair.Key!=null)pair.Key.rotation=pair.Value;
            camera.transform.position=new Vector3(0,0,-1000);camera.transform.rotation=Quaternion.identity;camera.orthographic=true;
            camera.orthographicSize=540;camera.farClipPlane=2000;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.025f,.043f,.065f);camera.cullingMask=1<<30;
            var copies=new List<GameObject>();
            Canvas localInteraction=null;foreach(var h in Object.FindObjectsByType<ReviveHUD>())if(h.IsOwner)localInteraction=h.LocalCanvas;
            foreach(var canvas in Object.FindObjectsByType<Canvas>())
            {
                bool wanted=canvas.renderMode==RenderMode.ScreenSpaceOverlay&&(canvas.GetComponentInParent<MissionHUD>()!=null||canvas.GetComponentInParent<EnemyDirectorHUD>()!=null
                    ||canvas==localInteraction);
                if(!wanted)continue;
                var copy=Object.Instantiate(canvas.gameObject);copies.Add(copy);var c=copy.GetComponent<Canvas>();c.renderMode=RenderMode.WorldSpace;c.worldCamera=camera;
                foreach(var child in copy.GetComponentsInChildren<Transform>(true))child.gameObject.layer=30;
                var rect=(RectTransform)c.transform;rect.position=Vector3.zero;rect.rotation=Quaternion.identity;rect.localScale=Vector3.one;rect.sizeDelta=new Vector2(1920,1080);
            }
            var target=new RenderTexture(1600,900,24);target.Create();camera.targetTexture=target;
            for(int frame=0;frame<8;frame++){Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});yield return null;}
            foreach(var copy in copies)foreach(var text in copy.GetComponentsInChildren<TextMeshProUGUI>())
            {text.ForceMeshUpdate();if(text.isTextOverflowing)report.failures.Add("Text overflow: "+text.text);else if(text.textInfo.characterCount>0)report.passed.Add("Readable: "+text.text);}
            Save(camera,Path.Combine(directory,name+"-HUD.png"),report,target);
            camera.targetTexture=null;target.Release();Object.Destroy(target);foreach(var copy in copies)Object.Destroy(copy);Object.Destroy(go);
            report.complete=true;File.WriteAllText(Path.Combine(directory,name+"-Checks.json"),JsonUtility.ToJson(report,true));
        }
        private static void Save(Camera camera,string path,Report report,RenderTexture target=null)
        {
            bool own=target==null;if(own){target=new RenderTexture(1600,900,24);target.Create();}
            RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
            var previous=RenderTexture.active;RenderTexture.active=target;
            var pixels=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0);pixels.Apply();RenderTexture.active=previous;
            var colors=pixels.GetPixels32();var baseline=colors[0];int visible=0;
            for(int i=0;i<colors.Length&&visible<12;i+=3)if(Mathf.Abs(colors[i].r-baseline.r)+Mathf.Abs(colors[i].g-baseline.g)+Mathf.Abs(colors[i].b-baseline.b)>60)visible++;
            if(visible>=12)report.passed.Add(Path.GetFileName(path)+" rendered content");else report.failures.Add(path+" blank");
            File.WriteAllBytes(path,pixels.EncodeToPNG());Object.Destroy(pixels);if(own){target.Release();Object.Destroy(target);}
        }
    }
}
#endif
