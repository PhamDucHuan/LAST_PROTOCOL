using System;
using Unity.Netcode;
using UnityEngine;

namespace LastProtocol
{
    public enum EnemyMode : byte { Patrol, Chase, Windup, Recover, Returning, Dead }

    public struct EnemyState : INetworkSerializable, IEquatable<EnemyState>
    {
        public HealthState Health;
        public EnemyMode Mode;
        public ulong TargetClientId;
        public uint SuccessfulAttacks;
        public const ulong NoTarget = ulong.MaxValue;
        public EnemyState(int maximum)
        { Health = new HealthState(maximum); Mode = EnemyMode.Patrol; TargetClientId = NoTarget; SuccessfulAttacks = 0; }
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Health);
            byte mode = (byte)Mode; serializer.SerializeValue(ref mode); Mode = (EnemyMode)mode;
            serializer.SerializeValue(ref TargetClientId); serializer.SerializeValue(ref SuccessfulAttacks);
        }
        public bool Equals(EnemyState other) => Health.Equals(other.Health) && Mode == other.Mode
            && TargetClientId == other.TargetClientId && SuccessfulAttacks == other.SuccessfulAttacks;
    }

    public static class EnemyRules
    {
        public static float PlanarDistance(Vector3 a, Vector3 b) => new Vector2(a.x - b.x, a.z - b.z).magnitude;
        public static bool InRange(Vector3 a, Vector3 b, float range) => CombatRules.IsFinite(a)
            && CombatRules.IsFinite(b) && range >= 0 && EnemyRules.PlanarDistance(a, b) <= range;
        public static bool CanHit(bool attackerAlive, bool targetAlive, bool lineOfSight, float distance, float range)
            => attackerAlive && targetAlive && lineOfSight && !float.IsNaN(distance)
            && !float.IsInfinity(distance) && distance >= 0 && distance <= range;
    }
}
