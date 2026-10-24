using UnityEngine;

namespace LastProtocol
{
    public sealed class LevelInfo
    {
        public readonly int Id;
        public readonly string Title, SceneName, ScenePath, Summary, Briefing;
        public readonly Color Accent;
        internal LevelInfo(int id, string title, string sceneName, string folder, string summary, string briefing, Color accent)
        {
            Id = id; Title = title; SceneName = sceneName;
            ScenePath = "Assets/_Project/Scenes/" + folder + "/" + sceneName + ".unity";
            Summary = summary; Briefing = briefing; Accent = accent;
        }
    }

    // Stable IDs are part of the wire protocol. Never accept arbitrary scene names from Clients.
    public static class LevelCatalog
    {
        public const int Training = 0, Containment = 1, CanTho = 2, Count = 3;
        private static readonly LevelInfo[] levels =
        {
            new LevelInfo(Training, "TRAINING GROUNDS", MultiplayerConfig.GameplayScene, "MultiplayerTest",
                "Open arena / movement & weapon drills",
                "Calibrate your squad in an open combat simulator.\n\nFour shooting targets, one patrol robot and clear sightlines. Practice movement, aiming and teammate revives.",
                new Color(0.18f, 0.87f, 0.9f)),
            new LevelInfo(Containment, "CONTAINMENT YARD", "ContainmentYard", "ContainmentYard",
                "Cover arena / close-range co-op drills",
                "Deploy to an enclosed industrial training yard.\n\nUse concrete barriers to break sightlines, approach the robot and protect a teammate during revival.",
                new Color(1f, 0.65f, 0.28f)),
            new LevelInfo(CanTho, "CAN THO / CAMPUS II", "CanThoCampus", "CanThoCampus",
                "Chapter 1 / 15-20 min / 2-4 player co-op",
                "CHAPTER 1 / 15-20 MINUTES\n\nRecover data and relay the chip.\nRestore power and synchronize two sites.\nDefend the rescue broadcast.\n\nExtract together. Checkpoints save progress.",
                new Color(0.4f, 0.85f, 0.55f))
        };
        public static bool IsValid(int id) => id >= 0 && id < Count;
        public static LevelInfo Get(int id) => IsValid(id) ? levels[id] : null;
    }
}
