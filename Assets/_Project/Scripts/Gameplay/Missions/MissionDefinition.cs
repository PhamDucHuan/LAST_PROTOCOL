using System;
using System.Collections.Generic;
using UnityEngine;

namespace LastProtocol
{
    public enum ObjectiveKind : byte { Rendezvous, Stations, HoldZone, SplitScan }
    public enum ObjectiveStatus : byte { Locked, Available, Active, Completed, Failed, Skipped }
    [Serializable]
    public sealed class RendezvousObjective
    {
        public string id, title;
        public ObjectiveKind kind;
        public string[] stationIds;
        [TextArea] public string instruction;
        public Vector3 center;
        public Vector3 secondaryCenter, checkpointPosition;
        [Min(1)] public float radius = 6;
        [Min(0.5f)] public float heightTolerance = 2.5f;
        [Range(0.5f, 300)] public float holdSeconds = 2;
        public bool Contains(Vector3 point) => MissionRules.Finite(point)
            && Mathf.Abs(point.y - center.y) <= heightTolerance
            && new Vector2(point.x - center.x, point.z - center.z).sqrMagnitude <= radius * radius;
    }

    [CreateAssetMenu(menuName = "LastProtocol/Missions/Squad Recon Mission")]
    public sealed class MissionDefinition : ScriptableObject
    {
        public string missionId, title;
        // Version 0 assets remain the original Rendezvous chain; zero is the serialized default.
        public int schemaVersion;
        [TextArea] public string briefing, completionText;
        public int levelId = LevelCatalog.CanTho;
        public RendezvousObjective[] objectives;

        public bool Validate(out string error)
        {
            error = string.Empty;
            if (!ValidId(missionId) || string.IsNullOrWhiteSpace(title) || !LevelCatalog.IsValid(levelId) || schemaVersion < 0 || schemaVersion > 1)
                error = "Mission ID, title or level is invalid.";
            else if (objectives == null || objectives.Length < 1 || objectives.Length > 16)
                error = "Mission must contain between 1 and 16 objectives.";
            else
            {
                var ids = new HashSet<string>(StringComparer.Ordinal);
                foreach (var objective in objectives)
                    if (objective == null || !ValidId(objective.id) || !ids.Add(objective.id)
                        || string.IsNullOrWhiteSpace(objective.title) || !MissionRules.Finite(objective.center)
                        || !MissionRules.Positive(objective.radius) || !MissionRules.Positive(objective.heightTolerance)
                        || !MissionRules.Positive(objective.holdSeconds)
                        || objective.holdSeconds > (ChapterPlan.Timed(objective.kind) ? 300 : 30)
                        || !MissionRules.Finite(objective.secondaryCenter) || !MissionRules.Finite(objective.checkpointPosition)
                        || objective.kind == ObjectiveKind.SplitScan && Vector3.Distance(objective.center, objective.secondaryCenter) <= objective.radius * 2)
                    { error = "Objective IDs must be unique; positions, radius and hold time must be valid."; break; }
                var stations = new HashSet<string>(StringComparer.Ordinal);
                foreach (var objective in objectives)
                {
                    if (error.Length != 0) break;
                    if (objective.kind != ObjectiveKind.Rendezvous && objective.kind != ObjectiveKind.Stations && !ChapterPlan.Timed(objective.kind))
                    { error = "Unknown objective kind."; break; }
                    if (objective.kind != ObjectiveKind.Stations) continue;
                    if (schemaVersion != 1 || objective.stationIds == null || objective.stationIds.Length == 0)
                    { error = "Station objectives require schema 1 and stable device IDs."; break; }
                    foreach (string id in objective.stationIds)
                        if (!ValidId(id) || !stations.Add(id) || stations.Count > 32)
                        { error = "Device IDs must be globally unique, valid and bounded to 32."; break; }
                }
            }
            return error.Length == 0;
        }
        public int StationIndex(string id)
        {
            int index = 0;
            foreach (var objective in objectives)
                if (objective.kind == ObjectiveKind.Stations && objective.stationIds != null)
                    foreach (string candidate in objective.stationIds)
                    { if (candidate == id) return index; index++; }
            return -1;
        }
        internal static bool ValidId(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length > 48) return false;
            foreach (char c in value) if (!(c >= 'A' && c <= 'Z') && !(c >= '0' && c <= '9') && c != '_') return false;
            return true;
        }
    }
}
