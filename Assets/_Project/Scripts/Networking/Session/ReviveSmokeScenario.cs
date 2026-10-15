#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using Unity.Netcode.Components;
using UnityEngine;

namespace LastProtocol
{
    [Serializable]
    public sealed class ReviveSmokeReport
    {
        public uint serverChecks;
        public bool channelObserved, partialHealthObserved, teamWipeObserved, roundRestartObserved;
        public bool clientRestartRejected, samePlayerObjects = true, passed;
        public bool clientReviveRejected;
    }

    // Real owner RPCs run in the remote Client process. Only damage/placement are test fixtures.
    public sealed class ReviveSmokeScenario
    {
        public readonly ReviveSmokeReport Report = new ReviveSmokeReport();
        public const uint ExpectedChecks = (1u << 16) - 1;
        private int previousStage = -1, localStage = -1;
        private float stageStarted, localStarted, nextSend;
        private bool injected;
        private uint shotsAtStart;
        private ulong[] playerIds;
        private GameObject blocker;

        public void Tick(MultiplayerSession session, NetworkPlayerController[] players, float gameTime)
        {
            if (gameTime < 23 || players.Length != MultiplayerConfig.MaxPlayers) return;
            var ordered = new NetworkPlayerController[MultiplayerConfig.MaxPlayers];
            foreach (var p in players) { int slot = session.Room.GetSlot(p.OwnerClientId); if (slot >= 0) ordered[slot] = p; }
            foreach (var p in ordered) if (p == null) return;
            var actor = ordered[1].GetComponent<PlayerRevive>();
            var target = ordered[2].GetComponent<PlayerRevive>();
            var rival = ordered[3].GetComponent<PlayerRevive>();
            if (playerIds == null) { playerIds = new ulong[4]; for (int i = 0; i < 4; i++) playerIds[i] = ordered[i].NetworkObjectId; }
            for (int i = 0; i < 4; i++) Report.samePlayerObjects &= playerIds[i] == ordered[i].NetworkObjectId;
            Report.channelObserved |= actor.IsChanneling;
            Report.partialHealthObserved |= ordered[2].GetComponent<PlayerHealth>().Snapshot.Current == 40;
            if (!session.IsHost && target.GetComponent<PlayerHealth>().Snapshot.IsDowned)
                Report.clientReviveRejected |= !target.GetComponent<PlayerHealth>().CompleteReviveServer(40);
            Report.teamWipeObserved |= session.Room.TeamWiped.Value;
            Report.roundRestartObserved |= Report.teamWipeObserved && !session.Room.TeamWiped.Value && session.Room.RoundRestarts.Value >= 2;
            if (!session.IsHost && session.Room.TeamWiped.Value) Report.clientRestartRejected |= !session.Room.RestartRoundServer();
            if (session.IsHost) Advance(session, ordered, actor, target, rival);
            int stage = actor.TestStage.Value;
            if (localStage != stage)
            {
                localStage = stage; localStarted = Time.realtimeSinceStartup; nextSend = 0;
                foreach (var p in ordered) if (p.IsOwner) p.GetComponent<PlayerRevive>().TestRelease();
            }
            float localTime = Time.realtimeSinceStartup - localStarted;
            bool hold = stage >= 1 && stage <= 14 && stage != 2 && localTime > 0.2f;
            if (stage == 3 && localTime > 0.5f) hold = false; // Drop heartbeat without sending release.
            if (stage == 9 && localTime > 0.6f && ordered[1].IsOwner) ordered[1].TestInput = Vector2.left;
            if (actor.IsOwner && hold && Time.realtimeSinceStartup >= nextSend)
            {
                nextSend = Time.realtimeSinceStartup + 0.1f;
                actor.TestHold(stage == 7 ? ulong.MaxValue : target.NetworkObjectId);
                if (stage == 10 && localTime > 0.5f)
                { var weapon = actor.GetComponent<NetworkHitscanWeapon>(); weapon.TestFire(weapon.Eye, Vector3.forward); }
            }
            if (rival.IsOwner && stage == 8 && localTime > 0.7f && Time.realtimeSinceStartup >= nextSend)
            { nextSend = Time.realtimeSinceStartup + 0.1f; rival.TestHold(target.NetworkObjectId); }
            Report.serverChecks = actor.TestChecks.Value;
            Report.passed = Report.serverChecks == ExpectedChecks && Report.channelObserved && Report.partialHealthObserved
                && Report.teamWipeObserved && Report.roundRestartObserved && Report.samePlayerObjects
                && (session.IsHost || (Report.clientRestartRejected && Report.clientReviveRejected));
        }

        private void Advance(MultiplayerSession session, NetworkPlayerController[] players, PlayerRevive actor, PlayerRevive target, PlayerRevive rival)
        {
            int stage = actor.TestStage.Value;
            if (stage == 0) { actor.TestStage.Value = 1; stage = 1; }
            float now = Time.realtimeSinceStartup;
            if (previousStage != stage)
            {
                previousStage = stage; stageStarted = now; injected = false;
                if (blocker != null) UnityEngine.Object.Destroy(blocker);
                if (stage == 17) return;
                if (stage <= 14 && stage != 2)
                {
                    foreach (var p in players) p.GetComponent<PlayerHealth>().DebugResetHealthServer();
                    Warp(players[0], new Vector3(-10, 1, -8)); Warp(players[1], new Vector3(-4, 1, -5));
                    Warp(players[2], new Vector3(-2.5f, 1, -5)); Warp(players[3], new Vector3(-4, 1, -3.5f));
                    target.GetComponent<PlayerHealth>().TryApplyDamageServer(int.MaxValue);
                }
                shotsAtStart = actor.GetComponent<NetworkHitscanWeapon>().ObservedShots;
                if (stage == 6)
                {
                    blocker = GameObject.CreatePrimitive(PrimitiveType.Cube); blocker.name = "Revive LOS test fixture";
                    blocker.transform.position = new Vector3(-3.25f, 1.5f, -5); blocker.transform.localScale = new Vector3(0.2f, 3, 3);
                    Physics.SyncTransforms();
                }
                if (stage == 15) foreach (var p in players) p.GetComponent<PlayerHealth>().TryApplyDamageServer(int.MaxValue);
            }
            float elapsed = now - stageStarted;
            if (!injected && elapsed > 0.8f)
            {
                injected = true;
                if (stage == 4) Warp(players[1], new Vector3(-9, 1, -5));
                if (stage == 5) actor.GetComponent<PlayerHealth>().TryApplyDamageServer(1);
                if (stage == 13) actor.GetComponent<PlayerHealth>().TryApplyDamageServer(int.MaxValue);
                if (stage == 14) target.GetComponent<PlayerHealth>().DebugResetHealthServer();
            }
            float length = stage == 11 || stage == 12 ? 4.4f : stage == 1 ? 1.2f : 2f;
            if (elapsed < length || stage == 17) return;
            bool down = target.GetComponent<PlayerHealth>().Snapshot.IsDowned;
            bool idle = !actor.IsChanneling;
            bool check = false;
            switch (stage)
            {
                case 1: check = actor.IsChanneling && down && actor.Snapshot.CompletedRevives == 0; break;
                case 2: check = idle && down; break;
                case 3: check = idle && down; break;
                case 4: check = idle && down; break;
                case 5: check = idle && down && actor.GetComponent<PlayerHealth>().Snapshot.Current == 99; break;
                case 6: check = idle && down; break;
                case 7: check = idle && down; break;
                case 8: check = actor.IsChanneling && !rival.IsChanneling && down; break;
                case 9: check = idle && down && players[1].GetComponent<PlayerMovement>().PlanarSpeed > 0.1f; break;
                case 10: check = actor.IsChanneling && down && actor.GetComponent<NetworkHitscanWeapon>().ObservedShots == shotsAtStart; break;
                case 11: check = idle && target.GetComponent<PlayerHealth>().Snapshot.Current == 40 && actor.Snapshot.CompletedRevives == 1; break;
                case 12: check = idle && target.GetComponent<PlayerHealth>().Snapshot.Current == 40 && actor.Snapshot.CompletedRevives == 2; break;
                case 13: check = idle && down && actor.GetComponent<PlayerHealth>().Snapshot.IsDowned; break;
                case 14: check = idle && target.GetComponent<PlayerHealth>().Snapshot.Current == 100 && actor.Snapshot.CompletedRevives == 2; break;
                case 15:
                    check = session.Room.TeamWiped.Value && !actor.GetComponent<PlayerHealth>().DebugResetHealthServer()
                        && session.Room.RestartRoundServer() && !session.Room.RestartRoundServer();
                    break;
                case 16:
                    check = !session.Room.TeamWiped.Value && session.Room.RoundRestarts.Value == 2 && players.Length == 4;
                    foreach (var p in players) check &= p.GetComponent<PlayerHealth>().Snapshot.Current == 100;
                    break;
            }
            if (check) actor.TestChecks.Value |= 1u << (stage - 1);
            else Debug.LogError("Revive smoke stage failed: " + stage + " actor=" + actor.IsChanneling + " targetHP=" + target.GetComponent<PlayerHealth>().Snapshot.Current);
            actor.TestStage.Value = stage + 1;
        }
        private static void Warp(NetworkPlayerController player, Vector3 position)
        {
            var controller = player.GetComponent<CharacterController>(); controller.enabled = false;
            player.GetComponent<NetworkTransform>().Teleport(position, Quaternion.identity, Vector3.one);
            player.GetComponent<PlayerMovement>().ResetMotion(); controller.enabled = true; Physics.SyncTransforms();
        }
    }
}
#endif
