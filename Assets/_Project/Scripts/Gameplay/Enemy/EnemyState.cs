using System;
using Unity.Netcode;
using UnityEngine;

namespace LastProtocol
{
    public enum EnemyMode : byte { Patrol, Chase, Windup, Recover, Returning, Dead, Idle, Suspicious, Investigate,
        Alert, Reposition, Search, CallReinforcement, ProtectNode, CaptureChip, Disabled, Hacked, Retreat, Repair, AreaDenial }

    public struct EnemyState : INetworkSerializable, IEquatable<EnemyState>
    {
        public HealthState Health;
        public EnemyMode Mode;
        public ulong TargetClientId;
        public uint SuccessfulAttacks;
        public EnemyRole Role;
        public int Shield;
        public byte Confidence;
        public Vector3 ActionPoint;
        public double ActionStartedAt;
        public float ActionDuration, ActionRadius;
        public const ulong NoTarget = ulong.MaxValue;
        public EnemyState(int maximum)
        { this = default; Health = new HealthState(maximum); Mode = EnemyMode.Patrol; TargetClientId = NoTarget; }
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Health);
            byte mode = (byte)Mode; serializer.SerializeValue(ref mode); Mode = (EnemyMode)mode;
            serializer.SerializeValue(ref TargetClientId); serializer.SerializeValue(ref SuccessfulAttacks);
            serializer.SerializeValue(ref Role); serializer.SerializeValue(ref Shield); serializer.SerializeValue(ref Confidence);
            serializer.SerializeValue(ref ActionPoint); serializer.SerializeValue(ref ActionStartedAt);
            serializer.SerializeValue(ref ActionDuration); serializer.SerializeValue(ref ActionRadius);
        }
        public bool Equals(EnemyState other) => Health.Equals(other.Health) && Mode == other.Mode
            && TargetClientId == other.TargetClientId && SuccessfulAttacks == other.SuccessfulAttacks
            && Role == other.Role && Shield == other.Shield && Confidence == other.Confidence && ActionPoint.Equals(other.ActionPoint)
            && ActionStartedAt.Equals(other.ActionStartedAt) && ActionDuration.Equals(other.ActionDuration) && ActionRadius.Equals(other.ActionRadius);
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
