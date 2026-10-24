using System;
using Unity.Netcode;
using UnityEngine;

namespace LastProtocol
{
    public struct EnemyMemory
    {
        public Vector3 Position;
        public ulong Target;
        public double ObservedAt, ExpiresAt;
        public float InitialConfidence;
        public float Confidence(double now) => ExpiresAt <= ObservedAt ? 0
            : Mathf.Clamp01((float)((ExpiresAt - now) / (ExpiresAt - ObservedAt))) * InitialConfidence;
        public void Observe(Vector3 position, ulong target, double now, float duration, float confidence)
        {
            if (!MissionRules.Finite(position) || !MissionRules.Positive(duration) || double.IsNaN(now) || double.IsInfinity(now) || float.IsNaN(confidence) || float.IsInfinity(confidence)) return;
            Position = position; Target = target; ObservedAt = now; ExpiresAt = now + duration; InitialConfidence = Mathf.Clamp01(confidence);
        }
    }
    public static class PerceptionRules
    {
        public static bool InCone(Vector3 eye, Vector3 forward, Vector3 point, float range, float angle)
        {
            if (!MissionRules.Finite(eye) || !MissionRules.Finite(point) || !MissionRules.Finite(forward)
                || !MissionRules.Positive(range) || !MissionRules.Positive(angle) || angle > 360) return false;
            Vector3 delta = point - eye;
            if (delta.sqrMagnitude > range * range || Mathf.Abs(delta.y) > 3) return false;
            delta.y = 0; forward.y = 0;
            return delta.sqrMagnitude < .01f || forward.sqrMagnitude > .01f && Vector3.Angle(forward, delta) <= angle * .5f;
        }
        public static bool CanHear(Vector3 ear, Vector3 point, float range, float intensity) => MissionRules.Finite(ear)
            && MissionRules.Finite(point) && MissionRules.Positive(intensity) && intensity <= 2
            && MissionRules.Positive(range) && Vector3.Distance(ear, point) <= range * intensity;
    }
    public struct AITelemetry
    {
        public int Shots, StaticShots, Hits, NearDamage, FarDamage, Downs, Revives, Transfers, TightSamples, SpreadSamples, Samples, HackActions;
        public TendencyTag Tag => Revives >= 2 || Downs >= 3 ? TendencyTag.FrequentRevive
            : Hits >= 4 && FarDamage > NearDamage * 2 && StaticShots * 2 >= Shots ? TendencyTag.LongRangeStatic
            : Hits >= 4 && NearDamage > FarDamage * 2 ? TendencyTag.CloseRangeHeavy
            : HackActions >= 3 && HackActions > Shots ? TendencyTag.HackDependent
            : Samples >= 12 && TightSamples * 3 >= Samples * 2 ? TendencyTag.TightFormation : TendencyTag.None;
    }
    public static class DirectorRules
    {
        public static bool Supported(AdaptiveModifier modifier) => modifier == AdaptiveModifier.ArmorResponse
            || modifier == AdaptiveModifier.CyberShield || modifier == AdaptiveModifier.FlankAmbush
            || modifier == AdaptiveModifier.ReviveHunter || modifier == AdaptiveModifier.AreaDenial;
        public static AdaptiveModifier Choose(TendencyTag tag, AdaptiveModifier[] whitelist)
        {
            var requested = tag switch { TendencyTag.CloseRangeHeavy => AdaptiveModifier.ArmorResponse,
                TendencyTag.LongRangeStatic => AdaptiveModifier.FlankAmbush, TendencyTag.TightFormation => AdaptiveModifier.AreaDenial,
                TendencyTag.FrequentRevive => AdaptiveModifier.ReviveHunter, TendencyTag.HackDependent => AdaptiveModifier.CyberShield,
                _ => AdaptiveModifier.None };
            return Supported(requested) && whitelist != null && Array.IndexOf(whitelist, requested) >= 0 ? requested : AdaptiveModifier.None;
        }
        public static int Band(int meter) => meter <= 25 ? 0 : meter <= 50 ? 1 : meter <= 75 ? 2 : meter < 100 ? 3 : 4;
        public static string BandName(int meter) => new[] { "Observing", "Alerted", "Hunting", "Emergency", "Protocol Zero" }[Band(Mathf.Clamp(meter, 0, 100))];
        public static int ArmorDamage(int amount, bool armored) => amount <= 0 ? 0 : armored ? Math.Max(1, Mathf.CeilToInt(amount * .8f)) : amount;
        public static int Retain(int current, int saved, int previousFloor, float fraction) => Mathf.Clamp(Math.Max(saved,
            Math.Max(previousFloor, Mathf.CeilToInt(current * Mathf.Clamp(fraction, .5f, 1)))), 0, 100);
    }
    public struct EnemyDirectorState : INetworkSerializable, IEquatable<EnemyDirectorState>
    {
        public uint Attempt;
        public int EncounterAlert, MissionMeter, CampaignThreat, Decisions;
        public AdaptiveModifier Modifier;
        public TendencyTag Tendency;
        public DeadlineReason Reason;
        public double ApplyAt;
        public bool SelectionLocked;
        public void NetworkSerialize<T>(BufferSerializer<T> s) where T : IReaderWriter
        {
            s.SerializeValue(ref Attempt); s.SerializeValue(ref EncounterAlert); s.SerializeValue(ref MissionMeter);
            s.SerializeValue(ref CampaignThreat); s.SerializeValue(ref Decisions); s.SerializeValue(ref Modifier);
            s.SerializeValue(ref Tendency); s.SerializeValue(ref Reason); s.SerializeValue(ref ApplyAt); s.SerializeValue(ref SelectionLocked);
        }
        public bool Equals(EnemyDirectorState x) => Attempt == x.Attempt && EncounterAlert == x.EncounterAlert
            && MissionMeter == x.MissionMeter && CampaignThreat == x.CampaignThreat && Decisions == x.Decisions
            && Modifier == x.Modifier && Tendency == x.Tendency && Reason == x.Reason && ApplyAt.Equals(x.ApplyAt) && SelectionLocked == x.SelectionLocked;
    }
}
