using UnityEngine;

namespace Parry
{
    public class PooledObject : MonoBehaviour
    {
        private Pool ownerPool;

        public void SetPool(Pool pool) => ownerPool = pool;

        public void Release() => ownerPool.Release(gameObject);
    }
}
