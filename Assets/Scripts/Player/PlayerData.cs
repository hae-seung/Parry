using System;
using System.Collections.Generic;
using UnityEngine;

namespace Parry.PlayerClass
{
    [Serializable]
    public struct PlayerCharacterData
    {
        [Min(0f)] public float rollCooldown;
        [Min(0f)] public float rollStaminaCost;
        [Range(0.01f,1f)] public float attackHitPlaybackSpeed;
        public AnimationClip rollClip;
        [Min(0f)] public float rollInvulnerabilityDuration;
    }

    [Serializable]
    public struct PlayerStatData
    {
        [Min(1f)] public float maxHp;
        [Min(1f)] public float maxStamina;
        [Min(0.01f)] public float staminaRecoveryInterval;
        [Min(0f)] public float staminaRecoveryAmount;
        [Min(0f)] public float parryCounterDamage;
        [Range(0f,100f)] public float criticalChance;
        [Range(0f,100f)] public float damageVariancePercent;
    }

    [Serializable]
    public struct PlayerMovementData
    {
        [Min(0f)] public float moveSpeed;
        [Min(0f)] public float jumpSpeed;
        [Min(0f)] public float groundCheckDistance;
        public LayerMask groundLayers;
        [Min(0f)] public float rollSpeed;
    }

    [Serializable]
    public struct PlayerAttackSetData
    {
        public List<PlayerAttackData> attacks;
        public List<BerserkerAttackData> berserkerAttacks;
        [Range(0f,100f)] public float berserkerStoredDamagePercent;
        [Min(0f)] public float comboResetDuration;
        public LayerMask targetLayers;
        public GameObject thirdHitEffectPrefab;
    }

    [Serializable]
    public struct PlayerParryData
    {
        public GameObject shieldBubblePrefab;
        public GameObject parryHitPrefab;
        public Vector3 shieldEffectOffset;
        public AnimationClip parryClip;
        [Min(0f)] public float parryDuration;
        [Min(0f)] public float parryCooldown;
        [Min(0f)] public float successCooldown;
    }

    [Serializable]
    public struct PlayerParryGaugeData
    {
        [Min(1f)] public float maxGauge;
        [Min(0f)] public float gaugePerParry;
        [Min(0f)] public float decayPerSecond;
        [Min(0f)] public float maxAttackBonus;
        [Min(0f)] public float initialGauge;
    }

    [Serializable]
    public struct PlayerBerserkerData
    {
        [Min(1f)] public float gaugeCost;
        [Min(1)] public int duration;
        [Min(0f)] public float cooldown;
        [Min(0f)] public float timeLossOnHit;
        [Min(0f)] public float timeRecoveryOnParry;
        public GameObject auraVfx;
        public Vector3 auraOffset;
        public GameObject burserkerHitVfx;
        public GameObject burserkerExecutionVfx;
        public AnimationClip roarClip;
    }

    [Serializable]
    public struct PlayerHitReactionData
    {
        public AnimationClip hurtClip;
        public AnimationClip faintClip;
        public AnimationClip getUpClip;
        public PlayerHitReactionSettings normalHit;
        public PlayerHitReactionSettings heavyHit;
        [Min(0f)] public float extraInvulnerability;
        [Min(0f)] public float faintHoldDuration;
    }

    [CreateAssetMenu(fileName="PlayerData",menuName="Parry/Player Data")]
    public sealed class PlayerData : ScriptableObject
    {
        [Tooltip("구르기와 공격 적중 연출 설정입니다.")]
        [SerializeField] private PlayerCharacterData character;
        [Tooltip("최대 체력·기력, 회복과 피해 계산 설정입니다.")]
        [SerializeField] private PlayerStatData stats;
        [SerializeField] private PlayerMovementData movement;
        [Tooltip("기존 일반·버서커 공격 데이터 목록과 판정 레이어입니다.")]
        [SerializeField] private PlayerAttackSetData attack;
        [SerializeField] private PlayerParryData parry;
        [Tooltip("시작 게이지와 획득·감소·공격 배율 설정입니다. 현재 게이지는 각 컴포넌트가 관리합니다.")]
        [SerializeField] private PlayerParryGaugeData parryGauge;
        [SerializeField] private PlayerBerserkerData berserker;
        [SerializeField] private PlayerHitReactionData hitReaction;

        public PlayerCharacterData Character => character;
        public PlayerStatData Stats => stats;
        public PlayerMovementData Movement => movement;
        public PlayerAttackSetData Attack => attack;
        public PlayerParryData Parry => parry;
        public PlayerParryGaugeData ParryGauge => parryGauge;
        public PlayerBerserkerData Berserker => berserker;
        public PlayerHitReactionData HitReaction => hitReaction;
    }
}
