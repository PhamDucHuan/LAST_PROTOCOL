#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Linq;
using Unity.Netcode.Components;
using UnityEngine;

namespace LastProtocol
{
    public static class InteractionPreviewScenario
    {
        public static IEnumerator Run(Action<bool, string> check, Func<string, IEnumerator> capture)
        {
            var session = MultiplayerSession.Instance;
            foreach (var enemy in UnityEngine.Object.FindObjectsByType<RobotEnemy>()) enemy.DebugPaused = true;
            var player = UnityEngine.Object.FindAnyObjectByType<NetworkPlayerController>();
            var input = player.GetComponent<PlayerInteraction>(); var hud = player.GetComponent<ReviveHUD>();
            var health = player.GetComponent<PlayerHealth>(); var station = UnityEngine.Object.FindObjectsByType<DataStation>().Single(x => x.stationId == DataStation.AdministrationId);
            player.TestInput = Vector2.zero; input.TestFocused = true; input.TestPressed = false;
            Warp(player); yield return new WaitForSecondsRealtime(.5f);
            check(input.HintStation == station && hud.VisibleText.Contains("HOLD E TO ACCESS DATA"), "Terminal proximity shows the shared E prompt");
            check(player.GetComponents<ReviveHUD>().Length == 1 && hud.LocalCanvas != null, "Single shared interaction HUD");
            yield return capture("10-data-station-prompt");
            input.TestPressed = true; yield return new WaitForSecondsRealtime(.9f);
            check(station.Snapshot.Active && hud.ProgressVisible && hud.Progress > .1f && hud.Progress < .8f, "Holding E drives the server channel and visible progress bar");
            yield return capture("11-data-station-progress");
            input.TestPressed = false; yield return new WaitForSecondsRealtime(.3f);
            check(!station.Snapshot.Active && !station.Snapshot.Completed && !hud.ProgressVisible, "Release E cancels and hides progress");
            yield return capture("12-data-station-release");
            input.TestPressed = true; yield return new WaitForSecondsRealtime(.6f);
            health.TryApplyDamageServer(int.MaxValue); yield return new WaitForSecondsRealtime(.3f);
            check(health.Snapshot.IsDowned && !station.Snapshot.Active && !station.Snapshot.Completed, "Being downed cancels data access without completing it");
            input.TestPressed = false;
            check(session.Room.RestartRoundServer(), "Real round reset restores the terminal");
            Warp(player); yield return new WaitForSecondsRealtime(.4f);
            input.TestPressed = true; yield return new WaitForSecondsRealtime(.5f);
            input.TestFocused = false; yield return new WaitForSecondsRealtime(.2f);
            check(!station.Snapshot.Active && !station.Snapshot.Completed, "Losing input focus cancels the held interaction");
            input.TestFocused = true; yield return new WaitForSecondsRealtime(.4f);
            check(!station.Snapshot.Active, "Regaining focus while E remains held cannot restart the action");
            input.TestPressed = false; yield return new WaitForSecondsRealtime(.3f);
            input.TestPressed = true; yield return new WaitForSecondsRealtime(3.5f);
            check(station.Snapshot.Completed && station.Snapshot.Completions == 1 && hud.VisibleText.Contains("DATA SECURED"), "Full hold completes once and shows authoritative success");
            yield return capture("13-data-station-complete");
            yield return new WaitForSecondsRealtime(.4f);
            check(station.Snapshot.Completions == 1 && !input.IsChanneling, "Continuing to hold E does not repeat completion");
            input.TestPressed = false;
        }
        private static void Warp(NetworkPlayerController player)
        {
            var controller = player.GetComponent<CharacterController>(); controller.enabled = false;
            player.GetComponent<NetworkTransform>().Teleport(new Vector3(30, 1.1f, -26), Quaternion.identity, player.transform.localScale);
            player.GetComponent<PlayerMovement>().ResetMotion(); controller.enabled = true;
        }
    }
}
#endif
