#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using Unity.Netcode.Components;
using UnityEngine;

namespace LastProtocol
{
    public static class MissionPreviewScenario
    {
        public static IEnumerator Run(Action<bool, string> check, Func<string, IEnumerator> capture)
        {
            var session = MultiplayerSession.Instance;
            var mission = session.Room.GetComponent<NetworkMissionState>();
            var hud = session.Room.GetComponent<MissionHUD>();
            check(mission != null && mission.Definition != null && hud != null && hud.Visible, "Campus mission and local HUD are active");
            if (mission == null || mission.Definition == null || hud == null) yield break;
            foreach (var enemy in UnityEngine.Object.FindObjectsByType<RobotEnemy>()) enemy.DebugPaused = true;
            var player = UnityEngine.Object.FindAnyObjectByType<NetworkPlayerController>();
            player.TestInput = Vector2.zero;
            player.GetComponent<PlayerHealth>().TryApplyDamageServer(int.MaxValue);
            float deadline = Time.realtimeSinceStartup + 4;
            while (mission.Snapshot.Phase != MissionPhase.Failed && Time.realtimeSinceStartup < deadline) yield return null;
            check(mission.Snapshot.Phase == MissionPhase.Failed && hud.ObjectiveText == "RECON FAILED", "Mission HUD shows authoritative wipe failure");
            check(session.Room.RestartRoundServer(), "Host restarts mission through existing round reset");
            for (int index = 0; index < mission.Definition.objectives.Length; index++)
            {
                var target = mission.Definition.objectives[index];
                var controller = player.GetComponent<CharacterController>(); controller.enabled = false;
                player.GetComponent<NetworkTransform>().Teleport(target.center + Vector3.up * 1.1f, Quaternion.identity, player.transform.localScale);
                player.GetComponent<PlayerMovement>().ResetMotion(); controller.enabled = true;
                for (int frame = 0; frame < 3; frame++) yield return null;
                check(hud.ObjectiveText == target.title, "Mission HUD displays objective " + (index + 1));
                if (index == 0) yield return capture("10-mission-active");
                deadline = Time.realtimeSinceStartup + 5;
                while (mission.Snapshot.ObjectiveIndex == index && Time.realtimeSinceStartup < deadline) yield return null;
                check(mission.Snapshot.ObjectiveIndex == index + 1, "Server advances rally objective " + (index + 1));
            }
            check(mission.Snapshot.Phase == MissionPhase.Completed && hud.ObjectiveText == "RECON COMPLETE", "Mission result reaches HUD after the final rally");
            check(session.Room.GetComponents<MissionHUD>().Length == 1, "Exactly one mission HUD after restart");
            yield return capture("11-mission-complete");
        }
    }
}
#endif
