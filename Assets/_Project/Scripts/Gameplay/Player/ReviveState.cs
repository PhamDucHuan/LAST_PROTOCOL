using System;
using Unity.Netcode;

namespace LastProtocol
{
    public struct ReviveState : INetworkSerializable, IEquatable<ReviveState>
    {
        public bool Active;
        public ulong TargetObjectId;
        public double StartedAt;
        public float Duration;
        public uint CompletedRevives;
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Active); serializer.SerializeValue(ref TargetObjectId);
            serializer.SerializeValue(ref StartedAt); serializer.SerializeValue(ref Duration);
            serializer.SerializeValue(ref CompletedRevives);
        }
        public bool Equals(ReviveState other) => Active == other.Active && TargetObjectId == other.TargetObjectId
            && StartedAt.Equals(other.StartedAt) && Duration.Equals(other.Duration) && CompletedRevives == other.CompletedRevives;
    }

    public static class ReviveRules
    {
        public static bool CanStart(bool rescuerAlive, bool targetDowned, bool differentPlayer, bool free,
            bool lineOfSight, float distance, float range) => rescuerAlive && targetDowned && differentPlayer && free && lineOfSight
            && !float.IsNaN(distance) && !float.IsInfinity(distance) && distance >= 0 && distance <= range;
        public static int RestoredHealth(int maximum, float fraction)
        {
            if (maximum < 1 || float.IsNaN(fraction) || float.IsInfinity(fraction) || fraction <= 0 || fraction > 1) return 0;
            return (int)Math.Min(maximum, Math.Max(1, Math.Round(maximum * (double)fraction, MidpointRounding.AwayFromZero)));
        }
    }
}
