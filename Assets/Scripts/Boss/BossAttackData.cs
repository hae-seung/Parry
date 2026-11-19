using Parry.MonsterClass;
using UnityEngine;

namespace Parry.BossClass
{
    [CreateAssetMenu(fileName = "BossAttack", menuName = "Parry/Boss Attack Data")]
    public sealed class BossAttackData : ScriptableObject
    {
        [SerializeField] private AnimationClip animationClip;
        [SerializeField] private MonsterAttackDistanceCondition distanceCondition;
        [SerializeField, Min(0f)] private float range = 1.2f;
        [SerializeField, Min(0f)] private float cooldown = 3f;
        [SerializeField, Min(0f)] private float attackEndDelayDuration = 0.5f;

        public AnimationClip AnimationClip => animationClip;
        public float Cooldown => cooldown;
        public float AttackEndDelayDuration => attackEndDelayDuration;

        public bool CanUseAtDistance(float distanceSquared)
        {
            bool withinRange = distanceSquared <= range * range;
            return distanceCondition == MonsterAttackDistanceCondition.WithinRange
                ? withinRange : !withinRange;
        }
    }
}
