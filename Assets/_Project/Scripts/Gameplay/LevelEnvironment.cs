using UnityEngine;
using UnityEngine.AI;

namespace LastProtocol
{
    [DisallowMultipleComponent]
    public sealed class LevelEnvironment : MonoBehaviour
    {
        public int levelId;
        public int artRevision = 1;
        public NavMeshData enemyNavigation;
    }
}
