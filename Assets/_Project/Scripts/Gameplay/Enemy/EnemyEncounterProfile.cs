using UnityEngine;

namespace LastProtocol
{
    [CreateAssetMenu(menuName = "LastProtocol/AI/Encounter Profile")]
    public sealed class EnemyEncounterProfile : ScriptableObject
    {
        // Index equals EnemyRole; changing role never heals or respawns an existing enemy.
        public RobotEnemy[] roster = new RobotEnemy[6];
        public EnemyRole[] opening = { EnemyRole.Frontliner, EnemyRole.Suppressor, EnemyRole.Hunter };
        public AdaptiveModifier[] whitelist = { AdaptiveModifier.ArmorResponse, AdaptiveModifier.CyberShield,
            AdaptiveModifier.FlankAmbush, AdaptiveModifier.ReviveHunter, AdaptiveModifier.AreaDenial };
        public Vector3 protectedNode = new(61, 0, -42);
        public int maximumAlive = 3;
        public float adaptationDelay = 10, adaptationWarning = 2, checkpointRetention = .5f;
        public bool Validate(out string error)
        {
            error = "";
            if (roster == null || roster.Length != 6 || opening == null || opening.Length < 1 || opening.Length > maximumAlive
                || maximumAlive < 1 || maximumAlive > 4 || !MissionRules.Finite(protectedNode)
                || !MissionRules.Positive(adaptationDelay) || adaptationDelay < 8 || adaptationDelay > 30
                || !MissionRules.Positive(adaptationWarning) || adaptationWarning < 2 || adaptationWarning > 5
                || !MissionRules.Positive(checkpointRetention) || checkpointRetention < .5f || checkpointRetention > 1 || whitelist == null || whitelist.Length > 5)
                error = "Invalid encounter budget, warning or retention.";
            else
            {
                for (int i = 0; i < roster.Length; i++)
                {
                    var brain = roster[i] != null ? roster[i].GetComponent<EnemyBrain>() : null;
                    if (brain == null || brain.profile == null || (int)brain.profile.role != i || !brain.profile.Validate(out _)
                        || roster[i].respawnOnDeath || !roster[i].externallyManagedNavigation || roster[i].navigation == null
                        || roster[i].navigation != roster[0].navigation || roster[i].encounterId != UplinkEncounterDefinition.WaveId)
                    { error = "Missing or mismatched tactical role " + i; break; }
                }
                var unique = new System.Collections.Generic.HashSet<AdaptiveModifier>();
                foreach (var modifier in whitelist)
                    if (!DirectorRules.Supported(modifier) || !unique.Add(modifier)) { error = "Unsupported or duplicate modifier."; break; }
                foreach (var role in opening) if (!System.Enum.IsDefined(typeof(EnemyRole), role)) error = "Unknown opening role.";
            }
            return error.Length == 0;
        }
    }
}
