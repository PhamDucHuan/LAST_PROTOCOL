using System;
using System.IO;
using LastProtocol.Editor;
using NUnit.Framework;
using UnityEngine;

namespace LastProtocol.Tests
{
    public sealed class CombatTests
    {
        [Test]
        public void ShotGateRejectsSpamAndReplaysAtServerCadence()
        {
            var gate = new ShotGate();
            Assert.IsTrue(gate.TryAccept(0, 1, 0.25f));
            Assert.IsFalse(gate.TryAccept(0.01, 2, 0.25f));
            Assert.IsFalse(gate.TryAccept(0.25, 2, 0.25f));
            Assert.IsTrue(gate.TryAccept(0.25, 3, 0.25f));
            Assert.IsFalse(gate.TryAccept(1, 1, 0.25f));
        }
        [Test]
        public void ShotSequenceHandlesUnsignedWrap()
        {
            var gate = new ShotGate();
            Assert.IsTrue(gate.TryAccept(0, uint.MaxValue, 0.25f));
            Assert.IsTrue(gate.TryAccept(0.25, 0, 0.25f));
            Assert.IsFalse(gate.TryAccept(0.5, uint.MaxValue, 0.25f));
        }
        [Test]
        public void MalformedClockOrIntervalCannotPassGate()
        {
            var gate = new ShotGate();
            Assert.IsFalse(gate.TryAccept(double.NaN, 1, 0.25f));
            Assert.IsFalse(gate.TryAccept(0, 1, float.PositiveInfinity));
            Assert.IsFalse(gate.TryAccept(0, 1, 0));
            Assert.IsTrue(gate.TryAccept(0, 1, 0.25f));
        }
        [Test]
        public void AimMustBeFiniteAndCloseToAuthoritativePlayer()
        {
            Assert.IsTrue(CombatRules.ValidAim(Vector3.zero, Vector3.back * 5, Vector3.forward, 7));
            Assert.IsFalse(CombatRules.ValidAim(Vector3.zero, Vector3.back * 100, Vector3.forward, 7));
            Assert.IsFalse(CombatRules.ValidAim(Vector3.zero, Vector3.zero, Vector3.zero, 7));
            Assert.IsFalse(CombatRules.ValidAim(Vector3.zero, Vector3.zero, new Vector3(float.NaN, 0, 1), 7));
            Assert.IsFalse(CombatRules.ValidAim(Vector3.zero, new Vector3(float.PositiveInfinity, 0, 0), Vector3.forward, 7));
        }
        [Test]
        public void RayRespectsCoverRangeTriggersAndOnlyIgnoresShooter()
        {
            var owner = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var trigger = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                Vector3 origin = new Vector3(2000, 1000, 2000);
                owner.transform.position = origin + Vector3.forward;
                trigger.transform.position = origin + Vector3.forward * 2;
                trigger.GetComponent<Collider>().isTrigger = true;
                wall.transform.position = origin + Vector3.forward * 4;
                target.transform.position = origin + Vector3.forward * 8;
                Physics.SyncTransforms();
                Assert.IsTrue(CombatRules.Raycast(origin, Vector3.forward, 10, owner.transform, out var hit));
                Assert.AreEqual(wall.GetComponent<Collider>(), hit.collider, "Cover must block a target behind it.");
                wall.GetComponent<Collider>().enabled = false;
                Assert.IsFalse(CombatRules.Raycast(origin, Vector3.forward, 5, owner.transform, out _));
                Assert.IsTrue(CombatRules.Raycast(origin, Vector3.forward, 10, owner.transform, out hit));
                Assert.AreEqual(target.GetComponent<Collider>(), hit.collider);
                Assert.IsTrue(CombatRules.Raycast(origin, Vector3.forward, 10, null, out hit));
                Assert.AreEqual(owner.GetComponent<Collider>(), hit.collider, "Other players must remain blockers.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner); UnityEngine.Object.DestroyImmediate(wall);
                UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(trigger);
            }
        }
        [Test]
        public void CombatConfigurationRejectsImpossibleValues()
        {
            var config = ScriptableObject.CreateInstance<MultiplayerConfig>();
            try
            {
                Assert.IsTrue(config.IsValid);
                Assert.IsFalse(config.friendlyFire);
                config.weaponDamage = 0; Assert.IsFalse(config.IsValid); config.weaponDamage = 25;
                config.weaponShotsPerSecond = 100; Assert.IsFalse(config.IsValid); config.weaponShotsPerSecond = 4;
                config.weaponRange = float.NaN; Assert.IsFalse(config.IsValid); config.weaponRange = 50;
                config.targetResetSeconds = 0; Assert.IsFalse(config.IsValid);
            }
            finally { UnityEngine.Object.DestroyImmediate(config); }
        }
        [Test]
        public void CombatInstallerIsIdempotentAndPreservesScenes()
        {
            string[] paths = { MultiplayerSetupTool.PlayerPath, MultiplayerSetupTool.PrefabListPath,
                CombatSetupTool.TargetPath, MultiplayerSetupTool.MenuPath, MultiplayerSetupTool.GamePath };
            var before = Array.ConvertAll(paths, File.ReadAllBytes);
            CombatSetupTool.Install(); CombatSetupTool.Install();
            for (int i = 0; i < paths.Length; i++) CollectionAssert.AreEqual(before[i], File.ReadAllBytes(paths[i]), paths[i]);
        }
    }
}
