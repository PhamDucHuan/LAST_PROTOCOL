using UnityEngine;

namespace LastProtocol
{
    // Session checkpoints contain confirmed progress, never a partially held interaction.
    public static class CheckpointRules
    {
        public static bool Valid(MissionDefinition definition, MissionSnapshot saved)
        {
            if (definition == null || definition.objectives == null || saved.MissionId != definition.missionId
                || saved.SchemaVersion != definition.schemaVersion || saved.Attempt == 0 || saved.ObjectiveIndex < 0
                || saved.ObjectiveIndex > definition.objectives.Length
                || !(saved.Phase == MissionPhase.Active && saved.ObjectiveIndex < definition.objectives.Length
                    || saved.Phase == MissionPhase.Completed && saved.ObjectiveIndex == definition.objectives.Length)) return false;
            uint known = 0;
            for (int i = 0; i < definition.objectives.Length; i++)
            {
                var objective = definition.objectives[i]; if (objective.kind != ObjectiveKind.Stations) continue;
                uint mask = 0;
                foreach (string id in objective.stationIds)
                { int bit = definition.StationIndex(id); if (bit < 0 || bit >= 32) return false; mask |= 1u << bit; }
                known |= mask;
                uint completed = saved.CompletedStations & mask;
                if (i < saved.ObjectiveIndex && completed != mask || i > saved.ObjectiveIndex && completed != 0
                    || i == saved.ObjectiveIndex && completed == mask) return false;
            }
            return (saved.CompletedStations & ~known) == 0;
        }

        public static MissionSnapshot Capture(MissionSnapshot confirmed)
        { confirmed.Inside = confirmed.Required = 0; confirmed.HeldSeconds = 0; confirmed.Failure = MissionFailure.None; return confirmed; }

        public static MissionSnapshot Restore(MissionDefinition definition, MissionSnapshot saved, uint attempt)
        {
            if (!Valid(definition, saved) || attempt <= saved.Attempt) return default;
            saved = Capture(saved); saved.Attempt = attempt; return saved;
        }

        public static bool StationCompleted(MissionDefinition definition, MissionSnapshot saved, string id)
        { int bit = definition != null ? definition.StationIndex(id) : -1; return bit >= 0 && bit < 32 && (saved.CompletedStations & (1u << bit)) != 0; }

        public static string Name(MissionSnapshot saved) => saved.Phase == MissionPhase.Completed ? saved.MissionId==ChapterPlan.MissionId?"RESCUE SECURED":"COMMS RESTORED"
            : saved.MissionId==ChapterPlan.MissionId ? ChapterPlan.CheckpointNames[Mathf.Clamp(saved.ObjectiveIndex,0,11)]
            : saved.ObjectiveIndex >= 3 ? "GENERATOR ONLINE" : saved.ObjectiveIndex >= 2 ? "DATA SECURED"
            : saved.ObjectiveIndex >= 1 ? "ADMIN DATA" : "CAMPUS ENTRY";

        public static Vector3 Position(MissionSnapshot saved, int slot)
        {
            Vector3 center = saved.ObjectiveIndex >= 4 ? new(61, 1.1f, -44.5f)
                : saved.ObjectiveIndex >= 3 ? new(40, 1.1f, -35) : new(30, 1.1f, -27);
            return center + new Vector3((slot - 1.5f) * .85f, 0, (slot % 2) * .75f);
        }
        public static Vector3 Position(MissionDefinition definition,MissionSnapshot saved,int slot)
        {
            if(saved.MissionId!=ChapterPlan.MissionId||definition==null)return Position(saved,slot);
            var center=definition.objectives[Mathf.Clamp(saved.ObjectiveIndex,0,definition.objectives.Length-1)].checkpointPosition;
            return center+new Vector3((slot-1.5f)*.85f,1.1f,(slot%2)*.75f);
        }
    }
}
