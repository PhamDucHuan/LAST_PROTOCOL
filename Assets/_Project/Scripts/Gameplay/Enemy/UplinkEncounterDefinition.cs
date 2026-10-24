using UnityEngine;
using UnityEngine.AI;

namespace LastProtocol
{
    [CreateAssetMenu(menuName = "LastProtocol/Missions/Uplink Defence Encounter")]
    public sealed class UplinkEncounterDefinition : ScriptableObject
    {
        public const string WaveId = "CTU01_UPLINK_DEFENCE";
        public const string MissionId = "CTU_01_RESTORE_UPLINK";
        public string encounterId = WaveId;
        public RobotEnemy enemyPrefab;
        public NavMeshData navigation;
        public Vector3[] spawnPoints;
        public bool Validate(out string error)
        {
            error = "";
            if (encounterId != WaveId || enemyPrefab == null || navigation == null || spawnPoints == null
                || spawnPoints.Length < 1 || spawnPoints.Length > 4)
                error = "Uplink defence requires its stable ID, registered robot prefab, navigation and 1-4 spawn points.";
            else if (enemyPrefab.respawnOnDeath || !enemyPrefab.externallyManagedNavigation || enemyPrefab.encounterId != WaveId
                || enemyPrefab.navigation != navigation)
                error = "Mission robots must share encounter navigation and must not respawn.";
            else
                for (int i = 0; i < spawnPoints.Length; i++)
                {
                    if (!MissionRules.Finite(spawnPoints[i])) { error = "Invalid encounter position."; break; }
                    for (int j = 0; j < i; j++)
                        if (Vector3.Distance(spawnPoints[i], spawnPoints[j]) < 2) { error = "Encounter spawns overlap."; break; }
                }
            return error.Length == 0;
        }
    }
}
