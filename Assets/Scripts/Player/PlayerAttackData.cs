using UnityEngine;

namespace Parry.PlayerClass
{
    [CreateAssetMenu(fileName = "PlayerAttack", menuName = "Parry/Player Attack Data")]
    public sealed class PlayerAttackData : ScriptableObject
    {
        [SerializeField, Min(0f)] private float damage = 20f;
        [SerializeField] private Vector2 hitboxOffset = new Vector2(0.9f, 0f);
        [SerializeField] private Vector2 hitboxSize = new Vector2(1.2f, 1.1f);
        [SerializeField] private string animatorStateName;

        public float Damage => damage;
        public Vector2 HitboxOffset => hitboxOffset;
        public Vector2 HitboxSize => hitboxSize;
        public string AnimatorStateName => animatorStateName;
    }
}
