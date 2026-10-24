using System;
using Unity.Collections;
using Unity.Netcode;

namespace LastProtocol
{
    public enum MissionPhase : byte { None, Active, Completed, Failed }
    public enum MissionFailure : byte { None, TeamWiped, NotEnoughPlayers, EncounterUnavailable, ChipTraced }

    // One atomic snapshot avoids showing a new objective with the old objective's counter.
    public struct MissionSnapshot : INetworkSerializable, IEquatable<MissionSnapshot>
    {
        private FixedString64Bytes missionId;
        public string MissionId => missionId.ToString();
        public MissionPhase Phase;
        public MissionFailure Failure;
        public int ObjectiveIndex, Inside, Required;
        public float HeldSeconds;
        public uint Attempt;
        public uint CompletedStations;
        public int SchemaVersion;
        public static MissionSnapshot Begin(MissionDefinition definition, uint attempt) => new()
        { missionId = new FixedString64Bytes(definition.missionId), Phase = MissionPhase.Active, Attempt = attempt, SchemaVersion = definition.schemaVersion };
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref missionId);
            serializer.SerializeValue(ref Phase); serializer.SerializeValue(ref Failure);
            serializer.SerializeValue(ref ObjectiveIndex); serializer.SerializeValue(ref Inside);
            serializer.SerializeValue(ref Required); serializer.SerializeValue(ref HeldSeconds);
            serializer.SerializeValue(ref Attempt);
            serializer.SerializeValue(ref CompletedStations); serializer.SerializeValue(ref SchemaVersion);
        }
        public bool Equals(MissionSnapshot other) => missionId.Equals(other.missionId) && Phase == other.Phase
            && Failure == other.Failure && ObjectiveIndex == other.ObjectiveIndex && Inside == other.Inside
            && Required == other.Required && HeldSeconds.Equals(other.HeldSeconds) && Attempt == other.Attempt
            && CompletedStations == other.CompletedStations && SchemaVersion == other.SchemaVersion;
        public override bool Equals(object obj) => obj is MissionSnapshot other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(missionId, Phase, ObjectiveIndex, Attempt);
    }
}
