using System;
using Unity.Netcode;

namespace LastProtocol
{
    public enum UplinkEncounterPhase : byte { Dormant, Active, Cleared, Secured, Failed }
    public struct UplinkEncounterState : INetworkSerializable, IEquatable<UplinkEncounterState>
    {
        public UplinkEncounterPhase Phase;
        public uint Attempt, Activations;
        public int Spawned, Alive, Defeated;
        public void NetworkSerialize<T>(BufferSerializer<T> s) where T : IReaderWriter
        {
            s.SerializeValue(ref Phase); s.SerializeValue(ref Attempt); s.SerializeValue(ref Activations);
            s.SerializeValue(ref Spawned); s.SerializeValue(ref Alive); s.SerializeValue(ref Defeated);
        }
        public bool Equals(UplinkEncounterState other) => Phase == other.Phase && Attempt == other.Attempt
            && Activations == other.Activations && Spawned == other.Spawned && Alive == other.Alive && Defeated == other.Defeated;
    }
    public static class UplinkEncounterRules
    {
        public static bool ShouldActivate(MissionDefinition definition, MissionSnapshot mission, UplinkEncounterState wave)
        {
            if (definition == null || !ChapterPlan.Campus(definition.missionId) || wave.Attempt != mission.Attempt
                || wave.Phase != UplinkEncounterPhase.Dormant || wave.Activations != 0
                || !MissionRules.CanUseStation(definition, mission, DataStation.UplinkId)) return false;
            int generator = definition.StationIndex(DataStation.GeneratorId);
            return generator >= 0 && (mission.CompletedStations & (1u << generator)) != 0;
        }
    }
}
