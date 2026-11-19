using UnityEngine;

namespace Parry.CombatClass
{
    public interface ICombatTarget
    {
        Transform transform { get; }
        bool CanTakeDamage { get; }
        void TakeDamage(ref MonsterHitContext context);
    }
}
