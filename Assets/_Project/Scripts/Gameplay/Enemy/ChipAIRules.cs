using UnityEngine;
namespace LastProtocol
{
    public static class ChipAIRules
    {
        // Prototype rates: dropped 2/s, held by Hunter 8/s. MissionMeter is a separate system.
        public static float AdvanceTrace(float trace, ChipLocation location, float seconds)
        {
            if (float.IsNaN(trace) || float.IsInfinity(trace) || !MissionRules.Positive(seconds) || seconds > 1) return trace;
            float rate = location == ChipLocation.Captured ? 8 : location == ChipLocation.Dropped ? 2 : 0;
            return Mathf.Clamp(trace + rate * seconds, 0, 100);
        }
    }
}
