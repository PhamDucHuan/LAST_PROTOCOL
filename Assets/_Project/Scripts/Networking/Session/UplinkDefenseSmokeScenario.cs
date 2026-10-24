#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Linq;
using Unity.Netcode.Components;
using UnityEngine;

namespace LastProtocol
{
    [Serializable] public sealed class UplinkDefenseSmokeReport
    {
        public uint checks;
        public int maximumRobots;
        public bool observedAttack, observedDamageCancel, observedDefenderHit, observedCleared, observedFailure;
        public bool clientWritesDenied, authorityCorrect = true, passed;
        public string firstWave, completedState, resetState, error;
    }
    // All holds and defender shots use owner RPCs; only placement and controlled AI pause are fixtures.
    public sealed class UplinkDefenseSmokeScenario
    {
        public const uint Expected = (1u << 22) - 1;
        public readonly UplinkDefenseSmokeReport Report = new();
        private int previous = -1, localStage = -1;
        private float at, localAt, nextSend;
        private bool sawChannel, fireAttempted;
        private uint actorShots;
        private Vector3 robotStart;
        private string initialIds;
        private static readonly string[] Ids = { DataStation.AdministrationId,"CTU_ADMIN_DATA_02","CTU_ADMIN_DATA_03",DataStation.GeneratorId,DataStation.UplinkId };
        public void Tick(MultiplayerSession session, NetworkPlayerController[] players, float time)
        {
            if (session?.Room == null || time < 5 || players.Length < 2) return;
            var mission = session.Room.GetComponent<NetworkMissionState>(); var encounter = session.Room.GetComponent<UplinkEncounter>();
            if (encounter == null || !encounter.EnabledForMission) { Report.error = "Uplink defence missing."; return; }
            var stations = UnityEngine.Object.FindObjectsByType<DataStation>();
            if (stations.Length != 5 || stations.Any(x => !x.IsSpawned)) return;
            var nodes = Ids.Select(id => stations.Single(x => x.stationId == id)).ToArray();
            var ordered = players.OrderBy(x => x.OwnerClientId).ToArray();
            var actor = ordered[0]; var defender = ordered[ordered.Length-1];
            var actorInput = actor.GetComponent<PlayerInteraction>(); var defenderGun = defender.GetComponent<NetworkHitscanWeapon>();
            var actorHealth = actor.GetComponent<PlayerHealth>(); var uplink = nodes[4];
            var enemies = UnityEngine.Object.FindObjectsByType<RobotEnemy>().OrderBy(x => x.NetworkObjectId).ToArray();
            var wave = encounter.Snapshot; var snapshot = mission.Snapshot; int stage = encounter.TestStage.Value;
            Report.checks = encounter.TestChecks.Value; Report.maximumRobots = Math.Max(Report.maximumRobots,enemies.Length);
            Report.authorityCorrect &= enemies.All(x => x.IsMissionEnemy && x.Agent != null
                && (session.IsHost ? x.Snapshot.Health.IsDowned || (x.Agent.enabled && x.Agent.isOnNavMesh) : !x.Agent.enabled));
            Report.observedAttack |= enemies.Any(x => x.Snapshot.SuccessfulAttacks > 0);
            Report.observedDamageCancel |= Report.observedAttack && actorHealth.Snapshot.Current < actorHealth.Snapshot.Maximum && !uplink.Snapshot.Active;
            Report.observedDefenderHit |= defenderGun.ObservedDamagingHits > 0;
            Report.observedCleared |= wave.Phase == UplinkEncounterPhase.Cleared;
            Report.observedFailure |= wave.Attempt == 2 && wave.Phase == UplinkEncounterPhase.Failed;
            if (stage == 8 && enemies.Length == 3) Report.firstWave = string.Join(",",enemies.Select(x => x.NetworkObjectId));
            if (wave.Phase == UplinkEncounterPhase.Secured && enemies.Length == 0) Report.completedState = Signature(wave);
            if (wave.Attempt == 3 && wave.Phase == UplinkEncounterPhase.Dormant && enemies.Length == 0) Report.resetState = Signature(wave);
            if (!session.IsHost) Report.clientWritesDenied = !encounter.TestTryClientWrite() && enemies.All(x => !x.TryApplyDamageServer(25));
            Report.passed = Report.checks == Expected && Report.maximumRobots == 3 && Report.authorityCorrect
                && Report.observedAttack && Report.observedDamageCancel && Report.observedDefenderHit && Report.observedCleared
                && Report.observedFailure && !string.IsNullOrEmpty(Report.completedState) && !string.IsNullOrEmpty(Report.resetState)
                && (session.IsHost || Report.clientWritesDenied);
            if (stage >= 23 || Report.error != null) return;
            int targetIndex = stage switch { 2 or 16 => 0, 3 or 17 => 1, 4 or 18 => 2, 5 or 6 or 19 => 3, _ => 4 };
            if (session.IsHost)
            {
                foreach (var robot in enemies)
                    robot.DebugPaused = stage == 10 && robot.Snapshot.Health.IsDowned ? false : stage != 7 || robot != enemies[0];
                if (stage == 0) { encounter.TestStage.Value = 1; return; }
                if (stage != previous)
                {
                    previous = stage; at = time; sawChannel = fireAttempted = false;
                    foreach (var p in ordered) p.GetComponent<PlayerInteraction>().ResetRoundServer();
                    if (stage < 12 || stage >= 15 && stage <= 19)
                    {
                        Warp(actor,nodes[targetIndex].transform.position + new Vector3(0,1.1f,-2));
                        for(int i=1;i<ordered.Length;i++)Warp(ordered[i],new Vector3(40+i*1.2f,1.1f,-44));
                    }
                    if (stage == 1 || stage == 15)
                        for(int i=0;i<ordered.Length;i++)Warp(ordered[i],new Vector3(i-1,1.1f,-40));
                    if (stage == 7)
                    {
                        enemies[0].DebugWarpServer(new Vector3(61,0,-48)); robotStart = enemies[0].transform.position;
                        initialIds = string.Join(",",enemies.Select(x => x.NetworkObjectId));
                    }
                    if (stage == 9)
                    {
                        enemies[0].DebugWarpServer(new Vector3(56,0,-44));
                        Warp(defender,new Vector3(58.5f,1.1f,-44));
                        actorShots = actor.GetComponent<NetworkHitscanWeapon>().ObservedShots;
                    }
                    if (stage == 11)
                    {
                        enemies[1].DebugWarpServer(new Vector3(56,0,-44)); enemies[2].DebugWarpServer(new Vector3(53,0,-44));
                        Warp(defender,new Vector3(58.5f,1.1f,-44));
                    }
                    if (stage == 13 || stage == 20)
                        foreach(var p in ordered)p.GetComponent<PlayerHealth>().TryApplyDamageServer(int.MaxValue);
                    if (stage == 22)
                        for(int i=0;i<ordered.Length;i++)Warp(ordered[i],new Vector3(28+i*1.2f,1.1f,-26));
                    Physics.SyncTransforms();
                }
                float elapsed = time-at;
                sawChannel |= uplink.Snapshot.Active;
                if(stage == 9 && !fireAttempted && elapsed > .8f)
                {
                    fireAttempted = true;
                    var gun = actor.GetComponent<NetworkHitscanWeapon>(); gun.TestFire(gun.Eye,Vector3.left);
                }
                float wait = stage switch { 1 or 15 => 2.9f, 2 or 3 or 4 or 16 or 17 or 18 => 4.1f,
                    6 or 19 => 5.2f, 7 => 3.2f, 8 => 2.2f, 9 => 2.6f, 10 => session.config.enemyRespawnSeconds+1.2f,
                    11 => 6, 12 or 20 or 22 => 2, _ => 1.7f };
                if (elapsed > wait)
                {
                    bool dormant = wave.Phase == UplinkEncounterPhase.Dormant && wave.Spawned == 0 && wave.Activations == 0 && enemies.Length == 0;
                    bool sameWave = wave.Spawned == 3 && wave.Activations == 1 && enemies.Length == 3
                        && string.Join(",",enemies.Select(x => x.NetworkObjectId)) == initialIds;
                    bool ok = stage switch
                    {
                        1 or 15 => snapshot.ObjectiveIndex == 1 && dormant,
                        2 or 3 or 4 or 16 or 17 or 18 => nodes[targetIndex].Snapshot.Completed && dormant,
                        5 => !nodes[3].Snapshot.Active && !nodes[3].Snapshot.Completed && dormant,
                        6 or 19 => wave.Phase == UplinkEncounterPhase.Active && wave.Spawned == 3 && wave.Alive == 3 && wave.Activations == 1 && enemies.Length == 3,
                        7 => sameWave && sawChannel && Report.observedDamageCancel && enemies[0].Snapshot.SuccessfulAttacks > 0
                            && Vector3.Distance(enemies[0].transform.position,robotStart) > .5f,
                        8 => sameWave && sawChannel && !uplink.Snapshot.Active && !uplink.Snapshot.Completed,
                        9 => sameWave && sawChannel && !uplink.Snapshot.Active && !uplink.Snapshot.Completed && enemies[0].Snapshot.Health.IsDowned
                            && wave.Defeated == 1 && defenderGun.ObservedDamagingHits > 0 && fireAttempted
                            && actor.GetComponent<NetworkHitscanWeapon>().ObservedShots == actorShots,
                        10 => sameWave && enemies[0].Snapshot.Health.IsDowned && wave.Alive == 2 && wave.Defeated == 1,
                        11 or 12 => snapshot.Phase == MissionPhase.Completed && wave.Phase == UplinkEncounterPhase.Secured
                            && wave.Spawned == 3 && wave.Activations == 1 && wave.Defeated == 3 && enemies.Length == 0,
                        13 => session.Room.TeamWiped.Value && session.Room.RestartRoundServer(),
                        14 => wave.Attempt == 2 && dormant,
                        20 => snapshot.Phase == MissionPhase.Failed && wave.Phase == UplinkEncounterPhase.Failed && enemies.Length == 0,
                        21 => session.Room.TeamWiped.Value && session.Room.RestartRoundServer(),
                        22 => wave.Attempt == 3 && dormant && snapshot.ObjectiveIndex == 0,
                        _ => false
                    };
                    if(!ok){Report.error = "Uplink defence stage failed: " + stage;Debug.LogError(Report.error);return;}
                    encounter.TestChecks.Value |= 1u << (stage-1); encounter.TestStage.Value++;
                }
            }
            if(stage != localStage)
            {
                localStage=stage;localAt=time;nextSend=time+.3f;
                foreach(var p in ordered)if(p.IsOwner)p.GetComponent<PlayerInteraction>().TestRelease();
            }
            if(time < nextSend)return;nextSend=time+.22f;
            bool hold = stage == 2 || stage == 3 || stage == 4 || stage == 6 || stage == 7 || stage == 11
                || stage >= 16 && stage <= 19 || (stage == 5 || stage == 8) && time-localAt < .9f
                || stage == 9 && time-localAt < 1.8f;
            if(actor.IsOwner)
            {
                if(hold)actorInput.TestHold(InteractionKind.DataStation,nodes[targetIndex].NetworkObjectId);
                else actorInput.TestRelease();
            }
            if(defender.IsOwner && (stage == 9 || stage == 11))
            {
                var target = stage == 9 ? enemies.FirstOrDefault() : enemies.FirstOrDefault(x => !x.Snapshot.Health.IsDowned);
                if (target != null && target.Snapshot.Health.IsDowned) target = null;
                if(target != null)defenderGun.TestFire(defenderGun.Eye,(target.transform.position+Vector3.up-defenderGun.Eye).normalized);
            }
        }
        private static string Signature(UplinkEncounterState s) => $"{s.Attempt}:{s.Phase}:{s.Activations}:{s.Spawned}:{s.Alive}:{s.Defeated}";
        private static void Warp(NetworkPlayerController player,Vector3 position)
        {
            var controller=player.GetComponent<CharacterController>();controller.enabled=false;
            player.GetComponent<NetworkTransform>().Teleport(position,Quaternion.identity,player.transform.localScale);
            player.GetComponent<PlayerMovement>().ResetMotion();controller.enabled=true;
        }
    }
}
#endif
