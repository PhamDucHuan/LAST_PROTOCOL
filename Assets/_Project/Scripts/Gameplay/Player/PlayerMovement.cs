using UnityEngine;

namespace LastProtocol
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        private CharacterController controller;
        private float verticalVelocity;
        private float jumpCooldown;
        public bool IsGrounded => controller != null && controller.isGrounded;
        public float PlanarSpeed { get; private set; }
        public float VerticalSpeed => verticalVelocity;
        private void Awake() => controller = GetComponent<CharacterController>();
        public bool Simulate(Vector2 input, bool sprint, bool jump, MultiplayerConfig config, float deltaTime)
        {
            if (controller == null) controller = GetComponent<CharacterController>();
            if (!controller.enabled || deltaTime <= 0) return false;
            input = Vector2.ClampMagnitude(input, 1);
            Vector3 direction = new Vector3(input.x, 0, input.y);
            jumpCooldown = Mathf.Max(0, jumpCooldown - deltaTime);
            if (controller.isGrounded && verticalVelocity < 0) verticalVelocity = -2;
            float gravity = Mathf.Abs(Physics.gravity.y) * config.gravityMultiplier;
            bool jumped = jump && controller.isGrounded && jumpCooldown <= 0;
            if (jumped)
            {
                verticalVelocity = Mathf.Sqrt(2 * gravity * config.jumpHeight);
                jumpCooldown = config.jumpCooldownSeconds;
            }
            verticalVelocity = Mathf.Max(verticalVelocity - gravity * deltaTime, -config.terminalFallSpeed);
            float speed = sprint ? config.sprintSpeed : config.movementSpeed;
            Vector3 previous = transform.position;
            CollisionFlags collisions = controller.Move((direction * speed + Vector3.up * verticalVelocity) * deltaTime);
            if ((collisions & CollisionFlags.Above) != 0 && verticalVelocity > 0) verticalVelocity = 0;
            Vector3 traveled = transform.position - previous;
            PlanarSpeed = new Vector2(traveled.x, traveled.z).magnitude / deltaTime;
            if (direction.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction), config.rotationSpeed * deltaTime);
            return jumped;
        }
    }
}
