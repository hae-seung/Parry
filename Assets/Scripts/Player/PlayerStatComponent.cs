using UnityEngine;
using Parry.CombatClass;

namespace Parry.PlayerClass
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerParryGauge))]
    public sealed class PlayerStatComponent : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float maxHp = 100f;
        [SerializeField, Min(1f)] private float maxStamina = 100f;
        [Tooltip("패링 성공 시 공격한 몬스터에게 주는 반격 피해량입니다.")]
        [SerializeField, Min(0f)] private float parryCounterDamage = 10f;
        [Tooltip("공격과 패링 반격의 치명타 확률(%)입니다.")]
        [SerializeField, Range(0f, 100f)] private float criticalChance = 10f;
        [Tooltip("공격과 패링 반격의 기본 피해량에 적용하는 ±난수 범위(%)입니다.")]
        [SerializeField, Range(0f, 100f)] private float damageVariancePercent = 10f;
        private PlayerParryGauge gauge;

        public float Hp { get; private set; }
        public float MaxHp => maxHp;
        public float Stamina { get; private set; }
        public float MaxStamina => maxStamina;
        public float ParryCounterDamage => parryCounterDamage;
        public bool IsDead => Hp <= 0f;

        private void Awake()
        {
            Hp = maxHp;
            Stamina = maxStamina;
            gauge = GetComponent<PlayerParryGauge>();
        }

        public bool TryConsumeStamina(float amount)
        {
            if (Stamina < amount)
                return false;

            Stamina -= amount;
            return true;
        }

        public MonsterHitContext CreateMonsterHit(float baseDamage, PlayerAttackType attackType)
        {
            float variance = damageVariancePercent * 0.01f;
            float damage = baseDamage * Random.Range(1f - variance, 1f + variance);
            // 패링 반격은 별도 스탯을 유지하고, 일반 공격만 게이지로 강화한다.
            if (attackType == PlayerAttackType.Normal)
                damage *= gauge.AttackMultiplier;
            
            bool isCritical = criticalChance >= 100f || Random.value < criticalChance * 0.01f;
            if (isCritical)
                damage *= 1.5f;

            // 표시 숫자와 실제 차감량이 같도록 최종 단계에서 정수로 반올림한다.
            damage = baseDamage > 0f ? Mathf.Max(1f, Mathf.Round(damage)) : 0f;
            
            return new MonsterHitContext(damage, attackType, isCritical);
        }
        

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
            if (IsDead && TryGetComponent<PlayerAttack>(out var attack))
                attack.CancelAttack();
        }
    }
}
