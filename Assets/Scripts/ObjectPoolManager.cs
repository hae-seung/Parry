using System.Collections.Generic;
using UnityEngine;

namespace Parry
{
    public sealed class ObjectPoolManager : Singleton<ObjectPoolManager>
    {
        public static bool IsQuitting { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetQuitState()
        {
            IsQuitting = false;
            Application.quitting -= MarkQuitting;
            Application.quitting += MarkQuitting;
        }
        private static void MarkQuitting() => IsQuitting = true;
        

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
