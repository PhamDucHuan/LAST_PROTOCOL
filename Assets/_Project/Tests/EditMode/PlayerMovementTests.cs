using NUnit.Framework;
using UnityEngine;

namespace LastProtocol.Tests
{
    public sealed class PlayerMovementTests
    {
        private GameObject floor, player, ceiling;
        private PlayerMovement movement;
        private MultiplayerConfig config;

        [SetUp]
        public void SetUp()
        {
            config = ScriptableObject.CreateInstance<MultiplayerConfig>();
            // Keep physics fixtures well away from the user's open scene.
            floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.position = new Vector3(1000, -0.5f, 1000);
            floor.transform.localScale = new Vector3(100, 1, 100);
            player = new GameObject("Movement test", typeof(CharacterController), typeof(PlayerMovement));
            player.transform.position = new Vector3(1000, 1.05f, 1000);
            var controller = player.GetComponent<CharacterController>();
            controller.height = 2; controller.radius = 0.45f; controller.skinWidth = 0.05f;
            controller.minMoveDistance = 0;
            movement = player.GetComponent<PlayerMovement>();
            Physics.SyncTransforms();
            for (int i = 0; i < 30; i++) Step();
            Assert.IsTrue(movement.IsGrounded, "Fixture must settle on ground before testing.");
        }
        [TearDown]
        public void TearDown()
        {
            if (ceiling != null) Object.DestroyImmediate(ceiling);
            Object.DestroyImmediate(player); Object.DestroyImmediate(floor); Object.DestroyImmediate(config);
        }
        private bool Step(bool sprint = false, bool jump = false, Vector2? input = null, float dt = 0.02f) =>
            movement.Simulate(input ?? Vector2.zero, sprint, jump, config, dt);

        [TestCase(0.01f, 100)]
        [TestCase(0.02f, 50)]
        [TestCase(1f / 30, 30)]
        public void WalkDistanceUsesTimeStepRatherThanFrameCount(float dt, int steps)
        {
            float start = player.transform.position.x;
            for (int i = 0; i < steps; i++) Step(input: Vector2.right, dt: dt);
            Assert.That(player.transform.position.x - start, Is.EqualTo(config.movementSpeed).Within(0.04f));
        }
        [Test]
        public void SprintUsesServerConfiguredSpeedAndClampsOversizedInput()
        {
            float start = player.transform.position.x;
            for (int i = 0; i < 50; i++) Step(sprint: true, input: new Vector2(100, 0));
            Assert.That(player.transform.position.x - start, Is.EqualTo(config.sprintSpeed).Within(0.04f));
        }
        [Test]
        public void JumpIsGroundedOnlyAndReturnsToFloorWithoutRepeatedAirJumps()
        {
            float ground = player.transform.position.y;
            Assert.IsTrue(Step(jump: true));
            float maximum = player.transform.position.y;
            int extraJumps = 0;
            for (int i = 0; i < 16; i++)
            {
                if (Step(jump: true)) extraJumps++;
                maximum = Mathf.Max(maximum, player.transform.position.y);
            }
            for (int i = 0; i < 150; i++) { Step(); maximum = Mathf.Max(maximum, player.transform.position.y); }
            Assert.AreEqual(0, extraJumps, "Requests while airborne must never double-jump.");
            Assert.That(maximum - ground, Is.InRange(config.jumpHeight - 0.2f, config.jumpHeight + 0.1f));
            Assert.IsTrue(movement.IsGrounded);
            Assert.That(player.transform.position.y, Is.EqualTo(ground).Within(0.06f));
        }
        [Test]
        public void CeilingStopsUpwardVelocity()
        {
            ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceiling.transform.position = new Vector3(1000, 2.55f, 1000);
            ceiling.transform.localScale = new Vector3(5, 0.2f, 5);
            Physics.SyncTransforms();
            Assert.IsTrue(Step(jump: true));
            bool hit = false;
            for (int i = 0; i < 25; i++)
            {
                float before = movement.VerticalSpeed;
                Step();
                if (before > 0 && movement.VerticalSpeed <= 0) hit = true;
            }
            Assert.IsTrue(hit);
            Assert.Less(player.transform.position.y, 1.55f);
        }
        [Test]
        public void InvalidMovementConfigurationIsRejected()
        {
            config.sprintSpeed = config.movementSpeed - 1; Assert.IsFalse(config.IsValid);
            config.sprintSpeed = 8; config.jumpHeight = float.NaN; Assert.IsFalse(config.IsValid);
            config.jumpHeight = 1.4f; config.gravityMultiplier = float.PositiveInfinity; Assert.IsFalse(config.IsValid);
        }
    }
}
