using System;
using System.IO;
using System.Linq;
using LastProtocol.Editor;
using NUnit.Framework;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
namespace LastProtocol.Tests
{
    public sealed class EnemyAITests
    {
        [TestCase(0,0), TestCase(25,0), TestCase(26,1), TestCase(50,1), TestCase(51,2), TestCase(75,2), TestCase(76,3), TestCase(99,3), TestCase(100,4)]
        public void DeadlineBandsAreDistinctFromChipFailure(int meter, int band) => Assert.AreEqual(band, DirectorRules.Band(meter));
        [Test] public void MemoryDecaysWithoutFollowingAnOccludedTarget()
        {
            var m = new EnemyMemory(); m.Observe(new Vector3(4,0,2), 17, 10, 8, 1);
            Assert.AreEqual(1, m.Confidence(10)); Assert.AreEqual(.5f,m.Confidence(14)); Assert.AreEqual(0,m.Confidence(18));
            Assert.AreEqual(new Vector3(4,0,2),m.Position);
            m.Observe(new Vector3(float.NaN,0,0), 3, 15, 8, 1); Assert.AreEqual(17,m.Target);
        }
        [Test] public void ConeRejectsRearOutsideAndInvalidCoordinates()
        {
            Assert.IsTrue(PerceptionRules.InCone(Vector3.zero,Vector3.forward,Vector3.forward*8,10,90));
            Assert.IsFalse(PerceptionRules.InCone(Vector3.zero,Vector3.forward,Vector3.back*8,10,90));
            Assert.IsFalse(PerceptionRules.InCone(Vector3.zero,Vector3.forward,Vector3.forward*11,10,90));
            Assert.IsFalse(PerceptionRules.InCone(Vector3.zero,Vector3.forward,Vector3.up*4,10,90));
            Assert.IsFalse(PerceptionRules.InCone(Vector3.zero,Vector3.forward,Vector3.one,float.NaN,90));
        }
        [Test] public void HearingRespectsIntensityAndRange()
        { Assert.IsTrue(PerceptionRules.CanHear(Vector3.zero,Vector3.forward*8,20,.5f)); Assert.IsFalse(PerceptionRules.CanHear(Vector3.zero,Vector3.forward*11,20,.5f)); Assert.IsFalse(PerceptionRules.CanHear(Vector3.zero,Vector3.one,20,float.NaN)); }
        [Test] public void UnsupportedCountersCannotBeSelectedAndWhitelistIsBinding()
        {
            Assert.IsFalse(DirectorRules.Supported(AdaptiveModifier.AntiSniperDrone)); Assert.IsFalse(DirectorRules.Supported(AdaptiveModifier.ResourcePressure));
            Assert.AreEqual(AdaptiveModifier.None,DirectorRules.Choose(TendencyTag.LongRangeStatic,new[]{AdaptiveModifier.ArmorResponse}));
            Assert.AreEqual(AdaptiveModifier.FlankAmbush,DirectorRules.Choose(TendencyTag.LongRangeStatic,new[]{AdaptiveModifier.FlankAmbush}));
        }
        [Test] public void TelemetryRequiresEvidenceAndDoesNotGuessClasses()
        {
            var t = new AITelemetry { Hits=3, NearDamage=80 }; Assert.AreEqual(TendencyTag.None,t.Tag);
            t.Hits=4; Assert.AreEqual(TendencyTag.CloseRangeHeavy,t.Tag);
            t.FarDamage=200; t.Shots=6; t.StaticShots=4; Assert.AreEqual(TendencyTag.LongRangeStatic,t.Tag);
            t.Revives=2; Assert.AreEqual(TendencyTag.FrequentRevive,t.Tag);
            Assert.AreEqual(TendencyTag.TightFormation,new AITelemetry{Samples=12,TightSamples=8}.Tag);
        }
        [Test] public void ArmorCannotBeAnAbsoluteCounter()
        { Assert.AreEqual(20,DirectorRules.ArmorDamage(25,true)); Assert.AreEqual(1,DirectorRules.ArmorDamage(1,true)); Assert.AreEqual(0,DirectorRules.ArmorDamage(-1,true)); }
        [Test] public void RetryRetentionHasAMonotonicFloor()
        {
            int first=DirectorRules.Retain(90,20,20,.5f); Assert.AreEqual(45,first);
            Assert.AreEqual(45,DirectorRules.Retain(first,20,first,.5f));
            Assert.AreEqual(60,DirectorRules.Retain(70,60,45,.5f));
        }
        [Test] public void TraceUsesExposureAndClampsAt100()
        {
            Assert.AreEqual(11,ChipAIRules.AdvanceTrace(10,ChipLocation.Dropped,.5f));
            Assert.AreEqual(14,ChipAIRules.AdvanceTrace(10,ChipLocation.Captured,.5f));
            Assert.AreEqual(10,ChipAIRules.AdvanceTrace(10,ChipLocation.Carried,.5f));
            Assert.AreEqual(100,ChipAIRules.AdvanceTrace(99,ChipLocation.Captured,.5f));
            Assert.AreEqual(10,ChipAIRules.AdvanceTrace(10,ChipLocation.Captured,float.NaN));
            Assert.IsFalse(new InteractionGate().TryHold(InteractionKind.EnemyCapture,1));
        }
        [Test] public void SixRegisteredRolesShareNavigationAndDoNotReplaceLegacyPrefabs()
        {
            var e=AssetDatabase.LoadAssetAtPath<EnemyEncounterProfile>(EnemyAISetupTool.EncounterPath); Assert.IsTrue(e.Validate(out string error),error);
            Assert.AreEqual(3,e.maximumAlive); Assert.AreEqual(3,e.opening.Length);
            var registry=AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>(MultiplayerSetupTool.PrefabListPath);
            var hashes=e.roster.Select(x=>new SerializedObject(x.GetComponent<NetworkObject>()).FindProperty("GlobalObjectIdHash").longValue).ToArray();
            Assert.AreEqual(6,hashes.Distinct().Count()); Assert.IsFalse(hashes.Contains(0));
            foreach(var r in e.roster){Assert.IsTrue(registry.Contains(r.gameObject)); Assert.AreEqual(100,r.GetComponent<EnemyBrain>().profile.maximumHealth); Assert.IsFalse(r.respawnOnDeath);}
            Assert.IsNull(AssetDatabase.LoadAssetAtPath<GameObject>(EnemySetupTool.EnemyPath).GetComponent<EnemyBrain>());
            Assert.IsNull(AssetDatabase.LoadAssetAtPath<GameObject>(UplinkDefenseSetupTool.EnemyPath).GetComponent<EnemyBrain>());
        }
        [Test] public void ProfileBoundsRejectNaNAndExcessiveDamage()
        {
            var p=ScriptableObject.CreateInstance<EnemyAIProfile>();p.profileId="TEST_AI";
            try { Assert.IsTrue(p.Validate(out _)); p.windup=float.NaN;Assert.IsFalse(p.Validate(out _));p.windup=.8f;p.damage=999;Assert.IsFalse(p.Validate(out _)); }
            finally { UnityEngine.Object.DestroyImmediate(p); }
        }
        [Test] public void InstallerIsIdempotent()
        {
            var paths=Enum.GetValues(typeof(EnemyRole)).Cast<EnemyRole>().SelectMany(x=>new[]{EnemyAISetupTool.ProfilePath(x),EnemyAISetupTool.PrefabPath(x)})
                .Concat(new[]{EnemyAISetupTool.EncounterPath,MultiplayerSetupTool.RoomPath,MultiplayerSetupTool.PrefabListPath,CanThoMapSetupTool.MenuPath}).ToArray();
            var before=paths.Select(File.ReadAllBytes).ToArray(); EnemyAISetupTool.Install();
            for(int i=0;i<paths.Length;i++)CollectionAssert.AreEqual(before[i],File.ReadAllBytes(paths[i]),paths[i]);
        }
    }
}
