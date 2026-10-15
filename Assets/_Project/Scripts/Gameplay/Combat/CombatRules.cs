using UnityEngine;

namespace LastProtocol
{
    public static class CombatRules
    {
        public static bool IsFinite(Vector3 value) => ConnectionPolicy.IsFinite(new Vector2(value.x, value.y))
            && !float.IsNaN(value.z) && !float.IsInfinity(value.z);

        public static bool ValidAim(Vector3 eye, Vector3 cameraOrigin, Vector3 direction, float maximumCameraOffset)
            => IsFinite(eye) && IsFinite(cameraOrigin) && IsFinite(direction)
            && direction.sqrMagnitude >= 0.25f && direction.sqrMagnitude <= 4
            && (cameraOrigin - eye).sqrMagnitude <= maximumCameraOffset * maximumCameraOffset;

        // Include cover and teammates in the closest-hit query; ignore only the shooter.
        public static bool Raycast(Vector3 origin, Vector3 direction, float distance, Transform shooter, out RaycastHit closest)
        {
            closest = default;
            float nearest = float.PositiveInfinity;
            foreach (var hit in Physics.RaycastAll(origin, direction, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (shooter != null && hit.collider.transform.IsChildOf(shooter)) continue;
                if (hit.distance >= nearest) continue;
                closest = hit; nearest = hit.distance;
            }
            return !float.IsPositiveInfinity(nearest);
        }
    }

    public sealed class ShotGate
    {
        private bool hasSequence;
        private uint lastSequence;
        private double nextAllowedTime;
        public bool TryAccept(double now, uint sequence, float interval)
        {
            if (double.IsNaN(now) || double.IsInfinity(now) || now < 0 || interval <= 0
                || float.IsNaN(interval) || float.IsInfinity(interval)) return false;
            if (hasSequence && unchecked((int)(sequence - lastSequence)) <= 0) return false;
            // Consume new sequences even when rate limited, so rejected requests cannot be replayed.
            lastSequence = sequence; hasSequence = true;
            if (now < nextAllowedTime) return false;
            nextAllowedTime = now + interval;
            return true;
        }
    }
}
