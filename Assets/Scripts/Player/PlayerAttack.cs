using System.Collections.Generic;
using Parry.MonsterClass;
using UnityEngine;

namespace Parry.PlayerClass
{
    [RequireComponent(typeof(PlayerCharacter))]
    public sealed class PlayerAttack : MonoBehaviour
    {
        [SerializeField] private List<PlayerAttackData> attacks = new List<PlayerAttackData>();
        [SerializeField, Min(0f)] private float comboResetDuration = 1f;
        [SerializeField] private LayerMask targetLayers;

        private readonly HashSet<Monster> hitTargets = new HashSet<Monster>();
        private Collider2D[] hitResults = new Collider2D[16];
        private PlayerCharacter character;
        private Animator animator;
        private ContactFilter2D hitFilter;
        private PlayerAttackData currentAttack;
        private int attackIndex;
        private int attackDirection = 1;
        private double lastAttackTime = double.NegativeInfinity;
        private bool hasHit;
        private bool hitEventPending;

        // Input and facing are locked until the single hit event, with no end event required.
        public bool IsAttacking => hitEventPending;
        public bool IsAttackPlaying
        {
            get
            {
                if (animator == null || currentAttack == null)
                    return hitEventPending;

                AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
                if (state.IsName(currentAttack.AnimatorStateName) && Time.timeAsDouble != lastAttackTime)
                    return state.normalizedTime < 1f;

                return hitEventPending && Time.timeAsDouble - lastAttackTime < comboResetDuration;
            }
        }
        public int ComboStep => currentAttack != null ? attackIndex + 1 : 0;
        public int AttackIndex => attackIndex;

        private void Awake()
        {
            character = GetComponent<PlayerCharacter>();
            animator = GetComponent<Animator>();
            hitFilter = new ContactFilter2D();
            hitFilter.SetLayerMask(targetLayers);
            hitFilter.useTriggers = true;
        }

        private void OnDisable() => CancelAttack();

        public bool TryStartAttack()
        {
            if (!isActiveAndEnabled || character.IsDead || !character.IsGrounded || character.IsRolling || attacks.Count == 0)
                return false;

            double now = Time.timeAsDouble;
            bool expired = now - lastAttackTime >= comboResetDuration;
            if (hitEventPending && !expired)
                return false;

            int nextIndex = expired || !hasHit ? 0 : (attackIndex + 1) % attacks.Count;
            PlayerAttackData nextAttack = attacks[nextIndex];
            if (nextAttack == null)
                return false;

            attackIndex = nextIndex;
            currentAttack = nextAttack;
            lastAttackTime = now;
            attackDirection = character.FacingDirection;
            hasHit = false;
            hitEventPending = true;
            hitTargets.Clear();

            if (animator != null && animator.runtimeAnimatorController != null &&
                !string.IsNullOrEmpty(currentAttack.AnimatorStateName))
                animator.Play(currentAttack.AnimatorStateName, 0, 0f);

            return true;
        }

        // Animation Event: query once, even if the same event is accidentally repeated.
        public void ApplyAttackHit()
        {
            if (!isActiveAndEnabled || character.IsDead || !hitEventPending || currentAttack == null)
                return;

            hitEventPending = false;
            int count;
            while (true)
            {
                count = Physics2D.OverlapBox(GetHitboxCenter(), currentAttack.HitboxSize,
                    0f, hitFilter, hitResults);
                if (count < hitResults.Length)
                    break;
                System.Array.Resize(ref hitResults, hitResults.Length * 2);
            }

            for (int i = 0; i < count; i++)
            {
                Monster target = hitResults[i].GetComponentInParent<Monster>();
                if (target == null || !target.isActiveAndEnabled || target.IsDead ||
                    currentAttack.Damage <= 0f || !hitTargets.Add(target))
                    continue;

                hasHit = true;
                target.TakeDamage(currentAttack.Damage);
            }
        }

        public void CancelAttack()
        {
            hitEventPending = false;
            hasHit = false;
            attackIndex = 0;
            currentAttack = null;
            lastAttackTime = double.NegativeInfinity;
            hitTargets.Clear();
        }

        private Vector2 GetHitboxCenter()
        {
            Vector2 offset = currentAttack.HitboxOffset;
            return (Vector2)transform.position + new Vector2(offset.x * attackDirection, offset.y);
        }

        private void OnDrawGizmosSelected()
        {
            if (currentAttack == null)
                return;
            Gizmos.color = hitEventPending ? Color.yellow : Color.red;
            Gizmos.DrawWireCube(GetHitboxCenter(), currentAttack.HitboxSize);
        }
    }
}
