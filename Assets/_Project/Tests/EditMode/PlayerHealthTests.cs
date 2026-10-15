using System;
using System.IO;
using LastProtocol.Editor;
using NUnit.Framework;
using UnityEngine;

namespace LastProtocol.Tests
{
    public sealed class PlayerHealthTests
    {
        [Test]
        public void StartsFullAndBecomesDownedExactlyAtZero()
        {
            var state = new HealthState(100);
            Assert.AreEqual(100, state.Current);
            Assert.AreEqual(1, state.Fraction);
            Assert.IsFalse(state.IsDowned);
            state = state.Damage(25);
            Assert.AreEqual(75, state.Current);
            Assert.AreEqual(0.75f, state.Fraction);
            state = state.Damage(75);
            Assert.AreEqual(0, state.Current);
            Assert.IsTrue(state.IsDowned);
            Assert.AreEqual(100, state.Maximum);
        }
        [TestCase(0)] [TestCase(-1)] [TestCase(int.MinValue)]
        public void NonPositiveDamageAndHealingCannotChangeHealth(int amount)
        {
            var state = new HealthState(100).Damage(25);
            Assert.AreEqual(state, state.Damage(amount));
            Assert.AreEqual(state, state.Heal(amount));
        }
        [Test]
        public void OverkillAndOversizedHealingNeverOverflow()
        {
            var state = new HealthState(int.MaxValue).Damage(1);
            Assert.AreEqual(int.MaxValue, state.Heal(int.MaxValue).Current);
            Assert.AreEqual(0, state.Damage(int.MaxValue).Current);
        }
        [Test]
        public void DownedStateCannotBeRevivedByNormalHealingOrRepeatedDamage()
        {
            var state = new HealthState(100).Damage(1000);
            Assert.AreEqual(state, state.Damage(25));
            Assert.AreEqual(state, state.Heal(int.MaxValue));
        }
        [TestCase(0)] [TestCase(-1)]
        public void InvalidMaximumIsRejected(int maximum) =>
            Assert.Throws<ArgumentOutOfRangeException>(() => new HealthState(maximum));
        [Test]
        public void UninitializedSnapshotIsNotAnActiveOrDownedPlayer()
        {
            var state = default(HealthState);
            Assert.IsFalse(state.IsInitialized);
            Assert.IsFalse(state.IsDowned);
            Assert.AreEqual(state, state.Damage(25));
            Assert.AreEqual(state, state.Heal(25));
        }
        [Test]
        public void UnspawnedComponentRejectsGameplayWrites()
        {
            var root = new GameObject("Health test");
            try
            {
                var health = root.AddComponent<PlayerHealth>();
                Assert.IsFalse(health.TryApplyDamageServer(25));
                Assert.IsFalse(health.TryHealServer(25));
                Assert.IsFalse(health.DebugResetHealthServer());
                Assert.IsFalse(health.CanAct);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        [Test]
        public void HealthInstallerPreservesAlreadyConfiguredPrefabAndScenes()
        {
            string[] paths = { MultiplayerSetupTool.PlayerPath, MultiplayerSetupTool.MenuPath, MultiplayerSetupTool.GamePath };
            var before = Array.ConvertAll(paths, File.ReadAllBytes);
            PlayerHealthSetupTool.Install();
            PlayerHealthSetupTool.Install();
            for (int i = 0; i < paths.Length; i++) CollectionAssert.AreEqual(before[i], File.ReadAllBytes(paths[i]), paths[i]);
        }
    }
}
