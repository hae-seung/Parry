using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Parry
{
    public sealed class Pool
    {
        private List<GameObject> objectList;
        private int maxSize = 0;
        private Transform storageParent;
        
        public Pool(int maxSize, Transform storageParent = null)
        {
            objectList = new List<GameObject>();
            this.maxSize = maxSize;
            this.storageParent = storageParent;
        }

        public GameObject SpawnObject(GameObject prefab, Vector3 pos, Transform parent = null)
        {
            if (objectList.Count == 0)
            {
                CreateNewObjectAndInsert(prefab);
            }

            GameObject go = objectList[objectList.Count - 1];
            objectList.RemoveAt(objectList.Count - 1);
            
            go.transform.SetParent(parent);
            go.transform.position = pos;
            go.SetActive(true);

            
            return go;
        }


        void CreateNewObjectAndInsert(GameObject prefab)
        {
            GameObject newObject = Object.Instantiate(prefab);
            PooledObject poolable = newObject.GetComponent<PooledObject>();
            
            if (poolable == null)
            {
                poolable = newObject.AddComponent<PooledObject>();
            }
            
            poolable.SetPool(this);
            objectList.Add(newObject);
        }

        public void Release(GameObject instance)
        {
            if (ObjectPoolManager.IsQuitting)
                return;

            if (instance == null || objectList.Contains(instance))
                return;

            if (objectList.Count >= maxSize)
            {
                Object.Destroy(instance);
                return;
            }
            
            instance.SetActive(false);
            
            instance.transform.SetParent(storageParent);
            objectList.Add(instance);
        }
    }
}
