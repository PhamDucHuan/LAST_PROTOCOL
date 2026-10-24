using UnityEngine;

namespace LastProtocol
{
    [CreateAssetMenu(menuName = "LastProtocol/Multiplayer Config")]
    public sealed class MultiplayerConfig : ScriptableObject
    {
        public const int MaxPlayers = 4;
        public const int ProtocolVersion = 28;
        public const string MenuScene = "MainMenu";
        public const string GameplayScene = "MultiplayerTest";
        public const int NetworkTickRate = 30;
        public const float InputInterval = 1f / NetworkTickRate;
        public const float ReviveInputInterval = 0.1f;

        [Min(2)] public int minimumPlayersToStart = 2;
        [Min(5)] public float operationTimeoutSeconds = 30;
        [Min(5)] public float connectionTimeoutSeconds = 20;
        [Min(1)] public float movementSpeed = 5;
        [Min(1)] public float sprintSpeed = 8;
        [Min(0.1f)] public float jumpHeight = 1.4f;
        [Min(0.1f)] public float gravityMultiplier = 2;
        [Min(1)] public float terminalFallSpeed = 30;
        [Min(0)] public float jumpCooldownSeconds = 0.15f;
        [Min(1)] public float rotationSpeed = 540;
        [Min(0.1f)] public float cameraSensitivity = 0.15f;
        [Min(1)] public float cameraDistance = 5;
        [Min(1)] public int playerMaxHealth = 100;
        [Min(1)] public int weaponDamage = 25;
        [Min(1)] public float weaponRange = 50;
        [Range(1, 10)] public float weaponShotsPerSecond = 4;
        public bool friendlyFire;
        [Min(1)] public int targetMaxHealth = 100;
        [Min(1)] public float targetResetSeconds = 3;
        [Min(1)] public int enemyMaxHealth = 100;
        [Min(0.1f)] public float enemyMoveSpeed = 3;
        [Min(1)] public float enemyDetectionRange = 12;
        [Min(1)] public float enemyLoseRange = 18;
        [Min(1)] public float enemyLeashDistance = 22;
        [Min(0.5f)] public float enemyAttackRange = 1.8f;
        [Min(1)] public int enemyAttackDamage = 10;
        [Min(0.1f)] public float enemyAttackWindup = 0.45f;
        [Min(0.1f)] public float enemyAttackCooldown = 1;
        [Min(1)] public float enemyRespawnSeconds = 8;
        [Min(0.5f)] public float reviveRange = 2.5f;
        [Min(0.5f)] public float reviveDuration = 3;
        [Range(0.01f, 1)] public float reviveHealthFraction = 0.4f;
        [Min(0.3f)] public float reviveHeartbeatTimeout = 0.5f;
        public string servicesEnvironment = "production";
        public MenuFrontEnd menuPrefab;

        public bool IsValid => minimumPlayersToStart >= 1 && minimumPlayersToStart <= MaxPlayers
            && FiniteAtLeast(operationTimeoutSeconds, 5) && FiniteAtLeast(connectionTimeoutSeconds, 5)
            && FiniteAtLeast(movementSpeed, 0.001f) && FiniteAtLeast(sprintSpeed, movementSpeed)
            && FiniteAtLeast(jumpHeight, 0.001f) && FiniteAtLeast(gravityMultiplier, 0.001f)
            && FiniteAtLeast(terminalFallSpeed, 1) && FiniteAtLeast(jumpCooldownSeconds, 0)
            && FiniteAtLeast(rotationSpeed, 0.001f) && FiniteAtLeast(cameraSensitivity, 0.001f)
            && FiniteAtLeast(cameraDistance, 1) && playerMaxHealth >= 1
            && weaponDamage >= 1 && FiniteAtLeast(weaponRange, 1)
            && FiniteAtLeast(weaponShotsPerSecond, 1) && weaponShotsPerSecond <= 10
            && targetMaxHealth >= 1 && FiniteAtLeast(targetResetSeconds, 1)
            && enemyMaxHealth >= 1 && FiniteAtLeast(enemyMoveSpeed, 0.1f)
            && FiniteAtLeast(enemyDetectionRange, 1) && FiniteAtLeast(enemyLoseRange, enemyDetectionRange)
            && FiniteAtLeast(enemyLeashDistance, enemyLoseRange) && FiniteAtLeast(enemyAttackRange, 0.5f)
            && enemyAttackRange <= enemyDetectionRange && enemyAttackDamage >= 1
            && FiniteAtLeast(enemyAttackWindup, 0.1f) && FiniteAtLeast(enemyAttackCooldown, 0.1f)
            && FiniteAtLeast(enemyRespawnSeconds, 1)
            && FiniteAtLeast(reviveRange, 0.5f) && FiniteAtLeast(reviveDuration, 0.5f)
            && FiniteAtLeast(reviveHealthFraction, 0.01f) && reviveHealthFraction <= 1
            && FiniteAtLeast(reviveHeartbeatTimeout, 0.3f) && reviveHeartbeatTimeout <= reviveDuration;

        private static bool FiniteAtLeast(float value, float minimum) =>
            !float.IsNaN(value) && !float.IsInfinity(value) && value >= minimum;
    }
}
