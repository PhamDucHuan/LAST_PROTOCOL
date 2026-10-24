using UnityEngine;

namespace LastProtocol
{
    public enum EnemyRole : byte { Frontliner, Flanker, Suppressor, Support, Controller, Hunter }
    public enum EnemyAttack : byte { Melee, Rifle, Repair, AreaDenial }
    public enum EnemyNoise : byte { Gunshot, Sprint, Alarm, ChipEject, Decoy }
    public enum AdaptiveModifier : byte { None, ArmorResponse, AntiSniperDrone, CyberShield, FlankAmbush, ReviveHunter, AreaDenial, ResourcePressure }
    public enum TendencyTag : byte { None, CloseRangeHeavy, LongRangeStatic, HackDependent, TightFormation, FrequentRevive }
    public enum DeadlineReason : byte { None, VisualContact, Gunfire, Alarm, CombatTime, ChipExposed, SquadDown, CleanData, Checkpoint, PursuitLost }

    [CreateAssetMenu(menuName = "LastProtocol/AI/Enemy Profile")]
    public sealed class EnemyAIProfile : ScriptableObject
    {
        public string profileId;
        public EnemyRole role;
        public EnemyAttack attack;
        public int maximumHealth = 100, damage = 8;
        public float speed = 3, visionRange = 16, visionAngle = 110, hearingRange = 24;
        public float memorySeconds = 7, groupRadius = 18, leash = 32;
        public float attackRange = 1.8f, windup = .7f, cooldown = 1.6f;
        public float repairRadius = 8, repairCooldown = 7, hazardRadius = 2.2f, hazardSeconds = 3;
        public float captureSeconds = 3, retreatFraction = .2f;
        public bool Validate(out string error)
        {
            error = "";
            if (!MissionDefinition.ValidId(profileId) || !System.Enum.IsDefined(typeof(EnemyRole), role)
                || !System.Enum.IsDefined(typeof(EnemyAttack), attack) || maximumHealth < 1 || maximumHealth > 500 || damage < 1 || damage > 20
                || !Between(speed, 1, 5) || !Between(visionRange, 4, 30) || !Between(visionAngle, 30, 180)
                || !Between(hearingRange, 4, 40) || !Between(memorySeconds, 2, 12) || !Between(groupRadius, 4, 24)
                || !Between(leash, visionRange, 45) || !Between(attackRange, 1, 18) || !Between(windup, .5f, 3)
                || !Between(cooldown, 1, 10) || !Between(repairRadius, 2, 10) || !Between(repairCooldown, 5, 20)
                || !Between(hazardRadius, 1, 3) || !Between(hazardSeconds, 1, 4) || !Between(captureSeconds, 2, 6)
                || !Between(retreatFraction, 0, .3f)) error = "Invalid or unbounded enemy AI profile: " + profileId;
            return error.Length == 0;
        }
        private static bool Between(float x, float min, float max) => !float.IsNaN(x) && !float.IsInfinity(x) && x >= min && x <= max;
    }

}
