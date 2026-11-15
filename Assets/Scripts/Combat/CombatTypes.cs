using Parry.MonsterClass;
using Parry.PlayerClass;

namespace Parry.CombatClass
{
    public enum PlayerAttackType
    {
        Normal,
        ParryCounter
    }

    // 몬스터가 플레이어에게 전달하는 타격 정보다.
    public struct AttackHitContext
    {
        public float Damage { get; }
        public Monster Attacker { get; }
        public MonsterAttackType AttackType { get; }
        public bool CanKnockback { get; }
        public float KnockbackDistance { get; }
        public bool CausesFaint { get; }
        public int AttackDirection { get; }

        
        public AttackHitContext(MonsterAttackData attackData, Monster attacker, int attackDirection)
        {
            Damage = attackData.Damage;
            Attacker = attacker;
            AttackType = attackData.AttackType;
            CanKnockback = attackData.CanKnockback;
            KnockbackDistance = attackData.KnockbackDistance;
            CausesFaint = attackData.CausesFaint;
            AttackDirection = attackDirection;
        }
    }

    // 플레이어의 공격·반격에서 계산한 최종 피해와 치명타 판정을 전달한다. -> 몬스터에게
    public struct MonsterHitContext
    {
        public float Damage { get; }
        public bool IsCritical { get; }
        public PlayerAttackType AttackType { get; }

        public bool IsBerserking { get; set; }
        public PlayerBerseker Berseker { get; set; }
        public float StoredDamage { get; set; }
        public int SlashStackCount { get; set; }

        public MonsterHitContext(float damage, PlayerAttackType attackType, bool isCritical = false,
            bool isBerserking = false, float storedDamage = 0f, int slashStackCount = 0)
        {
            Damage = damage;
            IsCritical = isCritical;
            AttackType = attackType;
            IsBerserking = isBerserking;
            Berseker = null;
            StoredDamage = storedDamage;
            SlashStackCount = slashStackCount;
        }
    }
}
