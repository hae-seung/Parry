using UnityEngine;
using UnityEngine.Serialization;

namespace Parry.MonsterClass
{
    public enum MonsterAttackType
    {
        Normal,
        Heavy
    }

    public enum MonsterAttackDistanceCondition
    {
        WithinRange,
        BeyondRange
    }

    [CreateAssetMenu(fileName = "MonsterAttack", menuName = "Parry/Monster Attack Data")]
    public sealed class MonsterAttackData : ScriptableObject
    {
        [Tooltip("공격 시작 시 클립 재생기로 재생할 애니메이션입니다. 반복을 끈 클립을 지정합니다.")]
        [SerializeField] private AnimationClip animationClip;
        [Tooltip("타격 이벤트가 적중했을 때 플레이어에게 주는 체력 피해입니다.")]
        [SerializeField, Min(0f)] private float damage = 10f;
        [Tooltip("일반·강공격에 따른 피격 반응의 지속 시간을 선택합니다. 밀림 거리는 공격별로 설정합니다.")]
        [SerializeField] private MonsterAttackType attackType;
        [Tooltip("켜면 넉백 모션과 밀림을 적용하고, 넉백 중 모든 행동을 막습니다.")]
        [SerializeField] private bool canKnockback = true;
        [Tooltip("평평한 지면에서 밀릴 목표 거리입니다. 0이면 밀리지 않습니다. 벽과 바닥 충돌은 그대로 적용합니다.")]
        [SerializeField, Min(0f)] private float knockbackDistance = 0.3f;
        [Tooltip("몬스터 Transform 원점에서 판정 중심까지의 월드 단위 거리입니다. X는 공격 방향에 맞춰 반전합니다.")]
        [SerializeField] private Vector2 hitboxOffset = new Vector2(0.9f, 0f);
        [Tooltip("타격 이벤트 순간 검사하는 사각형의 너비와 높이입니다. 월드 단위입니다.")]
        [SerializeField] private Vector2 hitboxSize = new Vector2(1.8f, 1.4f);
        [Tooltip("WithinRange는 사거리 이하, BeyondRange는 사거리 초과일 때 사용할 수 있습니다.")]
        [SerializeField] private MonsterAttackDistanceCondition distanceCondition;
        [Tooltip("공격 선택에 사용하는 몬스터와 플레이어 위치 사이의 2D 거리 기준입니다. 월드 단위이며 실제 타격 범위와는 별도입니다.")]
        [SerializeField, Min(0f)] private float range = 1.2f;
        [Tooltip("공격 시작 순간부터 다시 사용할 수 있을 때까지의 시간(초)입니다. 0이면 모션과 후딜레이 종료 후 쿨타임 대기 없이 재사용할 수 있습니다.")]
        [SerializeField, Min(0f)] private float cooldown = 3f;
        [Tooltip("공격 모션이 끝난 것을 확인한 뒤 다음 행동까지 기다리는 후딜레이 시간(초)입니다.")]
        [SerializeField, Min(0f)] private float attackEndDelayDuration = 0.5f;

        public AnimationClip AnimationClip => animationClip;
        public float Damage => damage;
        public MonsterAttackType AttackType => attackType;
        public bool CanKnockback => canKnockback;
        public float KnockbackDistance => knockbackDistance;
        public bool CausesFaint => attackType == MonsterAttackType.Heavy;
        public Vector2 HitboxOffset => hitboxOffset;
        public Vector2 HitboxSize => hitboxSize;
        public float Cooldown => cooldown;
        public float AttackEndDelayDuration => attackEndDelayDuration;

        public bool CanUseAtDistance(float distanceSquared)
        {
            bool withInRange = distanceSquared <= range * range;
            
            return distanceCondition == MonsterAttackDistanceCondition.WithinRange
                ? withInRange : !withInRange;
        }
    }
}
