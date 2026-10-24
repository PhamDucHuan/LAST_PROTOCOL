#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Linq;
using Unity.Netcode.Components;
using UnityEngine;

namespace LastProtocol
{
    public static class UplinkPreviewScenario
    {
        public static IEnumerator Run(Action<bool,string> check, Func<string,bool,IEnumerator> capture, Action captureDefenceScene = null)
        {
            var session = MultiplayerSession.Instance; var mission = session.Room.GetComponent<NetworkMissionState>();
            var missionHud = session.Room.GetComponent<MissionHUD>();
            var player = UnityEngine.Object.FindAnyObjectByType<NetworkPlayerController>();
            var input = player.GetComponent<PlayerInteraction>(); var hud = player.GetComponent<ReviveHUD>();
            foreach (var enemy in UnityEngine.Object.FindObjectsByType<RobotEnemy>()) enemy.DebugPaused = true;
            player.TestInput = Vector2.zero; input.TestFocused = true; input.TestPressed = false;
            var nodes = UnityEngine.Object.FindObjectsByType<DataStation>();
            var uplink = nodes.Single(x => x.stationId == DataStation.UplinkId);
            Warp(player, uplink.transform.position + new Vector3(0,1.1f,-2));
            yield return new WaitForSecondsRealtime(.5f);
            check(hud.VisibleText.Contains("LOCKED"), "Uplink explains the unmet prerequisite");
            input.TestPressed = true; yield return new WaitForSecondsRealtime(.6f);
            check(!uplink.Snapshot.Active, "Shared E cannot start an out-of-order uplink");
            input.TestPressed = false;
            Warp(player, new Vector3(0,1.1f,-40)); yield return new WaitForSecondsRealtime(.4f);
            yield return capture("20-uplink-rally",false);
            yield return new WaitForSecondsRealtime(2.3f);
            check(mission.Snapshot.ObjectiveIndex == 1, "Rally unlocks the three-record objective");
            string[] ids = { DataStation.AdministrationId, "CTU_ADMIN_DATA_02", "CTU_ADMIN_DATA_03", DataStation.GeneratorId, DataStation.UplinkId };
            for (int i = 0; i < ids.Length; i++)
            {
                var station = nodes.Single(x => x.stationId == ids[i]);
                input.TestPressed = false; Warp(player,station.transform.position + new Vector3(0,1.1f,-2));
                yield return new WaitForSecondsRealtime(.5f);
                check(station.Available && hud.VisibleText.Contains("HOLD E TO " + station.ActionText), "Available device shows action: " + ids[i]);
                if (i == 0 || i >= 3) yield return capture("21-device-prompt-" + i,true);
                input.TestPressed = true; yield return new WaitForSecondsRealtime(.8f);
                check(station.Snapshot.Active && hud.ProgressVisible && hud.Progress > .05f, "Shared E starts authoritative progress: " + ids[i]);
                if (i == 0 || i >= 3) yield return capture("22-device-progress-" + i,true);
                if (i == 3 || i == 4)
                {
                    input.TestPressed = false; yield return new WaitForSecondsRealtime(.3f);
                    if (i == 3)
                        check(!station.Snapshot.Active && !station.Snapshot.Completed && mission.Snapshot.ObjectiveIndex == 2, "Generator release cancels without powering the uplink");
                    else
                        check(!station.Snapshot.Active && !station.Snapshot.Completed && session.Room.GetComponent<UplinkEncounter>().Snapshot.Activations == 1
                            && UnityEngine.Object.FindObjectsByType<RobotEnemy>().Length == 3, "Releasing uplink E cancels the channel without spawning a second wave");
                    input.TestPressed = true;
                }
                yield return new WaitForSecondsRealtime(station.holdSeconds + .5f);
                check(station.Snapshot.Completed && station.Snapshot.Completions == 1 && hud.VisibleText.Contains(station.SuccessText), "Device completes once: " + ids[i]);
                if (i == 3)
                {
                    var defence = session.Room.GetComponent<UplinkEncounter>();
                    check(defence != null && defence.Snapshot.Spawned == 3 && defence.Snapshot.Activations == 1, "Generator activates one three-robot defence wave");
                    // This preview isolates UI; live AI and defender shooting are covered by the network fixture.
                    foreach (var robot in UnityEngine.Object.FindObjectsByType<RobotEnemy>()) robot.DebugPaused = true;
                    yield return capture("25-uplink-defence-active",false);
                    captureDefenceScene?.Invoke();
                }
                yield return capture("23-mission-after-device-" + i,false);
            }
            input.TestPressed = false; yield return new WaitForSecondsRealtime(.4f);
            check(mission.Snapshot.Phase == MissionPhase.Completed && mission.Snapshot.CompletedStations == 31 && missionHud.ObjectiveText == "COMMS RESTORED", "Local rescue communications restored on mission HUD");
            check(session.Room.GetComponents<MissionHUD>().Length == 1 && player.GetComponents<ReviveHUD>().Length == 1, "One mission HUD and one shared E HUD throughout the segment");
            check(UnityEngine.Object.FindObjectsByType<RobotEnemy>().Length == 0 && session.Room.GetComponent<UplinkEncounter>().Snapshot.Phase == UplinkEncounterPhase.Secured,
                "Restoring communications removes surviving mission robots");
            yield return capture("24-uplink-restored",true);
        }
        private static void Warp(NetworkPlayerController player, Vector3 position)
        {
            var controller = player.GetComponent<CharacterController>(); controller.enabled = false;
            player.GetComponent<NetworkTransform>().Teleport(position,Quaternion.identity,player.transform.localScale);
            player.GetComponent<PlayerMovement>().ResetMotion(); controller.enabled = true;
        }
    }
}
#endif
