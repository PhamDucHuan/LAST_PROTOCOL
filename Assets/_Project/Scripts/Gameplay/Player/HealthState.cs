using System;
using Unity.Netcode;

namespace LastProtocol
{
    // One atomic snapshot: observers cannot see HP and life state from different ticks.
    public struct HealthState : INetworkSerializable, IEquatable<HealthState>
    {
        private int current;
        private int maximum;
        public int Current => current;
        public int Maximum => maximum;
        public bool IsInitialized => maximum > 0;
        public bool IsDowned => IsInitialized && current == 0;
        public float Fraction => IsInitialized ? (float)current / maximum : 0;

        public HealthState(int maximum)
        {
            if (maximum < 1) throw new ArgumentOutOfRangeException(nameof(maximum));
            this.maximum = maximum;
            current = maximum;
        }
        public HealthState Damage(int amount)
        {
            if (!IsInitialized || amount <= 0 || IsDowned) return this;
            var next = this;
            next.current -= Math.Min(amount, current); // Safe even with int.MaxValue damage.
            return next;
        }
        public HealthState Heal(int amount)
        {
            // Ordinary healing cannot bypass the validated teammate revive channel.
            if (!IsInitialized || amount <= 0 || IsDowned) return this;
            var next = this;
            next.current += Math.Min(amount, maximum - current);
            return next;
        }
        public HealthState Revive(int amount)
        {
            if (!IsDowned || amount <= 0) return this;
            var next = this;
            next.current = Math.Min(amount, maximum);
            return next;
        }
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref current);
            serializer.SerializeValue(ref maximum);
        }
        public bool Equals(HealthState other) => current == other.current && maximum == other.maximum;
        public override bool Equals(object obj) => obj is HealthState other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(current, maximum);
    }
}
