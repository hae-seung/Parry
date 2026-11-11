using Parry.MonsterClass;

namespace Parry.CombatClass
{
    // 타격 순간의 피해·공격자·피격 반응 정보를 하나로 묶어 전달한다.
    public readonly struct AttackHitContext
    {
        public float Damage { get; }
        public Monster Attacker { get; }
        public MonsterAttackType AttackType { get; }
        public bool CausesStagger { get; }
        public float KnockbackDistance { get; }
        public bool KnocksDown { get; }
        public int AttackDirection { get; }

        public AttackHitContext(MonsterAttackData attackData, Monster attacker, int attackDirection)
        {
            Damage = attackData.Damage;
            Attacker = attacker;
            AttackType = attackData.AttackType;
            CausesStagger = attackData.CausesStagger;
            KnockbackDistance = attackData.KnockbackDistance;
            KnocksDown = attackData.KnocksDown;
            AttackDirection = attackDirection;
        }

        public AttackHitContext(float damage, Monster attacker = null)
        {
            Damage = damage;
            Attacker = attacker;
            AttackType = MonsterAttackType.Normal;
            CausesStagger = true;
            KnockbackDistance = 0f;
            KnocksDown = false;
            AttackDirection = 0;
        }
    }
}
