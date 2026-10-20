using Unity.Netcode.Components;

namespace LastProtocol
{
    /// <summary>Shared defaults for newly generated server-authoritative moving actors.</summary>
    public static class NetworkMotionSettings
    {
        public static void Configure(NetworkTransform sync)
        {
            sync.UseUnreliableDeltas = true;
            sync.Interpolate = true;
            sync.PositionInterpolationType = NetworkTransform.InterpolationTypes.Lerp;
            sync.RotationInterpolationType = NetworkTransform.InterpolationTypes.Lerp;
            sync.PositionThreshold = 0.01f;
            sync.RotAngleThreshold = 0.5f;
            sync.PositionMaxInterpolationTime = 0.05f;
            sync.RotationMaxInterpolationTime = 0.05f;
        }
    }
}
