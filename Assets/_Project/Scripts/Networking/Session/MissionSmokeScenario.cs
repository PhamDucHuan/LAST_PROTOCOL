#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using Unity.Netcode.Components;
using UnityEngine;

namespace LastProtocol
{
    [Serializable]
    public sealed class MissionSmokeReport
    {
        public int observedObjectives;
        public bool completed, failedObserved, restarted, clientWriteDenied;
        public bool partialSquadBlocked, downedSquadBlocked, interruptedHoldReset;
        public bool passed;
        public string error;
    }
    public sealed class MissionSmokeScenario
    {
        public readonly MissionSmokeReport Report = new();
        private int stage;
        private float stageAt;
        private uint initialAttempt;
        private NetworkPlayerController heldPlayer;
        private void Next(float time) { stage++; stageAt = time; }
        public void Tick(MultiplayerSession session, NetworkPlayerController[] players, float time)
        {
            // Session cleanup destroys Room before its asynchronous scene return finishes.
            if (session == null || session.Room == null) return;
            var mission = session.Room.GetComponent<NetworkMissionState>();
            if (mission == null || mission.Definition == null) { Report.error = "Mission missing on loaded campus."; return; }
            var snapshot = mission.Snapshot;
            if (snapshot.ObjectiveIndex >= 0 && snapshot.ObjectiveIndex <= mission.Definition.objectives.Length)
                Report.observedObjectives |= 1 << snapshot.ObjectiveIndex;
            Report.completed |= snapshot.Phase == MissionPhase.Completed;
            Report.failedObserved |= snapshot.Phase == MissionPhase.Failed;
            Report.restarted |= snapshot.Attempt >= 3;
            if (!session.IsHost)
            {
                Report.clientWriteDenied |= !mission.TestTryClientWrite();
                Report.passed = Report.observedObjectives == 15 && Report.completed && Report.failedObserved
                    && Report.restarted && Report.clientWriteDenied;
                return;
            }
            if (stage < 0 || time < 5) return;
            foreach (var enemy in UnityEngine.Object.FindObjectsByType<RobotEnemy>()) enemy.DebugPaused = true;
            float elapsed = time - stageAt;
            switch (stage)
            {
                case 0:
                    initialAttempt = snapshot.Attempt;
                    PlaceSquad(players, mission.Definition.objectives[0].center);
                    heldPlayer = players[players.Length - 1];
                    Place(heldPlayer, mission.Definition.objectives[0].center + Vector3.forward * 12);
                    Next(time); break;
                case 1 when elapsed >= 1.4f:
                    if (!Check(snapshot.ObjectiveIndex == 0 && snapshot.Inside == players.Length - 1, "Partial squad advanced.")) break;
                    Report.partialSquadBlocked = true;
                    heldPlayer.GetComponent<PlayerHealth>().TryApplyDamageServer(int.MaxValue);
                    Place(heldPlayer, mission.Definition.objectives[0].center);
                    Next(time); break;
                case 2 when elapsed >= 1.3f:
                    if (!Check(snapshot.ObjectiveIndex == 0 && snapshot.Inside == players.Length - 1, "Downed player counted in rendezvous.")) break;
                    Report.downedSquadBlocked = true;
                    if (!Check(heldPlayer.GetComponent<PlayerHealth>().CompleteReviveServer(40), "Fixture could not revive the downed player.")) break;
                    Next(time); break;
                case 3 when elapsed >= 0.9f:
                    Place(heldPlayer, mission.Definition.objectives[0].center + Vector3.forward * 12);
                    Next(time); break;
                case 4 when elapsed >= 0.8f:
                    if (!Check(snapshot.ObjectiveIndex == 0 && snapshot.HeldSeconds == 0, "Interrupted squad hold was not reset.")) break;
                    Report.interruptedHoldReset = true;
                    PlaceSquad(players, mission.Definition.objectives[0].center);
                    Next(time); break;
                case 5 when elapsed >= 3:
                    if (!Check(snapshot.ObjectiveIndex == 1, "Gate objective did not advance.")) break;
                    PlaceSquad(players, mission.Definition.objectives[1].center); Next(time); break;
                case 6 when elapsed >= 3:
                    if (!Check(snapshot.ObjectiveIndex == 2, "Administration objective did not advance.")) break;
                    PlaceSquad(players, mission.Definition.objectives[2].center); Next(time); break;
                case 7 when elapsed >= 3:
                    if (!Check(snapshot.Phase == MissionPhase.Completed, "Recon did not complete.")) break;
                    // Leave the result visible long enough for every Client to observe it.
                    Next(time); break;
                case 8 when elapsed >= 2:
                    DownSquad(players); Next(time); break;
                case 9 when session.Room.TeamWiped.Value:
                    if (!Check(session.Room.RestartRoundServer(), "Host round restart failed.")) break;
                    Next(time); break;
                case 10 when elapsed >= 1:
                    if (!Check(snapshot.Attempt == initialAttempt + 1 && snapshot.Phase == MissionPhase.Active,
                        "Mission was not reset with the round.")) break;
                    DownSquad(players); Next(time); break;
                case 11 when elapsed >= 1.5f:
                    if (!Check(snapshot.Phase == MissionPhase.Failed, "Wipe did not fail the active mission.")) break;
                    Next(time); break;
                case 12 when elapsed >= 2:
                    if (!Check(session.Room.RestartRoundServer(), "Second mission restart failed.")) break;
                    Next(time); break;
                case 13 when elapsed >= 1:
                    Report.passed = Report.observedObjectives == 15 && Report.completed && Report.failedObserved
                        && Report.restarted && Report.partialSquadBlocked && Report.downedSquadBlocked && Report.interruptedHoldReset;
                    stage = 14; break;
            }
        }
        private bool Check(bool value, string error)
        { if (!value) { Report.error = error; stage = -1; } return value; }
        private static void DownSquad(NetworkPlayerController[] players)
        { foreach (var player in players) player.GetComponent<PlayerHealth>().TryApplyDamageServer(int.MaxValue); }
        private static void PlaceSquad(NetworkPlayerController[] players, Vector3 center)
        {
            for (int i = 0; i < players.Length; i++)
                Place(players[i], center + new Vector3((i % 2 == 0 ? -1 : 1) * 1.2f, 0, (i / 2 == 0 ? -1 : 1) * 1.2f));
        }
        private static void Place(NetworkPlayerController player, Vector3 position)
        {
            var controller = player.GetComponent<CharacterController>(); controller.enabled = false;
            player.GetComponent<NetworkTransform>().Teleport(position + Vector3.up * 1.1f, Quaternion.identity, player.transform.localScale);
            player.GetComponent<PlayerMovement>().ResetMotion(); controller.enabled = true;
        }
    }
}
#endif
