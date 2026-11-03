using UnityEngine;

namespace Parry.PlayerClass
{
    [DisallowMultipleComponent]
    public sealed class PlayerStatComponent : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float maxHp = 100f;

        public float Hp { get; private set; }
        public bool IsDead => Hp <= 0f;

        private void Awake() => Hp = maxHp;

        [ContextMenu("Take Damage (50)")]
        private void TakeTestDamage()
        {
            if (Application.isPlaying)
                TakeDamage(50f);
        }

        
        public void TakeDamage(float damage)
        {
            if (IsDead || !(damage > 0f) || float.IsInfinity(damage))
                return;

            Hp = Mathf.Max(0f, Hp - damage);
        }
    }
}
