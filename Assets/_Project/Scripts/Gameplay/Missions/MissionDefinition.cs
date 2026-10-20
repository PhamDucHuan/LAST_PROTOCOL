using System;
using System.Collections.Generic;
using UnityEngine;

namespace LastProtocol
{
    [Serializable]
    public sealed class RendezvousObjective
    {
        public string id, title;
        [TextArea] public string instruction;
        public Vector3 center;
        [Min(1)] public float radius = 6;
        [Min(0.5f)] public float heightTolerance = 2.5f;
        [Range(0.5f, 30)] public float holdSeconds = 2;
        public bool Contains(Vector3 point) => MissionRules.Finite(point)
            && Mathf.Abs(point.y - center.y) <= heightTolerance
            && new Vector2(point.x - center.x, point.z - center.z).sqrMagnitude <= radius * radius;
    }

    [CreateAssetMenu(menuName = "LastProtocol/Missions/Squad Recon Mission")]
    public sealed class MissionDefinition : ScriptableObject
    {
        public string missionId, title;
        [TextArea] public string briefing, completionText;
        public int levelId = LevelCatalog.CanTho;
        public RendezvousObjective[] objectives;

        public bool Validate(out string error)
        {
            error = string.Empty;
            if (!ValidId(missionId) || string.IsNullOrWhiteSpace(title) || !LevelCatalog.IsValid(levelId))
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
                        || !MissionRules.Positive(objective.holdSeconds) || objective.holdSeconds > 30)
                    { error = "Objective IDs must be unique; positions, radius and hold time must be valid."; break; }
            }
            return error.Length == 0;
        }
        private static bool ValidId(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length > 48) return false;
            foreach (char c in value) if (!(c >= 'A' && c <= 'Z') && !(c >= '0' && c <= '9') && c != '_') return false;
            return true;
        }
    }
}
