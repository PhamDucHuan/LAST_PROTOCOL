namespace LastProtocol
{
    public enum InteractionKind : byte { None, Revive, DataStation, RecoverChip, RelayDrill, EnemyCapture }

    // A key gesture owns exactly one action/target until an explicit release.
    public sealed class InteractionGate
    {
        public InteractionKind Kind { get; private set; }
        public ulong Target { get; private set; }
        public bool RequiresRelease { get; private set; }
        public bool TryHold(InteractionKind kind, ulong target)
        {
            if (RequiresRelease || (kind != InteractionKind.Revive && kind != InteractionKind.DataStation
                && kind != InteractionKind.RecoverChip && kind != InteractionKind.RelayDrill)) return false;
            if (Kind != InteractionKind.None && (Kind != kind || Target != target))
            { Stop(true); return false; }
            Kind = kind; Target = target; return true;
        }
        public void Stop(bool requireRelease) => RequiresRelease |= requireRelease;
        public void Release() { Kind = InteractionKind.None; Target = 0; RequiresRelease = false; }
    }
}
