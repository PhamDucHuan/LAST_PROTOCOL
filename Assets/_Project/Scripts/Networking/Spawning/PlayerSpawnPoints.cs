using UnityEngine;

namespace LastProtocol
{
    public sealed class PlayerSpawnPoints : MonoBehaviour
    {
        public Transform[] points;
        public int Count => points == null ? 0 : points.Length;
        public Transform GetPoint(int slot)
        {
            if (slot < 0 || slot >= Count || points[slot] == null)
                throw new System.InvalidOperationException("Invalid player spawn slot: " + slot);
            return points[slot];
        }
    }
}
