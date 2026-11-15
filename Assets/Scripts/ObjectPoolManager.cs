using System.Collections.Generic;
using UnityEngine;

namespace Parry
{
    public sealed class ObjectPoolManager : Singleton<ObjectPoolManager>
    {
        [SerializeField, Min(1)] private int maxPoolSize = 16;
        
        private Dictionary<GameObject, Pool> pools = new Dictionary<GameObject, Pool>();


        public GameObject SpawnObject(GameObject prefab, Vector3 pos, Transform parent = null)
        {
            if (!pools.TryGetValue(prefab, out Pool pool))
            {
                pool = new Pool(maxPoolSize, transform);
                pools.Add(prefab, pool);
            }

            return pool.SpawnObject(prefab, pos, parent);
        }

        public void Release(GameObject instance)
        {
            if (instance != null)
            {
                PooledObject pooledObject = instance.GetComponent<PooledObject>();
                if(pooledObject)
                    pooledObject.Release();
            }
                
        }
    }
}
