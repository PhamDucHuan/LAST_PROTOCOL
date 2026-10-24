#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace LastProtocol
{
    public static class CheckpointChipPreviewScenario
    {
        public static IEnumerator Run(Action<bool, string> check, Func<string, bool, IEnumerator> capture)
        {
            var session = MultiplayerSession.Instance; var room = session.Room;
            var mission = room.GetComponent<NetworkMissionState>(); var chip = room.GetComponent<NetworkChip>();
            var players = UnityEngine.Object.FindObjectsByType<NetworkPlayerController>();
            var local = players.Single(x => x.IsOwner); var other = players.Single(x => !x.IsOwner);
            var input = local.GetComponent<PlayerInteraction>(); var hud = local.GetComponent<ReviveHUD>();
            input.TestFocused = true; input.TestPressed = false; local.TestInput = Vector2.zero;
            check(chip.IsCarrier(other.NetworkObjectId), "Remote client may be configured as initial chip carrier");
            CheckpointChipSmokeScenario.Warp(local, new Vector3(-1, 1.1f, -40));
            CheckpointChipSmokeScenario.Warp(other, new Vector3(1, 1.1f, -40));
            yield return new WaitForSecondsRealtime(2.8f);
            var nodes = UnityEngine.Object.FindObjectsByType<DataStation>();
            foreach (string id in new[] { DataStation.AdministrationId, "CTU_ADMIN_DATA_02", "CTU_ADMIN_DATA_03" })
            {
                var node = nodes.Single(x => x.stationId == id);
                input.TestPressed = false; CheckpointChipSmokeScenario.Warp(local, node.transform.position + new Vector3(0, 1.1f, -2));
                yield return new WaitForSecondsRealtime(.4f); input.TestPressed = true;
                yield return new WaitForSecondsRealtime(3.5f);
                check(node.Snapshot.Completed && node.Snapshot.Completions == 1, "Preview confirms record once: " + id);
            }
            input.TestPressed = false; CheckpointChipSmokeScenario.Warp(other, chip.drillPosition);
            CheckpointChipSmokeScenario.Warp(local, chip.drillPosition + Vector3.right * .9f);
            yield return new WaitForSecondsRealtime(.4f);
            check(chip.DrillPending && chip.BlocksGenerator, "Relay tutorial gates the generator in production");
            yield return capture("30-relay-tutorial", false);
            chip.TestStage.Value = 1;
            yield return new WaitForSecondsRealtime(2.4f);
            chip.TestStage.Value = 2;
            check(other.GetComponent<PlayerHealth>().Snapshot.IsDowned && chip.Snapshot.Location == ChipLocation.Dropped, "Emergency drill downs the remote carrier and ejects the only chip");
            yield return new WaitForSecondsRealtime(.4f);
            check(input.HintChip != null && input.HintRevive == null && hud.VisibleText.Contains("SECURE CONTROL CHIP"), "Chip recovery takes priority beside the downed carrier");
            yield return capture("31-chip-recovery-prompt", true);
            input.TestPressed = true; yield return new WaitForSecondsRealtime(.65f);
            check(hud.ProgressVisible && chip.Snapshot.Active && hud.VisibleText.Contains("SECURING CONTROL CHIP"), "Shared E shows recovery progress");
            yield return capture("32-chip-recovery-progress", true);
            input.TestPressed = false; yield return new WaitForSecondsRealtime(.35f);
            check(!chip.Snapshot.Active && chip.Snapshot.Location == ChipLocation.Dropped, "Release cancels recovery without creating another chip");
            input.TestPressed = true; yield return new WaitForSecondsRealtime(2.5f);
            check(chip.IsCarrier(local.NetworkObjectId) && chip.Snapshot.Transfers == 1 && chip.Snapshot.TutorialComplete, "Recovered chip belongs to the teammate and tutorial completes once");
            yield return new WaitForSecondsRealtime(.5f);
            check(other.GetComponent<PlayerHealth>().Snapshot.IsDowned && !local.GetComponent<PlayerRevive>().IsChanneling, "Continuing to hold recovery E cannot also revive");
            input.TestPressed = false; yield return new WaitForSecondsRealtime(.35f);
            yield return capture("33-chip-transferred-revive-next", true);
            input.TestPressed = true; yield return new WaitForSecondsRealtime(.7f);
            check(local.GetComponent<PlayerRevive>().IsChanneling && hud.VisibleText.Contains("REVIVING"), "A fresh E gesture starts revive");
            yield return capture("34-revive-after-transfer", true);
            yield return new WaitForSecondsRealtime(3.1f); input.TestPressed = false;
            check(other.GetComponent<PlayerHealth>().CanAct && chip.IsCarrier(local.NetworkObjectId), "Reviving the original carrier does not reclaim the chip");
            foreach (var p in players) p.GetComponent<PlayerHealth>().TryApplyDamageServer(int.MaxValue);
            yield return new WaitForSecondsRealtime(.5f);
            check(mission.Snapshot.Phase == MissionPhase.Failed && hud.VisibleText.Contains("Confirmed data retained"), "Failure offers the saved checkpoint and explains retained data");
            yield return capture("35-checkpoint-retry", true);
            check(room.RestartRoundServer(), "Host can retry the checkpoint");
            yield return new WaitForSecondsRealtime(.6f);
            check(mission.Snapshot.Attempt == 2 && mission.Snapshot.CompletedStations == 7 && mission.Snapshot.ObjectiveIndex == 2
                && chip.IsCarrier(local.NetworkObjectId) && chip.Snapshot.TutorialComplete && nodes.Where(x => x.stationId.StartsWith("CTU_ADMIN_DATA_")).All(x => x.Snapshot.Completions == 1),
                "Checkpoint restores data, tutorial and transferred carrier without duplicate completion");
            yield return capture("36-checkpoint-restored", false);
            input.TestFocused = null; input.TestPressed = null;
        }
    }
}
#endif
