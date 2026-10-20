using UnityEngine;

namespace LastProtocol
{
    public static class MissionRules
    {
        public static bool Positive(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && value > 0;
        public static bool Finite(Vector3 point) => ConnectionPolicy.IsFinite(new Vector2(point.x, point.z))
            && !float.IsNaN(point.y) && !float.IsInfinity(point.y);

        public static MissionSnapshot Observe(MissionDefinition definition, MissionSnapshot before,
            int inside, int required, float elapsed)
        {
            if (definition == null || before.Phase != MissionPhase.Active || before.MissionId != definition.missionId
                || before.ObjectiveIndex < 0 || before.ObjectiveIndex >= definition.objectives.Length
                || required < 1 || required > MultiplayerConfig.MaxPlayers || inside < 0 || inside > required
                || !Positive(elapsed)) return before;
            var next = before;
            next.Inside = inside; next.Required = required;
            // A roster change or leaving the area breaks a continuous squad hold.
            bool continuous = inside == required && (before.Required == 0 || before.Required == required);
            next.HeldSeconds = continuous ? before.HeldSeconds + Mathf.Min(elapsed, 0.25f) : 0;
            if (next.HeldSeconds + 0.0001f < definition.objectives[before.ObjectiveIndex].holdSeconds) return next;
            next.ObjectiveIndex++;
            next.Inside = 0; next.HeldSeconds = 0;
            if (next.ObjectiveIndex == definition.objectives.Length) next.Phase = MissionPhase.Completed;
            return next;
        }
        public static MissionSnapshot Fail(MissionSnapshot before, MissionFailure reason)
        {
            if (before.Phase != MissionPhase.Active || reason == MissionFailure.None) return before;
            before.Phase = MissionPhase.Failed; before.Failure = reason; before.HeldSeconds = 0;
            return before;
        }
    }
}
