#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Linq;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

namespace LastProtocol
{
    // Explicit command-line validation only. Never runs during normal play.
    public static class CampusRoutePreview
    {
        public static IEnumerator Run(string directory, Action<bool, string> check)
        {
            var enemy = UnityEngine.Object.FindAnyObjectByType<RobotEnemy>();
            var localCamera = UnityEngine.Object.FindAnyObjectByType<LocalPlayerCamera>();
            var environment = UnityEngine.Object.FindAnyObjectByType<LevelEnvironment>();
            if (enemy == null || localCamera == null || environment == null) { check(false, "Campus runtime fixtures available"); yield break; }
            var routeRoot = environment.transform.Cast<Transform>().SingleOrDefault(x => x.name == "Playable Campus Route");
            if (routeRoot == null || routeRoot.childCount < 6) { check(false, "Campus route waypoints available"); yield break; }
            var route = routeRoot.Cast<Transform>().Select(x => x.position).ToArray();
            bool paused = enemy.DebugPaused; enemy.DebugPaused = true;
            var agentObject = new GameObject("Campus route validation agent");
            agentObject.transform.position = route[0];
            var agent = agentObject.AddComponent<NavMeshAgent>();
            agent.radius = enemy.Agent.radius; agent.height = enemy.Agent.height; agent.agentTypeID = enemy.Agent.agentTypeID;
            agent.speed = 16; agent.acceleration = 50; agent.angularSpeed = 720; agent.stoppingDistance = .1f;
            try
            {
                check(agent.isOnNavMesh, "Campus route agent begins on navigation");
                if (agent.isOnNavMesh)
                    for (int i = 1; i < route.Length; i++)
                    {
                        bool requested = agent.SetDestination(route[i]); float deadline = Time.realtimeSinceStartup + 8;
                        while (Time.realtimeSinceStartup < deadline && (agent.pathPending || Vector3.Distance(agent.transform.position, route[i]) > .3f)) yield return null;
                        check(requested && !agent.pathPending && Vector3.Distance(agent.transform.position, route[i]) <= .3f, "Enemy-sized NavMeshAgent traverses campus route leg " + i);
                    }
                var neighborhood = environment.transform.Find("Playable Neighborhood Route");
                if (neighborhood != null && agent.isOnNavMesh)
                {
                    check(agent.Warp(neighborhood.GetChild(0).position), "Neighborhood agent begins at campus gate");
                    agent.speed = 50; agent.acceleration = 150;
                    for (int i = 1; i < neighborhood.childCount; i++)
                    {
                        var destination = neighborhood.GetChild(i).position;
                        bool requested = agent.SetDestination(destination); float deadline = Time.realtimeSinceStartup + 12;
                        while (Time.realtimeSinceStartup < deadline && (agent.pathPending || Vector3.Distance(agent.transform.position, destination) > .3f)) yield return null;
                        check(requested && !agent.pathPending && Vector3.Distance(agent.transform.position, destination) <= .3f,
                            "Enemy-sized NavMeshAgent traverses neighborhood route leg " + i);
                    }
                }
            }
            finally { UnityEngine.Object.Destroy(agentObject); enemy.DebugPaused = paused; }

            var rig = localCamera.virtualCamera; var follow = localCamera.follow; var previous = rig.Follow;
            var target = new GameObject("Campus camera runtime probe"); var camera = Camera.main;
            var brain = camera.GetComponent<CinemachineBrain>(); bool brainEnabled = brain.enabled;
            bool localEnabled = localCamera.enabled; localCamera.enabled = false; brain.enabled = false;
            try
            {
                rig.Follow = target.transform;
                var probes = new[] { new Vector4(32,1.6f,-15,180), new Vector4(57,1.6f,-40,180), new Vector4(35,1.6f,-47,0) };
                if (environment.transform.Find("Playable Neighborhood Route") != null)
                {
                    var authored = environment.transform.Find("Neighborhood Camera Probe");
                    var probe = authored == null ? new Vector4(-243,1.6f,-83.5f,0) :
                        new Vector4(authored.position.x,authored.position.y,authored.position.z,authored.eulerAngles.y);
                    probes = probes.Concat(new[] { probe }).ToArray();
                }
                for (int i = 0; i < probes.Length; i++)
                {
                    var p = probes[i]; target.transform.SetPositionAndRotation(new Vector3(p.x,p.y,p.z), Quaternion.Euler(0,p.w,0));
                    rig.PreviousStateIsValid = false; Physics.SyncTransforms(); rig.InternalUpdateCameraState(Vector3.up, -1);
                    var state = rig.State; var position = state.GetFinalPosition();
                    check(follow.CurrentObstacle != null && !Physics.CheckSphere(position,.2f,1,QueryTriggerInteraction.Ignore), "Runtime camera avoids campus wall " + i);
                    camera.transform.SetPositionAndRotation(position,state.GetFinalOrientation());
                    yield return null;
                    Capture(camera, Path.Combine(directory, "Campus-Camera-Wall-" + i + ".png"));
                }
            }
            finally
            {
                rig.Follow = previous; rig.PreviousStateIsValid = false;
                localCamera.enabled = localEnabled; brain.enabled = brainEnabled; UnityEngine.Object.Destroy(target);
            }
            for (int i = 0; i < 8; i++) yield return null;
        }
        private static void Capture(Camera camera, string path)
        {
            var destination = new RenderTexture(1600, 900, 24); destination.Create();
            var previous = RenderTexture.active; var image = new Texture2D(1600,900,TextureFormat.RGB24,false);
            try
            {
                RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest { destination=destination });
                RenderTexture.active=destination; image.ReadPixels(new Rect(0,0,1600,900),0,0); image.Apply();
                File.WriteAllBytes(path,image.EncodeToPNG());
            }
            finally { RenderTexture.active=previous; UnityEngine.Object.Destroy(image); destination.Release(); UnityEngine.Object.Destroy(destination); }
        }
    }
}
#endif
