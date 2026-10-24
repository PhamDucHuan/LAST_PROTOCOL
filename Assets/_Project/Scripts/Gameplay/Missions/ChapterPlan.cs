using System;
using UnityEngine;
namespace LastProtocol
{
    public static class ChapterPlan
    {
        public const string MissionId = "CTU_01_CAMPUS_RESCUE";
        public static bool Campus(string id) => id == MissionId || id == UplinkEncounterDefinition.MissionId;
        public static bool Timed(ObjectiveKind kind) => kind == ObjectiveKind.HoldZone || kind == ObjectiveKind.SplitScan;
        public static readonly string[] DeviceIds = { "CTU_GRID_EAST", "CTU_GRID_WEST", "CTU_ARCHIVE_B1", "CTU_ARCHIVE_RLC", "CTU_ROUTE_EDU", "CTU_ROUTE_ATL" };
        public static readonly Vector3[] DevicePositions = { new(78,0,33.5f),new(26,0,29),new(-9,0,106.06f),new(-73,0,124.11f),new(-108,0,20),new(-65,0,-32) };
        public static readonly string[] CheckpointNames = { "CAMPUS ENTRY","ADMIN DATA","DATA SECURED","GENERATOR ONLINE","LIBRARY LINK","GRID SWITCHES","GRID SYNC","EMERGENCY LOGS","SIGNAL SYNC","RADIO ROUTE","RESCUE BROADCAST","EXTRACTION" };
        public static string Clock(float seconds) { int s=Mathf.Max(0,Mathf.CeilToInt(seconds));return $"{s/60:00}:{s%60:00}"; }
        public static MissionSnapshot Observe(MissionDefinition definition,MissionSnapshot state,int insideA,int insideB,bool chipSafe,float elapsed)
        {
            if(definition==null||state.MissionId!=definition.missionId||state.SchemaVersion!=definition.schemaVersion||state.Phase!=MissionPhase.Active
                ||state.ObjectiveIndex<0||state.ObjectiveIndex>=definition.objectives.Length||!MissionRules.Positive(elapsed)
                ||insideA<0||insideA>4||insideB<0||insideB>4)return state;
            var objective=definition.objectives[state.ObjectiveIndex];if(!Timed(objective.kind))return state;
            state.Required=objective.kind==ObjectiveKind.SplitScan?2:1;
            state.Inside=(insideA>0?1:0)+(objective.kind==ObjectiveKind.SplitScan&&insideB>0?1:0);
            // Occupancy is continuous; confirmed work pauses rather than resetting on a short reposition.
            if(!chipSafe||state.Inside<state.Required)return state;
            state.HeldSeconds=Mathf.Min(objective.holdSeconds,state.HeldSeconds+Mathf.Min(elapsed,.25f));
            if(state.HeldSeconds+.0001f<objective.holdSeconds)return state;
            state.ObjectiveIndex++;state.HeldSeconds=0;state.Inside=state.Required=0;
            if(state.ObjectiveIndex==definition.objectives.Length)state.Phase=MissionPhase.Completed;
            return state;
        }
    }
}
