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
                || !Positive(elapsed) || before.SchemaVersion != definition.schemaVersion
                || definition.objectives[before.ObjectiveIndex].kind != ObjectiveKind.Rendezvous) return before;
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
        public static bool CanUseStation(MissionDefinition definition, MissionSnapshot state, string id)
        {
            if (definition == null || state.MissionId != definition.missionId || state.SchemaVersion != definition.schemaVersion
                || state.Phase != MissionPhase.Active || state.ObjectiveIndex < 0 || state.ObjectiveIndex >= definition.objectives.Length) return false;
            var objective = definition.objectives[state.ObjectiveIndex];
            int bit = definition.StationIndex(id);
            return objective.kind == ObjectiveKind.Stations && bit >= 0 && bit < 32
                && (state.CompletedStations & (1u << bit)) == 0 && System.Array.IndexOf(objective.stationIds, id) >= 0;
        }
        public static int Collected(MissionDefinition definition, MissionSnapshot state, int objective)
        {
            int count = 0;
            foreach (string id in definition.objectives[objective].stationIds)
            { int bit = definition.StationIndex(id); if (bit >= 0 && (state.CompletedStations & (1u << bit)) != 0) count++; }
            return count;
        }
        public static MissionSnapshot CompleteStation(MissionDefinition definition, MissionSnapshot before, string id)
        {
            if (!CanUseStation(definition, before, id)) return before;
            var next = before; next.CompletedStations |= 1u << definition.StationIndex(id);
            if (Collected(definition, next, next.ObjectiveIndex) == definition.objectives[next.ObjectiveIndex].stationIds.Length)
            {
                next.ObjectiveIndex++; next.Inside = next.Required = 0; next.HeldSeconds = 0;
                if (next.ObjectiveIndex == definition.objectives.Length) next.Phase = MissionPhase.Completed;
            }
            return next;
        }
        // A linear dependency graph: every objective depends on its immediate predecessor.
        public static ObjectiveStatus Status(MissionSnapshot state, int index, bool channeling = false)
        {
            if (state.Phase == MissionPhase.None || index > state.ObjectiveIndex) return ObjectiveStatus.Locked;
            if (index < state.ObjectiveIndex || state.Phase == MissionPhase.Completed) return ObjectiveStatus.Completed;
            if (state.Phase == MissionPhase.Failed) return ObjectiveStatus.Failed;
            return channeling || state.HeldSeconds > 0 ? ObjectiveStatus.Active : ObjectiveStatus.Available;
        }
    }
}
