using Parry.MonsterClass;
using UnityEngine;

namespace Parry.BossClass
{
    public sealed class BossAttackHitData : ScriptableObject
    {
        [SerializeField, Min(0f)] private float damage = 10f;
        [SerializeField] private MonsterAttackType attackType;
        [SerializeField] private bool canKnockback = true;
        [SerializeField, Min(0f)] private float knockbackDistance = 0.3f;
        [SerializeField] private bool causesFaint;
        [SerializeField] private Vector2 hitboxOffset = new Vector2(0.9f, 0f);
        [SerializeField] private Vector2 hitboxSize = new Vector2(1.8f, 1.4f);

        public float Damage => damage;
        public MonsterAttackType AttackType => attackType;
        public bool CanKnockback => canKnockback;
        public float KnockbackDistance => knockbackDistance;
        public bool CausesFaint => causesFaint;
        public Vector2 HitboxOffset => hitboxOffset;
        public Vector2 HitboxSize => hitboxSize;
    }
}
