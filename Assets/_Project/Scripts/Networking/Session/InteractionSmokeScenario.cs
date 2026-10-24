#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Linq;
using Unity.Netcode.Components;
using UnityEngine;

namespace LastProtocol
{
    [Serializable]
    public sealed class InteractionSmokeReport
    {
        public uint checks;
        public bool observedActive, observedCompleted, passed;
        public string error;
    }
    // Uses real owner RPCs in the remote client; damage and placement are fixtures.
    public sealed class InteractionSmokeScenario
    {
        public readonly InteractionSmokeReport Report = new();
        public const uint Expected = (1u << 13) - 1;
        private int previous = -1, localStage = -1;
        private float at, localAt, nextSend;
        private bool injected, observedRevive;
        private GameObject blocker;
        public void Tick(MultiplayerSession session, NetworkPlayerController[] players, float time)
        {
            if (time < 5 || session.Room == null || players.Length != 2) return;
            var station = UnityEngine.Object.FindObjectsByType<DataStation>().SingleOrDefault(x => x.stationId == DataStation.AdministrationId);
            if (station == null || !station.IsSpawned) { Report.error = "Station did not spawn."; return; }
            var ordered = players.OrderBy(x => x.OwnerClientId).ToArray();
            var actor = ordered[1].GetComponent<PlayerInteraction>();
            var other = ordered[0].GetComponent<PlayerInteraction>();
            var actorHealth = actor.GetComponent<PlayerHealth>();
            Report.observedActive |= station.Snapshot.Active;
            Report.observedCompleted |= station.Snapshot.Completed;
            Report.checks = station.TestChecks.Value;
            Report.passed = Report.checks == Expected && Report.observedActive && Report.observedCompleted;
            int stage = station.TestStage.Value;
            if (stage >= 14) return;
            if (session.IsHost)
            {
                foreach (var enemy in UnityEngine.Object.FindObjectsByType<RobotEnemy>()) enemy.DebugPaused = true;
                if (stage == 0) { station.TestStage.Value = 1; return; }
                if (stage != previous)
                {
                    previous = stage; at = time; injected = false; observedRevive = false;
                    if (blocker != null) UnityEngine.Object.Destroy(blocker);
                    if (stage >= 14) return;
                    foreach (var p in ordered) { p.GetComponent<PlayerInteraction>().ResetRoundServer(); p.GetComponent<PlayerHealth>().DebugResetHealthServer(); }
                    if (stage != 2 && stage != 10 && stage != 11) station.ResetRoundServer();
                    Warp(ordered[1], new Vector3(30, 1.1f, -26)); Warp(ordered[0], new Vector3(31.5f, 1.1f, -26));
                    if (stage == 4) Warp(ordered[1], new Vector3(30, 1.1f, -32));
                    if (stage == 6 || stage == 7 || stage == 8) other.GetComponent<PlayerHealth>().TryApplyDamageServer(int.MaxValue);
                    if (stage == 9) { Warp(ordered[1], new Vector3(30.8f, 1.1f, -26)); Warp(ordered[0], new Vector3(29.2f, 1.1f, -26)); }
                    if (stage == 13)
                    {
                        blocker = GameObject.CreatePrimitive(PrimitiveType.Cube); blocker.name = "Interaction LOS fixture";
                        blocker.transform.position = new Vector3(30, 1, -25); blocker.transform.localScale = new Vector3(3, 3, .1f); Physics.SyncTransforms();
                    }
                }
                float elapsed = time - at;
                observedRevive |= actor.GetComponent<PlayerRevive>().IsChanneling;
                if (!injected && elapsed > .8f)
                {
                    injected = true;
                    if (stage == 3) actorHealth.TryApplyDamageServer(int.MaxValue);
                    if (stage == 11)
                    {
                        foreach (var p in ordered) p.GetComponent<PlayerHealth>().TryApplyDamageServer(int.MaxValue);
                    }
                }
                if (stage == 11 && session.Room.TeamWiped.Value) session.Room.RestartRoundServer();
                float duration = stage == 8 || stage == 9 ? 4.4f : 1.7f;
                if (elapsed > duration)
                {
                    bool idle = !station.Snapshot.Active;
                    bool ok = stage switch
                    {
                        1 => station.Snapshot.Active && station.Snapshot.Actor == actor.NetworkObjectId,
                        2 => idle && !station.Snapshot.Completed,
                        3 => idle && actorHealth.Snapshot.IsDowned,
                        4 or 5 => idle && !station.Snapshot.Completed,
                        6 => idle && !actor.GetComponent<PlayerRevive>().IsChanneling && !station.Snapshot.Completed,
                        7 => idle && observedRevive && !actor.GetComponent<PlayerRevive>().IsChanneling,
                        8 => !other.GetComponent<PlayerHealth>().Snapshot.IsDowned && actor.GetComponent<PlayerRevive>().Snapshot.CompletedRevives == 1,
                        9 or 10 => station.Snapshot.Completed && station.Snapshot.Completions == 1,
                        11 => !station.Snapshot.Completed && !session.Room.TeamWiped.Value && session.Room.RoundRestarts.Value > 0,
                        12 or 13 => idle && !station.Snapshot.Completed,
                        _ => false
                    };
                    if (!ok) { Report.error = "Interaction stage failed: " + stage; Debug.LogError(Report.error); return; }
                    station.TestChecks.Value |= 1u << (stage - 1); station.TestStage.Value++;
                }
            }
            if (stage != localStage)
            {
                localStage = stage; localAt = time; nextSend = time + .25f;
                foreach (var p in ordered) if (p.IsOwner) p.GetComponent<PlayerInteraction>().TestRelease();
            }
            float localElapsed = time - localAt;
            if (stage < 1 || stage >= 14 || time < nextSend) return;
            nextSend = time + .1f;
            if (actor.IsOwner)
            {
                if (stage == 2 || stage == 11 || (stage == 12 && localElapsed > .6f)) return;
                if (stage == 6 && localElapsed > .65f) actor.TestHold(InteractionKind.Revive, other.NetworkObjectId);
                else if (stage == 7 && localElapsed < .65f || stage == 8) actor.TestHold(InteractionKind.Revive, other.NetworkObjectId);
                else actor.TestHold(InteractionKind.DataStation, stage == 5 ? ulong.MaxValue : station.NetworkObjectId);
            }
            if (other.IsOwner && stage == 9) other.TestHold(InteractionKind.DataStation, station.NetworkObjectId);
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
