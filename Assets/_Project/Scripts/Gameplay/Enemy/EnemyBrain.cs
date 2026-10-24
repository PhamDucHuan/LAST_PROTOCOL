using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace LastProtocol
{
    // Server-only decisions. EnemyState carries presentation; clients never run navigation or damage.
    [DisallowMultipleComponent, RequireComponent(typeof(RobotEnemy))]
    public sealed class EnemyBrain : MonoBehaviour
    {
        public EnemyAIProfile profile;
        public EnemyAIProfile CurrentProfile { get; private set; }
        public RobotEnemy Enemy { get; private set; }
        public bool IsConfigured => profile != null;
        public bool IsAlive => Enemy != null && Enemy.IsSpawned && !Enemy.Snapshot.Health.IsDowned;
        public string Group => Enemy != null ? Enemy.encounterId : "";
        public bool Armored { get; internal set; }
        public EnemyMemory Memory => memory;
        private EnemyDirector director;
        private NetworkChip chip;
        private EnemyMemory memory;
        private PlayerHealth visible;
        private Vector3 home, lockedAim;
        private double nextThink, nextPath, actionEnd, nextAttack, nextRepair, retreatUntil, retreatReady, disabledUntil, hackedUntil, idleUntil;
        private uint consumedNoise;
        private bool attacking, repairing, hazardActive;
        private double hazardUntil, nextHazardTick;
        private int patrolSide = 1;
        private float Now => Time.realtimeSinceStartup;
        private NavMeshAgent Agent => Enemy.Agent;

        internal void BeginServer(RobotEnemy enemy, Vector3 origin)
        {
            Enemy = enemy; CurrentProfile = profile; home = origin;
            director = MultiplayerSession.Instance?.Room?.GetComponent<EnemyDirector>();
            chip = MultiplayerSession.Instance?.Room?.GetComponent<NetworkChip>();
            if (!profile.Validate(out string error)) { Debug.LogError(error); DisableServer(30); return; }
            Agent.speed = profile.speed; Set(EnemyMode.Idle); idleUntil = Now + .5;
        }
        internal int IncomingDamage(int amount) => DirectorRules.ArmorDamage(amount, Armored);
        internal void DamagedServer(PlayerHealth attacker, bool dead)
        {
            CancelActions(); chip?.ReleaseEnemyServer(Enemy);
            if (dead) return;
            // Damage reveals the impact direction, not a target's future position behind cover.
            if (attacker != null && attacker.CanAct)
                memory.Observe(attacker.transform.position, attacker.NetworkObjectId, Now, CurrentProfile.memorySeconds, .8f);
            Set(EnemyMode.Suspicious);
        }
        internal void ReceiveShared(Vector3 point, ulong target)
        {
            if (!IsAlive || disabledUntil > Now || hackedUntil > Now || visible != null) return;
            memory.Observe(point, target, Now, CurrentProfile.memorySeconds, .65f);
            Set(EnemyMode.Alert); // Recipients do not rebroadcast until they see a player themselves.
        }
        internal void AssignRoleServer(EnemyAIProfile value)
        {
            if (!IsAlive || !Enemy.IsServer || value == null || !value.Validate(out _) || value.maximumHealth != Enemy.Snapshot.Health.Maximum) return;
            CancelActions(); chip?.ReleaseEnemyServer(Enemy); CurrentProfile = value; Agent.speed = value.speed; Set(EnemyMode.Reposition);
        }
        public bool DisableServer(float seconds)
        {
            if (!IsAlive || !Enemy.IsServer || !MissionRules.Positive(seconds) || seconds > 30) return false;
            disabledUntil = Now + seconds; hackedUntil = 0; CancelActions(); chip?.ReleaseEnemyServer(Enemy); Set(EnemyMode.Disabled); Stop(); return true;
        }
        // Called by a future validated Tier-2 ability; deliberately has no client RPC.
        public bool HackServer(float seconds)
        {
            if (!IsAlive || !Enemy.IsServer || !MissionRules.Positive(seconds) || seconds > 15) return false;
            hackedUntil = Now + seconds; disabledUntil = 0; CancelActions(); chip?.ReleaseEnemyServer(Enemy); Set(EnemyMode.Hacked); return true;
        }
        internal void TickServer(double now)
        {
            if (!IsAlive || !Enemy.IsServer) return;
            if (disabledUntil > now) { Stop(); Set(EnemyMode.Disabled); return; }
            if (hackedUntil > now) { TickHacked(now); return; }
            if (Enemy.Snapshot.Mode == EnemyMode.Disabled || Enemy.Snapshot.Mode == EnemyMode.Hacked)
            { memory = default; Set(EnemyMode.Returning); }
            TickHazard(now);
            if (attacking)
            {
                Stop(); if (now < actionEnd) return;
                attacking = false; ResolveAttack(now); nextAttack = now + CurrentProfile.cooldown; Set(EnemyMode.Recover); return;
            }
            if (repairing)
            {
                Stop(); if (now < actionEnd) return; repairing = false;
                foreach (var ally in Allies()) if (Vector3.Distance(transform.position, ally.transform.position) <= CurrentProfile.repairRadius && Clear(ally.Eye, ally.transform)) ally.RepairServer(15, 20);
                nextRepair = now + CurrentProfile.repairCooldown; Set(EnemyMode.Recover); return;
            }
            if (now < nextThink) return; nextThink = now + .2;
            visible = Perceive();
            if (visible != null)
            {
                bool acquired = memory.Confidence(now) <= 0 || memory.Target != visible.NetworkObjectId;
                memory.Observe(visible.transform.position, visible.NetworkObjectId, now, CurrentProfile.memorySeconds, 1);
                director?.ObservePursuit(); director?.ShareContact(this, memory.Position, memory.Target);
                if (acquired) { Set(EnemyMode.CallReinforcement); Stop(); return; }
            }
            else if (director != null && director.Hear(this, ref consumedNoise, out var noise))
            { memory.Observe(noise.Position, EnemyState.NoTarget, now, CurrentProfile.memorySeconds, .5f); Set(EnemyMode.Suspicious); Stop(); return; }
            if (Enemy.Snapshot.Health.Fraction <= CurrentProfile.retreatFraction && now >= retreatReady && visible != null)
            { retreatUntil = now + 2; retreatReady = now + 10; CancelActions(); }
            if (now < retreatUntil) { Set(EnemyMode.Retreat); Move(home, now); return; }
            if (CurrentProfile.role == EnemyRole.Hunter && chip != null && chip.EnemyEligible(Enemy))
            {
                var drop = chip.Snapshot.DropPosition;
                if (PerceptionRules.InCone(Enemy.Eye, transform.forward, drop, CurrentProfile.visionRange, CurrentProfile.visionAngle) && chip.EnemyCanSeeDrop(Enemy))
                {
                    Set(EnemyMode.CaptureChip);
                    if (Vector3.Distance(transform.position, drop) > 2.2f) Move(drop, now);
                    else { Stop(); chip.TryCaptureEnemyServer(Enemy, CurrentProfile.captureSeconds); }
                    return;
                }
            }
            if (chip != null && chip.IsEnemyHolder(Enemy.NetworkObjectId)) { Set(EnemyMode.ProtectNode); Move(home, now); return; }
            if (CurrentProfile.attack == EnemyAttack.Repair && now >= nextRepair && Allies().Any(x => (x.Snapshot.Health.Fraction < 1 || x.Snapshot.Shield == 0)
                && Vector3.Distance(transform.position, x.transform.position) <= CurrentProfile.repairRadius && Clear(x.Eye, x.transform)))
            {
                Stop(); repairing = true; actionEnd = now + CurrentProfile.windup; SetAction(EnemyMode.Repair, transform.position, CurrentProfile.repairRadius); return;
            }
            if (visible != null)
            {
                float distance = Vector3.Distance(transform.position, visible.transform.position);
                if (!hazardActive && CurrentProfile.attack != EnemyAttack.Repair && distance <= CurrentProfile.attackRange && now >= nextAttack)
                {
                    Stop(); attacking = true; lockedAim = visible.transform.position + Vector3.up * .4f;
                    actionEnd = now + CurrentProfile.windup;
                    SetAction(EnemyMode.Windup, lockedAim, CurrentProfile.attack == EnemyAttack.AreaDenial ? CurrentProfile.hazardRadius : 0); Face(lockedAim - Enemy.Eye); return;
                }
                Vector3 destination = memory.Position;
                if (CurrentProfile.role == EnemyRole.Flanker)
                    destination += Vector3.Cross(Vector3.up, (home - memory.Position).normalized) * ((Enemy.NetworkObjectId % 2 == 0) ? 4 : -4);
                if (CurrentProfile.attack != EnemyAttack.Melee && distance < 4)
                    destination = transform.position + (transform.position - memory.Position).normalized * 3;
                Set(CurrentProfile.role == EnemyRole.Flanker || distance < 4 && CurrentProfile.attack != EnemyAttack.Melee ? EnemyMode.Reposition : EnemyMode.Chase);
                Move(destination, now); Face(memory.Position - transform.position); return;
            }
            if (memory.Confidence(now) > 0)
            {
                if (Vector3.Distance(transform.position, memory.Position) > 1) { Set(EnemyMode.Investigate); Move(memory.Position, now); }
                else { Set(EnemyMode.Search); Stop(); Face(Quaternion.Euler(0, 50, 0) * transform.forward); }
                return;
            }
            if (Vector3.Distance(transform.position, home) > 3.8f) { Set(EnemyMode.Returning); Move(home, now); return; }
            if (now < idleUntil) { Set(EnemyMode.Idle); Stop(); return; }
            Vector3 patrol = home + Vector3.right * patrolSide * 3;
            if (Vector3.Distance(transform.position, patrol) < .5f) { patrolSide *= -1; idleUntil = now + .8; }
            Set(EnemyMode.Patrol); Move(patrol, now); Face(Agent.desiredVelocity);
        }
        private PlayerHealth Perceive()
        {
            PlayerHealth best = null; float score = float.MinValue;
            foreach (var p in FindObjectsByType<PlayerHealth>())
            {
                if (!p.IsSpawned || !p.CanAct || director?.IsReviveProtected(p.NetworkObjectId) == true
                    || Vector3.Distance(home, p.transform.position) > CurrentProfile.leash
                    || !PerceptionRules.InCone(Enemy.Eye, transform.forward, p.transform.position + Vector3.up * .4f, CurrentProfile.visionRange, CurrentProfile.visionAngle)
                    || !Clear(p.transform.position + Vector3.up * .4f, p.transform)) continue;
                float s = -Vector3.Distance(transform.position, p.transform.position) + (director?.TargetBonus(CurrentProfile.role, p) ?? 0);
                if (s > score) { score = s; best = p; }
            }
            return best;
        }
        private bool Clear(Vector3 point, Transform allowed)
        {
            Vector3 delta = point - Enemy.Eye;
            if (!CombatRules.Raycast(Enemy.Eye, delta.normalized, delta.magnitude + .02f, transform, out var hit)) return true;
            return allowed != null && hit.transform.IsChildOf(allowed);
        }
        private void ResolveAttack(double now)
        {
            if (CurrentProfile.attack == EnemyAttack.AreaDenial)
            { hazardActive = true; hazardUntil = now + CurrentProfile.hazardSeconds; nextHazardTick = now; SetAction(EnemyMode.AreaDenial, lockedAim, CurrentProfile.hazardRadius); return; }
            foreach (var p in FindObjectsByType<PlayerHealth>())
            {
                if (!p.IsSpawned || !p.CanAct || director?.IsReviveProtected(p.NetworkObjectId) == true
                    || Vector3.Distance(transform.position, p.transform.position) > CurrentProfile.attackRange + .1f) continue;
                bool hit = CurrentProfile.attack == EnemyAttack.Melee
                    ? p == visible && Vector3.Distance(p.transform.position + Vector3.up * .4f, lockedAim) <= 1.25f && Clear(p.transform.position + Vector3.up * .4f, p.transform)
                    : CombatRules.Raycast(Enemy.Eye, (lockedAim - Enemy.Eye).normalized, CurrentProfile.attackRange, transform, out var ray) && ray.collider.GetComponentInParent<PlayerHealth>() == p;
                if (hit && p.TryApplyDamageServer(CurrentProfile.damage)) { CountAttack(); break; }
            }
        }
        private void TickHazard(double now)
        {
            if (!hazardActive) return;
            if (now >= hazardUntil) { hazardActive = false; return; }
            if (now < nextHazardTick) return; nextHazardTick = now + 1;
            foreach (var p in FindObjectsByType<PlayerHealth>())
                if (p.IsSpawned && p.CanAct && director?.IsReviveProtected(p.NetworkObjectId) != true
                    && Vector3.Distance(p.transform.position + Vector3.up * .4f, lockedAim) <= CurrentProfile.hazardRadius
                    && Clear(p.transform.position + Vector3.up * .4f, p.transform) && p.TryApplyDamageServer(4)) CountAttack();
        }
        private void TickHacked(double now)
        {
            Set(EnemyMode.Hacked); var other = Allies().OrderBy(x => Vector3.Distance(transform.position, x.transform.position)).FirstOrDefault();
            if (other == null) { Stop(); return; }
            if (Vector3.Distance(transform.position, other.transform.position) > 2) Move(other.transform.position, now);
            else { Stop(); if (now >= nextAttack && Clear(other.Eye, other.transform)) { other.TryApplyDamageServer(5); nextAttack = now + 1.5; } }
        }
        private RobotEnemy[] Allies() => FindObjectsByType<RobotEnemy>().Where(x => x != Enemy && x.IsSpawned && !x.Snapshot.Health.IsDowned && x.encounterId == Group).ToArray();
        private void CountAttack() { var s = Enemy.Snapshot; s.SuccessfulAttacks++; Enemy.SetTacticalStateServer(s); }
        private void Set(EnemyMode mode)
        {
            var s = Enemy.Snapshot; s.Mode = mode; s.Role = CurrentProfile.role;
            s.TargetClientId = visible != null && visible.CanAct ? visible.OwnerClientId : EnemyState.NoTarget;
            s.Confidence = (byte)Mathf.RoundToInt(memory.Confidence(Now) * 100);
            if (mode != EnemyMode.Windup && mode != EnemyMode.Repair && !hazardActive) { s.ActionDuration = 0; s.ActionRadius = 0; }
            Enemy.SetTacticalStateServer(s);
        }
        private void SetAction(EnemyMode mode, Vector3 point, float radius)
        {
            Set(mode); var s = Enemy.Snapshot; s.ActionPoint = point; s.ActionRadius = radius;
            s.ActionStartedAt = Enemy.NetworkManager.ServerTime.Time; s.ActionDuration = mode == EnemyMode.AreaDenial ? CurrentProfile.hazardSeconds : CurrentProfile.windup;
            Enemy.SetTacticalStateServer(s);
        }
        private void Move(Vector3 destination, double now)
        {
            if (now < nextPath) return; nextPath = now + .2;
            Vector3 offset = destination - home; if (offset.magnitude > CurrentProfile.leash) destination = home + offset.normalized * CurrentProfile.leash;
            if (!NavMesh.SamplePosition(destination, out var hit, 2, NavMesh.AllAreas)) { Stop(); return; }
            var path = new NavMeshPath();
            if (!Agent.CalculatePath(hit.position, path) || path.status != NavMeshPathStatus.PathComplete) { Stop(); return; }
            Agent.stoppingDistance = .2f; Agent.isStopped = false; Agent.SetPath(path);
            Face(Agent.desiredVelocity);
        }
        private void Stop() { if (Agent != null && Agent.enabled && Agent.isOnNavMesh) { Agent.isStopped = true; Agent.velocity = Vector3.zero; } }
        private void Face(Vector3 delta) { delta.y = 0; if (delta.sqrMagnitude > .01f) transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(delta), 108); }
        private void CancelActions() { attacking = repairing = hazardActive = false; visible = null; nextAttack = Now + .7; }
        internal void EndServer() { if (Enemy != null && Enemy.IsServer) chip?.ReleaseEnemyServer(Enemy); CancelActions(); }
    }
}
