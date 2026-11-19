using System;
using System.Collections.Generic;
using Parry.MonsterClass;
using UnityEngine;

namespace Parry.BossClass
{
    [CreateAssetMenu(fileName = "BossPhaseData", menuName = "Parry/Boss Phase Data")]
    public sealed class BossPhaseData : ScriptableObject
    {
        [SerializeField] private string bossName;
        [SerializeField] private MonsterBaseStats baseStats = new MonsterBaseStats
        {
            maxHp = 500f, moveSpeed = 2f
        };
        [SerializeField] private BossSettings settings = new BossSettings { burstHitInterval = 0.1f };
        [SerializeField] private LayerMask targetLayers;
        [SerializeField] private List<BossDialogueData> dialogues = new List<BossDialogueData>();
        [SerializeField] private List<BossAttackData> attacks = new List<BossAttackData>();
        [SerializeField] private BossFlinchData flinch = new BossFlinchData { duration = 1f, resistance = 3 };
        [Tooltip("이동 시 벽과 낭떠러지를 검사할 지형 레이어입니다.")]
        [SerializeField] private LayerMask groundLayers = 1;
        [SerializeField] private MonsterWanderSettings wander = new MonsterWanderSettings
        {
            radius = 3f, waitDuration = 1f, travelTimeout = 3f, arrivalDistance = 0.1f
        };

        [SerializeField] private MonsterDetectionSettings detection = new MonsterDetectionSettings
        {
            obstacleLayers = 1, distance = 6f, heightTolerance = 0.5f
        };
        [SerializeField] private MonsterChaseSettings chase = new MonsterChaseSettings { stopDistance = 1.2f };
        [SerializeField] private MonsterCombatSettings combat = new MonsterCombatSettings
        {
            combatExitDistance = 9f, sightLossDuration = 3f
        };

        public string BossName => bossName;
        public MonsterBaseStats BaseStats => baseStats;
        public BossSettings Settings => settings;
        public LayerMask TargetLayers => targetLayers;
        public List<BossDialogueData> Dialogues => dialogues;
        public List<BossAttackData> Attacks => attacks;
        public BossFlinchData Flinch => flinch;
        public MonsterDetectionSettings Detection => detection;
        public MonsterChaseSettings Chase => chase;
        public MonsterCombatSettings Combat => combat;
        public LayerMask GroundLayers => groundLayers;
        public MonsterWanderSettings Wander => wander;
    }

    [Serializable]
    public struct BossSettings
    {
        public AnimationClip deathClip;
        [Min(0f)] public float burstHitInterval;
    }

    [Serializable]
    public struct BossDialogueData
    {
        [TextArea] public string text;
        [Min(0f)] public float duration;
    }
}
