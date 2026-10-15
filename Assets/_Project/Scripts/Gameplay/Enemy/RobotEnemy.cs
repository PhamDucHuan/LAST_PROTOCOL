using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace LastProtocol
{
    [DisallowMultipleComponent, RequireComponent(typeof(NetworkObject), typeof(NavMeshAgent), typeof(CapsuleCollider))]
    public sealed class RobotEnemy : NetworkBehaviour
    {
        public NavMeshData navigation;
        private readonly NetworkVariable<EnemyState> state = new NetworkVariable<EnemyState>(default,
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public EnemyState Snapshot => state.Value;
        public event Action<EnemyState> Changed;
        public NavMeshAgent Agent { get; private set; }
        private CapsuleCollider bodyCollider;
        private NavMeshDataInstance meshInstance;
        private PlayerHealth target;
        private Vector3 home;
        private int patrolSide = 1;
        private double nextThink, lastSeen, actionEnds, respawnAt;
        private bool navigationReady;
        private MultiplayerConfig Config => MultiplayerSession.Instance.config;
        public Vector3 Eye => transform.position + Vector3.up * 1.5f;

        public override void OnNetworkSpawn()
        {
            Agent = GetComponent<NavMeshAgent>(); bodyCollider = GetComponent<CapsuleCollider>();
            Agent.enabled = false;
            home = transform.position;
            state.OnValueChanged += OnChanged;
            if (IsServer)
            {
                if (navigation == null) { Debug.LogError("RobotEnemy requires baked Enemy navigation data."); return; }
                meshInstance = NavMesh.AddNavMeshData(navigation);
                if (!NavMesh.SamplePosition(home, out var hit, 2, NavMesh.AllAreas))
                { Debug.LogError("RobotEnemy spawn is not on the baked NavMesh."); return; }
                home = hit.position;
                Agent.speed = Config.enemyMoveSpeed; Agent.angularSpeed = 540; Agent.acceleration = 16;
                Agent.stoppingDistance = 0.1f; Agent.updateRotation = false;
                Agent.enabled = true; Agent.Warp(home);
                navigationReady = true;
                state.Value = new EnemyState(Config.enemyMaxHealth);
            }
            OnChanged(default, Snapshot);
        }
        private void OnChanged(EnemyState before, EnemyState after)
        {
            if (bodyCollider != null) bodyCollider.enabled = !after.Health.IsDowned;
            Changed?.Invoke(after);
        }
        public bool TryApplyDamageServer(int amount, PlayerHealth attacker = null)
        {
            if (!IsSpawned || !IsServer || !navigationReady || MultiplayerSession.Instance == null
                || !MultiplayerSession.Instance.CanMove) return false;
            var next = Snapshot; next.Health = next.Health.Damage(amount);
            if (next.Health.Equals(Snapshot.Health)) return false;
            if (next.Health.IsDowned)
            {
                next.Mode = EnemyMode.Dead; next.TargetClientId = EnemyState.NoTarget; target = null;
                Stop(); Agent.enabled = false;
                respawnAt = Time.realtimeSinceStartupAsDouble + Config.enemyRespawnSeconds;
            }
            else if (target == null && attacker != null && attacker.CanAct
                && EnemyRules.InRange(home, attacker.transform.position, Config.enemyLeashDistance)
                && EnemyRules.InRange(transform.position, attacker.transform.position, Config.enemyLoseRange) && HasSight(attacker))
            {
                target = attacker; lastSeen = Time.realtimeSinceStartupAsDouble;
                next.TargetClientId = attacker.OwnerClientId;
            }
            state.Value = next;
            return true;
        }
        private void Update()
        {
            if (!IsSpawned || !IsServer || !navigationReady) return;
            var session = MultiplayerSession.Instance;
            if (session == null || !session.CanMove) { Stop(); return; }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (DebugPaused) { Stop(); return; }
#endif
            double now = Time.realtimeSinceStartupAsDouble;
            if (Snapshot.Health.IsDowned)
            {
                if (now >= respawnAt) Respawn();
                return;
            }
            if (!Agent.enabled || !Agent.isOnNavMesh) return;
            if (Snapshot.Mode == EnemyMode.Windup)
            {
                FaceTarget();
                if (now < actionEnds) return;
                var next = Snapshot;
                if (target != null && EnemyRules.CanHit(true, target.CanAct, HasSight(target),
                    Vector3.Distance(Eye, target.transform.position + Vector3.up * 0.2f), Config.enemyAttackRange))
                {
                    if (target.TryApplyDamageServer(Config.enemyAttackDamage)) next.SuccessfulAttacks++;
                }
                next.Mode = EnemyMode.Recover; state.Value = next;
                actionEnds = now + Config.enemyAttackCooldown;
                return;
            }
            if (Snapshot.Mode == EnemyMode.Recover && now < actionEnds) { Stop(); return; }
            if (now >= nextThink)
            {
                nextThink = now + 0.2;
                UpdateTarget(now);
            }
            if (target != null)
            {
                if (!target.CanAct) { ReleaseTarget(); return; }
                FaceTarget();
                float distance = EnemyRules.PlanarDistance(transform.position, target.transform.position);
                if (distance <= Config.enemyAttackRange && HasSight(target))
                {
                    Stop(); SetMode(EnemyMode.Windup);
                    actionEnds = now + Config.enemyAttackWindup;
                }
                else
                {
                    SetMode(EnemyMode.Chase); Agent.isStopped = false;
                    Agent.stoppingDistance = Config.enemyAttackRange * 0.65f;
                    if (now >= nextPath) { Agent.SetDestination(target.transform.position); nextPath = now + 0.2; }
                }
                return;
            }
            Agent.stoppingDistance = 0.1f; Agent.isStopped = false;
            bool returning = EnemyRules.PlanarDistance(transform.position, home) > 4;
            SetMode(returning ? EnemyMode.Returning : EnemyMode.Patrol);
            Vector3 destination = returning ? home : home + Vector3.right * patrolSide * 3;
            if (EnemyRules.PlanarDistance(transform.position, destination) < 0.4f) patrolSide *= -1;
            if (now >= nextPath) { Agent.SetDestination(destination); nextPath = now + 0.3; }
            Face(Agent.desiredVelocity);
        }
        private double nextPath;
        private void UpdateTarget(double now)
        {
            if (target == null && Snapshot.TargetClientId != EnemyState.NoTarget) ReleaseTarget();
            if (target != null)
            {
                bool valid = target.IsSpawned && target.CanAct
                    && EnemyRules.InRange(home, target.transform.position, Config.enemyLeashDistance)
                    && EnemyRules.InRange(transform.position, target.transform.position, Config.enemyLoseRange);
                if (valid && HasSight(target)) lastSeen = now;
                if (valid && now - lastSeen <= 2) return;
                ReleaseTarget();
            }
            PlayerHealth nearest = null; float nearestDistance = Config.enemyDetectionRange;
            foreach (var player in FindObjectsByType<PlayerHealth>())
            {
                if (!player.IsSpawned || !player.CanAct || !EnemyRules.InRange(home, player.transform.position, Config.enemyLeashDistance)) continue;
                float distance = EnemyRules.PlanarDistance(transform.position, player.transform.position);
                if (distance > nearestDistance || !HasSight(player)) continue;
                nearest = player; nearestDistance = distance;
            }
            target = nearest;
            if (target == null) return;
            lastSeen = now;
            var next = Snapshot; next.TargetClientId = target.OwnerClientId; state.Value = next;
        }
        private bool HasSight(PlayerHealth player)
        {
            if (player == null || Mathf.Abs(player.transform.position.y - transform.position.y) > 3) return false;
            Vector3 delta = player.transform.position + Vector3.up * 0.2f - Eye;
            return CombatRules.Raycast(Eye, delta.normalized, delta.magnitude + 0.1f, transform, out var hit)
                && hit.collider.GetComponentInParent<PlayerHealth>() == player;
        }
        private void ReleaseTarget()
        {
            target = null; nextPath = 0;
            var next = Snapshot; next.TargetClientId = EnemyState.NoTarget; next.Mode = EnemyMode.Returning; state.Value = next;
        }
        internal void ResetRoundServer()
        {
            if (IsSpawned && IsServer && navigationReady) Respawn();
        }
        private void Respawn()
        {
            target = null; nextThink = nextPath = 0;
            Agent.enabled = true; Agent.Warp(home); Agent.ResetPath(); Agent.isStopped = false;
            // Preserve the cumulative counter so observers can audit actual melee hits.
            var next = new EnemyState(Config.enemyMaxHealth) { SuccessfulAttacks = Snapshot.SuccessfulAttacks };
            state.Value = next;
        }
        private void SetMode(EnemyMode mode)
        { if (Snapshot.Mode != mode) { var next = Snapshot; next.Mode = mode; state.Value = next; } }
        private void Stop() { if (Agent != null && Agent.enabled && Agent.isOnNavMesh) Agent.isStopped = true; }
        private void FaceTarget() { if (target != null) Face(target.transform.position - transform.position); }
        private void Face(Vector3 direction)
        {
            direction.y = 0;
            if (direction.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction), 540 * Time.deltaTime);
        }
        public override void OnNetworkDespawn()
        {
            state.OnValueChanged -= OnChanged;
            if (Agent != null) Agent.enabled = false;
            if (meshInstance.valid) meshInstance.Remove();
            navigationReady = false;
        }
        public static void SpawnServer(RobotEnemy prefab)
        {
            if (prefab == null || NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Existing isolated movement/health/combat suites must not take incidental AI damage.
            var args = Environment.GetCommandLineArgs();
            if (Array.IndexOf(args, "--lp-smoke") >= 0 && Array.IndexOf(args, "--lp-enemy-test") < 0) return;
#endif
            if (FindObjectsByType<RobotEnemy>().Length > 0) return;
            var enemy = Instantiate(prefab, new Vector3(0, 0, 15), Quaternion.identity);
            var environment = FindAnyObjectByType<LevelEnvironment>();
            if (environment != null && environment.enemyNavigation != null) enemy.navigation = environment.enemyNavigation;
            enemy.NetworkObject.Spawn(true);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            enemy.DebugPaused = Array.IndexOf(Environment.GetCommandLineArgs(), "--lp-enemy-test") >= 0;
#endif
        }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public bool DebugPaused { get; set; }
        public void DebugWarpServer(Vector3 position)
        {
            if (!IsServer || !navigationReady || !Agent.enabled) return;
            if (NavMesh.SamplePosition(position, out var hit, 2, NavMesh.AllAreas))
            { Agent.Warp(hit.position); Agent.ResetPath(); }
        }
#endif
    }
}
