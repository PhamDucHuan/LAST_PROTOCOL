#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Linq;
using Unity.Netcode.Components;
using UnityEngine;

namespace LastProtocol
{
    [Serializable] public sealed class CheckpointChipSmokeReport
    {
        public uint checks;
        public int checkpointNotifications;
        public bool passed, clientWritesDenied, sawDrop, sawRecovery, sawDrill, sawRevive, sawRetry, carrierIsRemote;
        public string finalState, error;
    }
    public sealed class CheckpointChipSmokeScenario
    {
        public const uint Expected = uint.MaxValue;
        public readonly CheckpointChipSmokeReport Report = new();
        private int previous = -1, localStage = -1;
        private float at, localAt, nextSend;
        private bool sawChannel, damageSent;
        private GameObject blocker;
        private string waveIds;
        private ulong winner;
        private static readonly string[] Ids = { DataStation.AdministrationId, "CTU_ADMIN_DATA_02", "CTU_ADMIN_DATA_03", DataStation.GeneratorId, DataStation.UplinkId };
        public void Tick(MultiplayerSession session, NetworkPlayerController[] players, float time)
        {
            if (session?.Room == null || time < 5 || players.Length < 2) return;
            var mission = session.Room.GetComponent<NetworkMissionState>(); var chip = session.Room.GetComponent<NetworkChip>();
            var encounter = session.Room.GetComponent<UplinkEncounter>();
            if (chip == null || !chip.EnabledForMission || !mission.UsesCheckpoints) { Fail("Production checkpoint/chip missing"); return; }
            var all = UnityEngine.Object.FindObjectsByType<DataStation>();
            if (all.Length != 5 || all.Any(x => !x.IsSpawned) || chip.Snapshot.Location == ChipLocation.Inactive) return;
            var nodes = Ids.Select(id => all.Single(x => x.stationId == id)).ToArray();
            var ordered = players.OrderBy(x => session.Room.GetSlot(x.OwnerClientId)).ToArray();
            var original = ordered[0]; var operatorPlayer = ordered[ordered.Length - 1];
            var c = chip.Snapshot; var s = mission.Snapshot; int stage = chip.TestStage.Value;
            var enemies = UnityEngine.Object.FindObjectsByType<RobotEnemy>();
            Report.checks = chip.TestChecks.Value;
            Report.checkpointNotifications = mission.TestCheckpointNotifications;
            Report.sawDrop |= c.Location == ChipLocation.Dropped;
            Report.sawRecovery |= c.Active && c.Action == InteractionKind.RecoverChip;
            Report.sawDrill |= c.Active && c.Action == InteractionKind.RelayDrill;
            Report.sawRevive |= players.Any(x => x.GetComponent<PlayerRevive>().IsChanneling);
            Report.sawRetry |= s.Attempt >= 2 && s.CompletedStations == 1;
            if (c.Location == ChipLocation.Carried && c.Transfers == 1) { winner = c.Carrier; Report.carrierIsRemote |= c.Carrier != original.NetworkObjectId; }
            if (!session.IsHost) Report.clientWritesDenied = !chip.TestTryClientWrite() && !mission.TestTryClientWrite() && !session.Room.RestartRoundServer();
            if (stage >= 30 && s.Attempt == 5 && s.Phase == MissionPhase.Completed && nodes.All(x => x.Snapshot.Completed && x.Snapshot.Completions == 1))
                Report.finalState = $"{s.Attempt}:{s.Phase}:{s.CompletedStations}:{c.Carrier}:{c.Transfers}:{c.TutorialComplete}:"
                    + string.Join(";", nodes.Select(x => $"{x.stationId}={x.Snapshot.Completed},{x.Snapshot.Completions},{x.Snapshot.Active}"));
            Report.passed = Report.checks == Expected && Report.sawDrop && Report.sawRecovery && Report.sawDrill && Report.sawRevive
                && Report.sawRetry && Report.carrierIsRemote && !string.IsNullOrEmpty(Report.finalState)
                && (session.IsHost || Report.clientWritesDenied && Report.checkpointNotifications >= 5);
            if (stage == 33 || Report.error != null) return;
            var currentCarrier = ordered.FirstOrDefault(x => x.NetworkObjectId == c.Carrier);
            var relayWinner = ordered.FirstOrDefault(x => x.NetworkObjectId == winner) ?? operatorPlayer;
            int nodeIndex = stage switch { 4 or 6 => 1, 7 => 2, 8 or 20 => 3, >= 21 => 4, _ => 0 };
            var node = nodes[nodeIndex];
            if (session.IsHost)
            {
                foreach (var robot in enemies) robot.DebugPaused = true;
                if (stage == 0) { chip.TestStage.Value = 1; return; }
                if (previous != stage)
                {
                    previous = stage; at = time; sawChannel = damageSent = false;
                    if (blocker != null) UnityEngine.Object.Destroy(blocker);
                    foreach (var p in ordered) p.GetComponent<PlayerInteraction>().ResetRoundServer();
                    for (int i = 0; i < ordered.Length; i++) Warp(ordered[i], new Vector3(28 + i * .8f, 1.1f, -29));
                    if (stage == 1) for (int i = 0; i < ordered.Length; i++) Warp(ordered[i], new Vector3(i - 1, 1.1f, -40));
                    if (stage == 2 || stage == 5 || stage == 6 || stage == 7)
                        for (int i = 0; i < ordered.Length; i++) Warp(ordered[i], node.transform.position + new Vector3((i - 1.5f) * .7f, 1.1f, -2));
                    if (stage == 4) Warp(operatorPlayer, nodes[1].transform.position + new Vector3(0, 1.1f, -2));
                    if (stage == 8)
                    {
                        Warp(original, chip.drillPosition);
                        for (int i = 1; i < ordered.Length; i++) Warp(ordered[i], nodes[3].transform.position + new Vector3((i - 1) * .8f, 1.1f, -2));
                    }
                    if (stage >= 9 && stage <= 19 || stage == 32)
                    {
                        Warp(original, chip.drillPosition);
                        // Three distinct clear approaches, all inside the 2.5 m recovery radius.
                        for (int i = 1; i < ordered.Length; i++) Warp(ordered[i], chip.drillPosition + (i switch
                        { 1 => new Vector3(1.05f, 0, -.7f), 2 => new Vector3(-1.05f, 0, -.7f), _ => new Vector3(0, 0, -1.5f) }));
                    }
                    if (stage == 11) Warp(operatorPlayer, chip.drillPosition + Vector3.right * 9);
                    if (stage == 12)
                    {
                        Warp(operatorPlayer, chip.drillPosition + Vector3.right * 2);
                        blocker = GameObject.CreatePrimitive(PrimitiveType.Cube); blocker.name = "Chip LOS test cover";
                        blocker.transform.position = chip.drillPosition + new Vector3(1, .2f, 0); blocker.transform.localScale = new Vector3(.15f, 3, 3);
                    }
                    if (stage == 20) Warp(relayWinner, nodes[3].transform.position + new Vector3(0, 1.1f, -2));
                    if (stage == 21) Warp(original, nodes[4].transform.position + new Vector3(0, 1.1f, -2));
                    if (stage == 22 || stage == 23 || stage == 27 || stage == 28)
                    { if (currentCarrier != null) Warp(currentCarrier, nodes[4].transform.position + new Vector3(0, 1.1f, -2)); }
                    if (stage == 22) waveIds = string.Join(",", enemies.Select(x => x.NetworkObjectId).OrderBy(x => x));
                    if (stage == 24) Warp(original, c.DropPosition + new Vector3(.9f, 0, -.3f));
                    if (stage == 26) Warp(original, nodes[4].transform.position + new Vector3(0, 1.1f, -2));
                    if (stage == 3 || stage == 25 || stage == 29 || stage == 31)
                        foreach (var p in ordered) p.GetComponent<PlayerHealth>().TryApplyDamageServer(int.MaxValue);
                    Physics.SyncTransforms();
                }
                float elapsed = time - at;
                sawChannel |= c.Active || nodes[4].Snapshot.Active;
                if ((stage == 15 || stage == 23) && elapsed > .8f && !damageSent)
                {
                    damageSent = true;
                    (stage == 15 ? operatorPlayer : relayWinner).GetComponent<PlayerHealth>().TryApplyDamageServer(stage == 15 ? 10 : int.MaxValue);
                }
                float wait = stage switch
                {
                    1 => 2.8f, 2 or 6 or 7 => 4.3f, 10 => 2.3f, 17 or 24 => 2.9f, 19 => 3.9f,
                    20 => 4.9f, 27 => 5.9f, 9 or 13 or 14 or 15 or 18 or 22 or 23 => 2.2f,
                    30 => 2.5f, 32 => 2.5f, _ => 1.6f
                };
                if (elapsed > wait)
                {
                    bool recordsOnce = nodes.Take(3).All(x => x.Snapshot.Completed && x.Snapshot.Completions == 1);
                    bool ok = stage switch
                    {
                        1 => s.ObjectiveIndex == 1 && c.Carrier == original.NetworkObjectId,
                        2 => s.CompletedStations == 1 && nodes[0].Snapshot.Completions == 1 && mission.Checkpoint.CompletedStations == 1,
                        3 => s.Phase == MissionPhase.Failed && session.Room.RestartRoundServer(),
                        4 => s.Attempt == 2 && s.CompletedStations == 1 && nodes[0].Snapshot.Completed && !nodes[1].Snapshot.Active
                            && !c.Active && ordered.All(x => x.GetComponent<PlayerHealth>().CanAct),
                        5 => s.CompletedStations == 1 && nodes[0].Snapshot.Completions == 1 && !nodes[0].Snapshot.Active,
                        6 => s.CompletedStations == 3,
                        7 => s.ObjectiveIndex == 2 && s.CompletedStations == 7 && recordsOnce && chip.BlocksGenerator,
                        8 => !c.Active && !c.DrillStarted && !nodes[3].Snapshot.Active && s.CompletedStations == 7,
                        9 => sawChannel && !c.Active && c.Location == ChipLocation.Carried && original.GetComponent<PlayerHealth>().CanAct,
                        10 => c.Location == ChipLocation.Dropped && c.DrillStarted && original.GetComponent<PlayerHealth>().Snapshot.IsDowned,
                        11 or 12 or 16 => !c.Active && c.Location == ChipLocation.Dropped && c.Transfers == 0,
                        13 or 14 or 15 => sawChannel && !c.Active && c.Location == ChipLocation.Dropped && c.Transfers == 0,
                        17 => c.Location == ChipLocation.Carried && c.Carrier != original.NetworkObjectId && c.Transfers == 1 && c.TutorialComplete
                            && original.GetComponent<PlayerHealth>().Snapshot.IsDowned && !ordered.Any(x => x.GetComponent<PlayerRevive>().IsChanneling),
                        18 => original.GetComponent<PlayerHealth>().Snapshot.IsDowned && !ordered.Any(x => x.GetComponent<PlayerRevive>().IsChanneling),
                        19 => original.GetComponent<PlayerHealth>().CanAct && c.Carrier == winner && c.Transfers == 1,
                        20 => s.CompletedStations == 15 && s.ObjectiveIndex == 3 && encounter.Snapshot.Spawned == 3 && mission.Checkpoint.CompletedStations == 15,
                        21 => !nodes[4].Snapshot.Active && !nodes[4].Snapshot.Completed && c.Carrier == winner,
                        22 => sawChannel && !nodes[4].Snapshot.Active && !nodes[4].Snapshot.Completed && enemies.Length == 3
                            && waveIds == string.Join(",", enemies.Select(x => x.NetworkObjectId).OrderBy(x => x)) && encounter.Snapshot.Activations == 1,
                        23 => sawChannel && !nodes[4].Snapshot.Active && c.Location == ChipLocation.Dropped && relayWinner.GetComponent<PlayerHealth>().Snapshot.IsDowned,
                        24 => c.Carrier == original.NetworkObjectId && c.Transfers == 2 && c.TutorialComplete,
                        25 => s.Phase == MissionPhase.Failed && enemies.Length == 0 && session.Room.RestartRoundServer(),
                        26 => s.Attempt == 4 && s.ObjectiveIndex == 3 && s.CompletedStations == 15 && recordsOnce && nodes[3].Snapshot.Completions == 1
                            && !nodes[4].Snapshot.Active && !nodes[4].Snapshot.Completed && c.TutorialComplete && c.Carrier == original.NetworkObjectId
                            && c.Transfers == 2 && enemies.Length == 3 && encounter.Snapshot.Activations == 1 && ordered.All(x => x.GetComponent<PlayerHealth>().CanAct),
                        27 or 28 => s.Phase == MissionPhase.Completed && s.CompletedStations == 31 && nodes.All(x => x.Snapshot.Completed && x.Snapshot.Completions == 1)
                            && enemies.Length == 0 && encounter.Snapshot.Phase == UplinkEncounterPhase.Secured,
                        29 => session.Room.TeamWiped.Value && session.Room.RestartRoundServer(),
                        30 => s.Attempt == 5 && s.Phase == MissionPhase.Completed && s.CompletedStations == 31 && c.Transfers == 2 && c.TutorialComplete
                            && nodes.All(x => x.Snapshot.Completions == 1 && !x.Snapshot.Active) && enemies.Length == 0 && ordered.All(x => x.GetComponent<PlayerHealth>().CanAct),
                        31 => s.Phase == MissionPhase.Failed && mission.Checkpoint.CompletedStations == 7 && session.Room.RestartRoundServer(),
                        32 => s.Attempt == 3 && s.CompletedStations == 7 && recordsOnce && c.Location == ChipLocation.Dropped
                            && c.Transfers == 0 && !c.TutorialComplete && c.DrillStarted && original.GetComponent<PlayerHealth>().Snapshot.IsDowned,
                        _ => false
                    };
                    if (!ok) { Fail($"Checkpoint/chip stage {stage}: mission={s.Phase}/{s.ObjectiveIndex}/{s.CompletedStations}/attempt{s.Attempt} chip={c.Location}/{c.Active}/{c.Transfers} wave={encounter.Snapshot.Phase}/{enemies.Length} saw={sawChannel}"); return; }
                    chip.TestChecks.Value |= 1u << (stage - 1);
                    chip.TestStage.Value = stage switch { 10 => 31, 32 => 11, 30 => 33, _ => stage + 1 };
                }
            }
            if (stage != localStage)
            {
                localStage = stage; localAt = time; nextSend = time + .3f;
                foreach (var p in ordered) if (p.IsOwner && stage != 18) p.GetComponent<PlayerInteraction>().TestRelease();
            }
            if (time < nextSend) return; nextSend = time + .1f;
            foreach (var p in ordered)
            {
                if (!p.IsOwner) continue;
                var input = p.GetComponent<PlayerInteraction>(); float elapsed = time - localAt;
                if (stage == 4 && p == operatorPlayer) { input.TestHoldOldAttempt(InteractionKind.DataStation, nodes[1].NetworkObjectId, 1); continue; }
                if (stage == 26 && p == original) { input.TestHoldOldAttempt(InteractionKind.DataStation, nodes[4].NetworkObjectId, 3); continue; }
                if (stage == 2 || stage == 5 || (stage == 6 || stage == 7) && p == operatorPlayer)
                    input.TestHold(InteractionKind.DataStation, node.NetworkObjectId);
                if (stage == 8)
                { if (p == original) input.TestHold(InteractionKind.RelayDrill, chip.NetworkObjectId); else input.TestHold(InteractionKind.DataStation, nodes[3].NetworkObjectId); }
                if ((stage == 9 || stage == 10 || stage == 32) && p == original)
                { if (stage == 9 && elapsed > .75f) input.TestRelease(); else input.TestHold(InteractionKind.RelayDrill, chip.NetworkObjectId); }
                if (stage >= 11 && stage <= 15 && p == operatorPlayer)
                {
                    if (stage == 13 && elapsed > .8f) input.TestRelease();
                    else if (stage != 14 || elapsed < .7f) input.TestHold(InteractionKind.RecoverChip, chip.NetworkObjectId);
                }
                if (stage == 16 && p == original || stage == 17 && p != original || stage == 24 && p == original)
                    input.TestHold(InteractionKind.RecoverChip, chip.NetworkObjectId);
                if (stage == 18 && p.NetworkObjectId == winner || stage == 19 && p.NetworkObjectId == winner)
                    input.TestHold(InteractionKind.Revive, original.NetworkObjectId);
                if (stage == 20 && p.NetworkObjectId == winner) input.TestHold(InteractionKind.DataStation, nodes[3].NetworkObjectId);
                if (stage == 21 && p == original || (stage == 22 || stage == 23) && p.NetworkObjectId == winner
                    || (stage == 27 || stage == 28) && p == original)
                { if (stage == 22 && elapsed > .9f) input.TestRelease(); else input.TestHold(InteractionKind.DataStation, nodes[4].NetworkObjectId); }
            }
        }
        private void Fail(string text) { Report.error = text; Debug.LogError(text); }
        public static void Warp(NetworkPlayerController player, Vector3 position)
        {
            var cc = player.GetComponent<CharacterController>(); cc.enabled = false;
            player.GetComponent<NetworkTransform>().Teleport(position, Quaternion.identity, player.transform.localScale);
            player.GetComponent<PlayerMovement>().ResetMotion(); cc.enabled = true;
        }
    }
}
#endif
