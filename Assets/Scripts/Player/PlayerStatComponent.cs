using System;
using UnityEngine;
using Parry.CombatClass;
using Random = UnityEngine.Random;

namespace Parry.PlayerClass
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerParryGauge))]
    public sealed class PlayerStatComponent : MonoBehaviour
    {
        private PlayerStatData data;

        private PlayerParryGauge gauge;
        private float nextStaminaRecoveryTime;

        public event Action<float, float> OnHpChanged;
        public event Action<float, float> OnStaminaChanged;

        public float Hp { get; private set; }
        public float MaxHp => data.maxHp;
        public float Stamina { get; private set; }
        public float MaxStamina => data.maxStamina;
        public float ParryCounterDamage => data.parryCounterDamage;
        public bool IsDead => Hp <= 0f;

        public void Initialize(PlayerStatData data)
        {
            this.data = data;
            Hp = data.maxHp;
            Stamina = data.maxStamina;
            gauge = GetComponent<PlayerParryGauge>();
        }

        private void Update()
        {
            if (IsDead || Stamina >= data.maxStamina || Time.time < nextStaminaRecoveryTime)
                return;

            
            Stamina = Mathf.Min(data.maxStamina, Stamina + data.staminaRecoveryAmount);
            nextStaminaRecoveryTime = Time.time + data.staminaRecoveryInterval;
            OnStaminaChanged?.Invoke(Stamina, data.maxStamina);
        }

        public void InitUI()
        {
            OnHpChanged?.Invoke(Hp, data.maxHp);
            OnStaminaChanged?.Invoke(Stamina, data.maxStamina);
        }

        public bool TryConsumeStamina(float amount)
        {
            if (Stamina < amount)
                return false;

            Stamina -= amount;
            if (amount > 0f)
                nextStaminaRecoveryTime = Time.time + data.staminaRecoveryInterval;
            OnStaminaChanged?.Invoke(Stamina, data.maxStamina);
            return true;
        }

        public MonsterHitContext CreateMonsterHit(float baseDamage, PlayerAttackType attackType)
        {
            float variance = data.damageVariancePercent * 0.01f;
            float damage = baseDamage * Random.Range(1f - variance, 1f + variance);
            // 패링 반격은 별도 스탯을 유지하고, 일반 공격만 게이지로 강화한다.
            if (attackType == PlayerAttackType.Normal)
                damage *= gauge.AttackMultiplier;
            
            bool isCritical = data.criticalChance >= 100f || Random.value < data.criticalChance * 0.01f;
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
            OnHpChanged?.Invoke(Hp, data.maxHp);
            
            if (IsDead && TryGetComponent<PlayerAttack>(out var attack))
                attack.CancelAttack();
        }
    }
}
