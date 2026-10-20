using System.Reflection;
using NUnit.Framework;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEngine;

namespace LastProtocol.Tests
{
    public sealed class NetworkPresentationTests
    {
        [TestCase("Assets/_Project/Prefabs/Player/NetworkPlayer.prefab")]
        [TestCase("Assets/_Project/Prefabs/Networking/RobotEnemy.prefab")]
        public void MovingActorsRetainServerAuthorityAndLossTolerantInterpolation(string path)
        {
            var sync = AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<NetworkTransform>();
            Assert.IsTrue(sync.Interpolate);
            Assert.IsTrue(sync.UseUnreliableDeltas);
            Assert.AreEqual(NetworkTransform.InterpolationTypes.Lerp, sync.PositionInterpolationType);
            Assert.AreEqual(NetworkTransform.InterpolationTypes.Lerp, sync.RotationInterpolationType);
            var serialized = new SerializedObject(sync);
            Assert.AreEqual(0, serialized.FindProperty("AuthorityMode").intValue, "Server authority must be retained.");
        }
        [Test]
        public void CameraPivotUsesLatestTargetInLateUpdateEvenWithoutMouseFocus()
        {
            var cameraObject = new GameObject("Camera timing fixture");
            var target = new GameObject("Target");
            var pivot = new GameObject("Pivot");
            try
            {
                var camera = cameraObject.AddComponent<LocalPlayerCamera>();
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                typeof(LocalPlayerCamera).GetField("playerTarget", flags).SetValue(camera, target.transform);
                typeof(LocalPlayerCamera).GetField("pivot", flags).SetValue(camera, pivot.transform);
                // Represents NGO's PreLateUpdate moving the target after input Update.
                target.transform.position = new Vector3(12, 3, 9);
                typeof(LocalPlayerCamera).GetMethod("LateUpdate", flags).Invoke(camera, null);
                Assert.AreEqual(target.transform.position, pivot.transform.position);
                Assert.Less(typeof(LocalPlayerCamera).GetCustomAttribute<DefaultExecutionOrder>().order, 0);
            }
            finally
            {
                var component = cameraObject.GetComponent<LocalPlayerCamera>();
                if (component != null) typeof(LocalPlayerCamera).GetField("pivot", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(component, null);
                Object.DestroyImmediate(cameraObject);
                if (pivot != null) Object.DestroyImmediate(pivot);
                Object.DestroyImmediate(target);
            }
        }
    }
}
