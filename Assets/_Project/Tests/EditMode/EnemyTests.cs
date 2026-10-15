using System;
using System.IO;
using LastProtocol.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace LastProtocol.Tests
{
    public sealed class EnemyTests
    {
        [TestCase(true, true, true, 1f, true)]
        [TestCase(false, true, true, 1f, false)]
        [TestCase(true, false, true, 1f, false)]
        [TestCase(true, true, false, 1f, false)]
        [TestCase(true, true, true, 2.5f, false)]
        [TestCase(true, true, true, -1f, false)]
        public void MeleeRequiresLiveActorsSightAndRange(bool attacker, bool target, bool sight, float distance, bool expected)
            => Assert.AreEqual(expected, EnemyRules.CanHit(attacker, target, sight, distance, 1.8f));
        [Test]
        public void InvalidSpatialInputCannotAcquireOrHit()
        {
            Assert.IsFalse(EnemyRules.InRange(Vector3.zero, new Vector3(float.NaN, 0, 0), 12));
            Assert.IsFalse(EnemyRules.CanHit(true, true, true, float.PositiveInfinity, 1.8f));
            Assert.IsFalse(EnemyRules.CanHit(true, true, true, float.NaN, 1.8f));
        }
        [Test]
        public void ConfigurationRejectsBrokenPerceptionAndAttackTimings()
        {
            var config = ScriptableObject.CreateInstance<MultiplayerConfig>();
            try
            {
                Assert.IsTrue(config.IsValid);
                config.enemyLoseRange = 1; Assert.IsFalse(config.IsValid); config.enemyLoseRange = 18;
                config.enemyLeashDistance = 1; Assert.IsFalse(config.IsValid); config.enemyLeashDistance = 22;
                config.enemyAttackWindup = 0; Assert.IsFalse(config.IsValid); config.enemyAttackWindup = 0.45f;
                config.enemyAttackDamage = 0; Assert.IsFalse(config.IsValid);
            }
            finally { UnityEngine.Object.DestroyImmediate(config); }
        }
        [Test]
        public void EnemyRejectsDamageBeforeNetworkSpawn()
        {
            var root = new GameObject("Unspawned Enemy"); root.SetActive(false);
            try
            {
                root.AddComponent<NavMeshAgent>().enabled = false;
                var enemy = root.AddComponent<RobotEnemy>();
                Assert.IsFalse(enemy.TryApplyDamageServer(25));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        [Test]
        public void BakedNavigationConnectsSpawnToPlayersAndRoutesAroundPylons()
        {
            var data = AssetDatabase.LoadAssetAtPath<NavMeshData>(EnemySetupTool.NavigationPath);
            Assert.IsNotNull(data);
            var instance = NavMesh.AddNavMeshData(data);
            try
            {
                Assert.IsTrue(NavMesh.SamplePosition(new Vector3(0, 0, 15), out var from, 2, NavMesh.AllAreas));
                Assert.IsTrue(NavMesh.SamplePosition(new Vector3(0, 0, -8), out var to, 2, NavMesh.AllAreas));
                var path = new NavMeshPath();
                Assert.IsTrue(NavMesh.CalculatePath(from.position, to.position, NavMesh.AllAreas, path));
                Assert.AreEqual(NavMeshPathStatus.PathComplete, path.status);
                NavMesh.SamplePosition(new Vector3(-3, 0, 7), out from, 2, NavMesh.AllAreas);
                NavMesh.SamplePosition(new Vector3(-3, 0, 13), out to, 2, NavMesh.AllAreas);
                Assert.IsTrue(NavMesh.Raycast(from.position, to.position, out _, NavMesh.AllAreas), "Straight path must meet pylon exclusion.");
                Assert.IsTrue(NavMesh.CalculatePath(from.position, to.position, NavMesh.AllAreas, path));
                Assert.AreEqual(NavMeshPathStatus.PathComplete, path.status);
                Assert.GreaterOrEqual(path.corners.Length, 3);
            }
            finally { instance.Remove(); }
        }
        [Test]
        public void InstallerPreservesConfiguredAssetsAndScenes()
        {
            string[] paths = { EnemySetupTool.EnemyPath, EnemySetupTool.NavigationPath, MultiplayerSetupTool.PlayerPath,
                MultiplayerSetupTool.PrefabListPath, MultiplayerSetupTool.MenuPath, MultiplayerSetupTool.GamePath };
            var before = Array.ConvertAll(paths, File.ReadAllBytes);
            EnemySetupTool.Install(); EnemySetupTool.Install();
            for (int i = 0; i < paths.Length; i++) CollectionAssert.AreEqual(before[i], File.ReadAllBytes(paths[i]), paths[i]);
        }
    }
}
