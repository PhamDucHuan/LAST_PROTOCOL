#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Linq;
using Unity.Netcode.Components;
using UnityEngine;

namespace LastProtocol
{
    [Serializable] public sealed class UplinkSmokeReport
    {
        public uint checks, observedObjectives, observedActive;
        public bool completed, failed, reset, clientWritesDenied, passed;
        public string completionState, resetState, error;
    }
    // Placement/damage are Host fixtures. All interaction requests travel through real owner RPCs.
    public sealed class UplinkSmokeScenario
    {
        public const uint Expected = (1u << 21) - 1;
        public readonly UplinkSmokeReport Report = new();
        private int previous = -1, localStage = -1;
        private float at, localAt, nextSend;
        private GameObject blocker;
        private static readonly string[] Ids = { DataStation.AdministrationId, "CTU_ADMIN_DATA_02", "CTU_ADMIN_DATA_03", DataStation.GeneratorId, DataStation.UplinkId };
        public void Tick(MultiplayerSession session, NetworkPlayerController[] players, float time)
        {
            if (session?.Room == null || time < 5 || players.Length < 2) return;
            var mission = session.Room.GetComponent<NetworkMissionState>();
            if (mission?.Definition?.missionId != "CTU_01_RESTORE_UPLINK") { Report.error = "Uplink mission missing."; return; }
            var all = UnityEngine.Object.FindObjectsByType<DataStation>();
            if (all.Length != 5 || all.Any(x => !x.IsSpawned)) return;
            var nodes = Ids.Select(id => all.Single(x => x.stationId == id)).ToArray();
            var ordered = players.OrderBy(x => x.OwnerClientId).ToArray();
            var actor = ordered[ordered.Length - 1];
            var state = mission.Snapshot;
            Report.checks = mission.TestChecks.Value;
            Report.observedObjectives |= 1u << state.ObjectiveIndex;
            for (int i = 0; i < nodes.Length; i++) if (nodes[i].Snapshot.Active) Report.observedActive |= 1u << i;
            Report.completed |= state.Phase == MissionPhase.Completed;
            Report.failed |= state.Phase == MissionPhase.Failed;
            if (state.Phase == MissionPhase.Completed && nodes.All(x => x.Snapshot.Completed)) Report.completionState = Signature(mission, nodes);
            if (state.Attempt == 3 && state.ObjectiveIndex == 0 && state.CompletedStations == 0 && nodes.All(x => !x.Snapshot.Active && !x.Snapshot.Completed && x.Snapshot.Completions == 0))
            { Report.reset = true; Report.resetState = Signature(mission, nodes); }
            if (!session.IsHost) Report.clientWritesDenied = !mission.TestTryClientWrite() && nodes.All(x => !x.TestTryClientWrite());
            Report.passed = Report.checks == Expected && Report.observedObjectives == 31 && Report.observedActive == 31
                && Report.completed && Report.failed && Report.reset && (session.IsHost || Report.clientWritesDenied);
            int stage = mission.TestStage.Value;
            if (stage >= 22 || Report.error != null) return;
            int targetIndex = stage switch { 1 or 3 or 13 or 14 or 15 => 3, 12 or 16 or 17 => 4, 10 => 1, 11 => 2, _ => 0 };
            var target = nodes[targetIndex];
            if (session.IsHost)
            {
                foreach (var enemy in UnityEngine.Object.FindObjectsByType<RobotEnemy>()) enemy.DebugPaused = true;
                if (stage == 0) { mission.TestStage.Value = 1; return; }
                if (previous != stage)
                {
                    previous = stage; at = time;
                    if (blocker != null) UnityEngine.Object.Destroy(blocker);
                    foreach (var p in ordered) p.GetComponent<PlayerInteraction>().ResetRoundServer();
                    if (stage < 18)
                    {
                        for (int i = 0; i < ordered.Length; i++)
                            Warp(ordered[i], target.transform.position + new Vector3((i-(ordered.Length-1)*.5f)*.7f,1.1f,-1.8f));
                        if (stage == 4) Warp(actor, target.transform.position + new Vector3(0,1.1f,-9));
                        if (stage == 5) actor.GetComponent<PlayerHealth>().TryApplyDamageServer(int.MaxValue);
                        if (stage == 7)
                        {
                            blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            blocker.transform.position = target.transform.position + new Vector3(0,1,-1);
                            blocker.transform.localScale = new Vector3(4,3,.1f); Physics.SyncTransforms();
                        }
                    }
                    if (stage == 2)
                        for (int i = 0; i < ordered.Length; i++) Warp(ordered[i], new Vector3(i-1,1.1f,-40));
                    if (stage == 18 || stage == 20)
                        foreach (var p in ordered) p.GetComponent<PlayerHealth>().TryApplyDamageServer(int.MaxValue);
                }
                float elapsed = time - at;
                float wait = stage switch { 2 => 2.8f, 8 or 10 or 11 => 4.3f, 14 => 5.3f, 16 => 6.3f, 17 or 21 => 3, 20 => 2, _ => 1.8f };
                if (elapsed > wait)
                {
                    bool idle = !target.Snapshot.Active;
                    bool ok = stage switch
                    {
                        1 => state.ObjectiveIndex == 0 && nodes.All(x => !x.Snapshot.Active && !x.Snapshot.Completed),
                        2 => state.ObjectiveIndex == 1 && state.CompletedStations == 0,
                        3 or 4 or 5 or 6 or 7 => idle && !target.Snapshot.Completed && state.ObjectiveIndex == 1 && state.CompletedStations == 0,
                        8 or 9 => nodes[0].Snapshot.Completed && nodes[0].Snapshot.Completions == 1 && state.CompletedStations == 1 && state.ObjectiveIndex == 1,
                        10 => state.CompletedStations == 3 && state.ObjectiveIndex == 1,
                        11 => state.CompletedStations == 7 && state.ObjectiveIndex == 2,
                        12 => idle && !target.Snapshot.Completed && state.ObjectiveIndex == 2,
                        13 => idle && !target.Snapshot.Completed && state.CompletedStations == 7,
                        14 or 15 => nodes[3].Snapshot.Completed && nodes[3].Snapshot.Completions == 1 && state.CompletedStations == 15 && state.ObjectiveIndex == 3,
                        16 or 17 => state.Phase == MissionPhase.Completed && state.CompletedStations == 31 && nodes.All(x => x.Snapshot.Completed && x.Snapshot.Completions == 1),
                        18 => session.Room.TeamWiped.Value && session.Room.RestartRoundServer(),
                        19 => state.Attempt == 2 && state.Phase == MissionPhase.Active && state.CompletedStations == 0 && nodes.All(x => x.Snapshot.Completions == 0),
                        20 => state.Phase == MissionPhase.Failed && session.Room.RestartRoundServer(),
                        21 => Report.reset && state.Phase == MissionPhase.Active,
                        _ => false
                    };
                    if (!ok) { Report.error = "Uplink authority stage failed: " + stage; Debug.LogError(Report.error); return; }
                    if (stage == 5) actor.GetComponent<PlayerHealth>().DebugResetHealthServer();
                    mission.TestChecks.Value |= 1u << (stage - 1); mission.TestStage.Value++;
                }
            }
            if (stage != localStage)
            {
                localStage = stage; localAt = time; nextSend = time + .3f;
                foreach (var p in ordered) if (p.IsOwner) p.GetComponent<PlayerInteraction>().TestRelease();
            }
            if (time < nextSend || stage == 2 || stage >= 18) return;
            nextSend = time + .1f;
            foreach (var p in ordered)
            {
                if (!p.IsOwner || (stage != 8 && stage != 14 && stage != 16 && p != actor)) continue;
                var input = p.GetComponent<PlayerInteraction>();
                if (stage == 13 && time-localAt > .8f) { input.TestRelease(); continue; }
                input.TestHold(InteractionKind.DataStation, stage == 6 ? ulong.MaxValue : target.NetworkObjectId);
            }
        }
        private static string Signature(NetworkMissionState mission, DataStation[] nodes)
        {
            var s = mission.Snapshot;
            return $"{s.MissionId}:{s.SchemaVersion}:{s.Attempt}:{s.Phase}:{s.Failure}:{s.ObjectiveIndex}:{s.CompletedStations}:{s.Inside}:{s.Required}:{s.HeldSeconds:R}:"
                + string.Join(";", nodes.Select(x => $"{x.stationId}={x.Snapshot.Active},{x.Snapshot.Completed},{x.Snapshot.Completions},{x.Snapshot.Actor},{x.Snapshot.StartedAt:R},{x.Snapshot.Duration:R}"));
        }
        private static void Warp(NetworkPlayerController player, Vector3 position)
        {
            var controller = player.GetComponent<CharacterController>(); controller.enabled = false;
            player.GetComponent<NetworkTransform>().Teleport(position, Quaternion.identity, player.transform.localScale);
            player.GetComponent<PlayerMovement>().ResetMotion(); controller.enabled = true;
        }
    }
}
#endif
