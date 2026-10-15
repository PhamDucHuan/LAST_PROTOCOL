#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using Unity.Netcode.Components;
using UnityEngine;

namespace LastProtocol
{
    [Serializable]
    public sealed class EnemySmokeReport
    {
        public int maximumEnemies;
        public bool patrol, chase, windup, meleeDamage, retarget, dodgedAttack, returning;
        public bool enemyDamaged, enemyDead, enemyRespawned, clientWriteRejected, authorityCorrect = true;
        public bool deadMoved, deadAttacked, clientShotDamagedEnemy, passed;
    }

    // A deterministic scenario, enabled only by --lp-enemy-test in Development builds.
    public sealed class EnemySmokeScenario
    {
        public readonly EnemySmokeReport Report = new EnemySmokeReport();
        private int phase;
        private float phaseTime, nextShot;
        private bool started, victimWindup, sawDead, baselineSet;
        private uint attacksBeforeDodge, attacksAtDeath, baselineHits;
        private Vector3 deadPosition;
        private Vector3 patrolOrigin;
        private bool patrolOriginSet;
        private float deadStarted;
        private ulong victimId = EnemyState.NoTarget, secondId = EnemyState.NoTarget;

        public void Tick(MultiplayerSession session, NetworkPlayerController[] players, float gameTime)
        {
            var enemies = UnityEngine.Object.FindObjectsByType<RobotEnemy>();
            Report.maximumEnemies = Mathf.Max(Report.maximumEnemies, enemies.Length);
            if (enemies.Length != 1 || players.Length != MultiplayerConfig.MaxPlayers) return;
            var enemy = enemies[0]; var state = enemy.Snapshot;
            Report.authorityCorrect &= enemy.IsServer ? state.Health.IsDowned || (enemy.Agent.enabled && enemy.Agent.isOnNavMesh)
                : !enemy.Agent.enabled;
            var first = BySlot(session, players, 1);
            var second = BySlot(session, players, 2);
            var shooter = BySlot(session, players, 3);
            if (first == null || second == null || shooter == null) return;
            victimId = first.OwnerClientId; secondId = second.OwnerClientId;
            var weapon = shooter.GetComponent<NetworkHitscanWeapon>();
            if (!baselineSet && gameTime >= 22) { baselineHits = weapon.ObservedDamagingHits; baselineSet = true; }
            if (gameTime < 23) return;
            if (!patrolOriginSet) { patrolOriginSet = true; patrolOrigin = enemy.transform.position; }
            Report.patrol |= state.Mode == EnemyMode.Patrol && Vector3.Distance(patrolOrigin, enemy.transform.position) > 0.5f;
            Report.chase |= state.Mode == EnemyMode.Chase;
            Report.windup |= state.Mode == EnemyMode.Windup;
            Report.meleeDamage |= state.SuccessfulAttacks > 0 && first.GetComponent<PlayerHealth>().Snapshot.Current < session.config.playerMaxHealth;
            Report.retarget |= state.TargetClientId == secondId;
            Report.returning |= state.Mode == EnemyMode.Returning;
            Report.enemyDamaged |= state.Health.IsInitialized && state.Health.Current < state.Health.Maximum;
            if (state.TargetClientId == secondId && state.Mode == EnemyMode.Windup && !victimWindup)
            { victimWindup = true; attacksBeforeDodge = state.SuccessfulAttacks; }
            if (victimWindup && state.Mode == EnemyMode.Recover && state.SuccessfulAttacks == attacksBeforeDodge
                && second.GetComponent<PlayerHealth>().Snapshot.Current == session.config.playerMaxHealth) Report.dodgedAttack = true;
            if (state.Health.IsDowned)
            {
                Report.enemyDead = true;
                if (!sawDead) { sawDead = true; deadStarted = gameTime; deadPosition = enemy.transform.position; attacksAtDeath = state.SuccessfulAttacks; }
                // Allow the remote NetworkTransform interpolation buffer to settle first.
                if (gameTime - deadStarted < 0.4f) deadPosition = enemy.transform.position;
                else if (Vector3.Distance(deadPosition, enemy.transform.position) > 0.15f) Report.deadMoved = true;
                if (state.SuccessfulAttacks != attacksAtDeath) Report.deadAttacked = true;
            }
            else if (sawDead) Report.enemyRespawned = true;
            Report.clientShotDamagedEnemy |= baselineSet && weapon.ObservedDamagingHits > baselineHits && Report.enemyDamaged;
            if (!session.IsHost && !Report.clientWriteRejected && state.Health.IsInitialized)
                Report.clientWriteRejected = !enemy.TryApplyDamageServer(25);
            if (session.IsHost) AdvanceServer(session, players, enemy, first, second, shooter, gameTime);
            if (shooter.IsOwner && shooter.transform.position.z > 10 && !state.Health.IsDowned
                && !Report.enemyDead && gameTime >= nextShot)
            {
                nextShot = gameTime + 1f / session.config.weaponShotsPerSecond + 0.07f;
                weapon.TestFire(weapon.Eye, (enemy.transform.position + Vector3.up - weapon.Eye).normalized);
            }
            Report.passed = Report.maximumEnemies == 1 && Report.patrol && Report.chase && Report.windup
                && Report.meleeDamage && Report.retarget && Report.dodgedAttack && Report.returning
                && Report.enemyDamaged && Report.enemyDead && Report.enemyRespawned && Report.clientShotDamagedEnemy
                && Report.authorityCorrect && !Report.deadMoved && !Report.deadAttacked
                && (session.IsHost || Report.clientWriteRejected);
        }
        private void AdvanceServer(MultiplayerSession session, NetworkPlayerController[] players, RobotEnemy enemy,
            NetworkPlayerController first, NetworkPlayerController second, NetworkPlayerController shooter, float time)
        {
            if (!started) { started = true; phaseTime = time; enemy.DebugPaused = false; }
            float elapsed = time - phaseTime;
            switch (phase)
            {
                case 0:
                    if (elapsed < 1.3f) break;
                    WarpPlayer(first, new Vector3(0, 1.05f, 6)); Next(time); break;
                case 1:
                    if (enemy.Snapshot.SuccessfulAttacks == 0) break;
                    Next(time); break;
                case 2:
                    if (elapsed < 0.8f) break;
                    first.GetComponent<PlayerHealth>().TryApplyDamageServer(int.MaxValue);
                    WarpPlayer(second, enemy.transform.position + new Vector3(2.5f, 1.05f, 0));
                    Next(time); break;
                case 3:
                    if (enemy.Snapshot.TargetClientId != second.OwnerClientId || enemy.Snapshot.Mode != EnemyMode.Windup) break;
                    WarpPlayer(second, new Vector3(15, 1.05f, -12)); Next(time); break;
                case 4:
                    if (!Report.dodgedAttack || enemy.Snapshot.Mode != EnemyMode.Patrol || enemy.transform.position.z < 14) break;
                    var point = enemy.transform.position + Vector3.back * 4;
                    point.y = 1.05f; point.z = Mathf.Max(11, point.z);
                    WarpPlayer(shooter, point); Next(time); break;
                case 5:
                    if (!enemy.Snapshot.Health.IsDowned) break;
                    foreach (var player in players)
                    {
                        int slot = session.Room.GetSlot(player.OwnerClientId);
                        WarpPlayer(player, new Vector3(-9 + slot * 6, 1.05f, -4));
                    }
                    Next(time); break;
            }
        }
        private void Next(float time) { phase++; phaseTime = time; }
        private static NetworkPlayerController BySlot(MultiplayerSession session, NetworkPlayerController[] players, int slot)
        { foreach (var player in players) if (session.Room.GetSlot(player.OwnerClientId) == slot) return player; return null; }
        private static void WarpPlayer(NetworkPlayerController player, Vector3 point)
        {
            var collider = player.GetComponent<CharacterController>(); collider.enabled = false;
            player.GetComponent<NetworkTransform>().Teleport(point, Quaternion.identity, Vector3.one);
            collider.enabled = true; Physics.SyncTransforms();
        }
    }
}
#endif
