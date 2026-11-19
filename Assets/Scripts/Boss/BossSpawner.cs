using UnityEngine;

namespace Parry.BossClass
{
    public sealed class BossSpawner : MonoBehaviour
    {
        [SerializeField] private Boss prefab;
        private Boss spawnedBoss;

        public Boss Spawn()
        {
            if (spawnedBoss == null)
                spawnedBoss = Instantiate(prefab, transform.position, transform.rotation);

            return spawnedBoss;
        }
    }
}
