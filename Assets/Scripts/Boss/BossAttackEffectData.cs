using UnityEngine;

namespace Parry.BossClass
{
    public sealed class BossAttackEffectData : ScriptableObject
    {
        [SerializeField] private GameObject prefab;
        [SerializeField] private Vector2 offset;

        public GameObject Prefab => prefab;
        public Vector2 Offset => offset;
    }
}
