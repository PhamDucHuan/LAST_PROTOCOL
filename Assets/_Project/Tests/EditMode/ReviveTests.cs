using System;
using System.IO;
using LastProtocol.Editor;
using NUnit.Framework;
using UnityEngine;

namespace LastProtocol.Tests
{
    public sealed class ReviveTests
    {
        [Test]
        public void ReviveOnlyChangesDownedHealthAndNormalHealNeverRevives()
        {
            var alive = new HealthState(100).Damage(25);
            Assert.AreEqual(alive, alive.Revive(40));
            var down = alive.Damage(100);
            Assert.AreEqual(down, down.Heal(100));
            Assert.AreEqual(40, down.Revive(40).Current);
            Assert.IsFalse(down.Revive(40).IsDowned);
            Assert.AreEqual(100, down.Revive(int.MaxValue).Current);
            Assert.AreEqual(default(HealthState), default(HealthState).Revive(40));
        }
        [TestCase(0)] [TestCase(-1)] [TestCase(int.MinValue)]
        public void InvalidReviveAmountCannotChangeDownedHealth(int amount)
        { var down = new HealthState(100).Damage(100); Assert.AreEqual(down, down.Revive(amount)); }
        [TestCase(100, 0.4f, 40)] [TestCase(1, 0.4f, 1)] [TestCase(101, 0.4f, 40)]
        [TestCase(int.MaxValue, 1f, int.MaxValue)] [TestCase(100, 0f, 0)] [TestCase(100, 2f, 0)]
        public void RestoreFractionRoundsClampsAndNeverOverflows(int max, float fraction, int expected)
            => Assert.AreEqual(expected, ReviveRules.RestoredHealth(max, fraction));
        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
        public void EveryEligibilityGateIsRequired(int gate)
        {
            var flags = new[] { true, true, true, true, true }; flags[gate] = false;
            Assert.IsFalse(ReviveRules.CanStart(flags[0], flags[1], flags[2], flags[3], flags[4], 1, 2.5f));
        }
        [Test]
        public void RangeBoundaryAndInvalidDistancesAreValidated()
        {
            Assert.IsTrue(ReviveRules.CanStart(true, true, true, true, true, 2.5f, 2.5f));
            foreach (float distance in new[] { 2.501f, -1f, float.NaN, float.PositiveInfinity })
                Assert.IsFalse(ReviveRules.CanStart(true, true, true, true, true, distance, 2.5f));
            Assert.AreEqual(0, ReviveRules.RestoredHealth(100, float.NaN));
        }
        [Test]
        public void InstallerIsIdempotentAndPreservesScenes()
        {
            ReviveSetupTool.Install();
            string[] paths = { MultiplayerSetupTool.PlayerPath, MultiplayerSetupTool.MenuPath, MultiplayerSetupTool.GamePath };
            var bytes = Array.ConvertAll(paths, File.ReadAllBytes);
            ReviveSetupTool.Install(); ReviveSetupTool.Install();
            for (int i = 0; i < paths.Length; i++) CollectionAssert.AreEqual(bytes[i], File.ReadAllBytes(paths[i]), paths[i]);
        }
        [Test]
        public void ReviveConfigurationRejectsInvalidValues()
        {
            var config = ScriptableObject.CreateInstance<MultiplayerConfig>();
            try
            {
                Assert.IsTrue(config.IsValid);
                config.reviveDuration = float.NaN; Assert.IsFalse(config.IsValid);
                config.reviveDuration = 3; config.reviveHealthFraction = 0; Assert.IsFalse(config.IsValid);
                config.reviveHealthFraction = 0.4f; config.reviveHeartbeatTimeout = 4; Assert.IsFalse(config.IsValid);
            }
            finally { UnityEngine.Object.DestroyImmediate(config); }
        }
    }
}
