using UnityEngine;

namespace Parry.PlayerClass
{
    [CreateAssetMenu(fileName = "BerserkerAttack", menuName = "Parry/Berserker Attack Data")]
    public sealed class BerserkerAttackData : PlayerAttackData
    {
        [Tooltip("이 공격이 적중했을 때 대상에게 저장할 참격 중첩 수입니다.")]
        [SerializeField, Min(1)] private int slashStackCount = 1;

        public int SlashStackCount => slashStackCount;
    }
}
