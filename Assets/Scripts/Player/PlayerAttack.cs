using System.Collections.Generic;
using Parry.MonsterClass;
using UnityEngine;
using UnityEngine.Serialization;

namespace Parry.PlayerClass
{
    [RequireComponent(typeof(PlayerCharacter))]
    public sealed class PlayerAttack : MonoBehaviour
    {
        private PlayerCharacter character;
        private Animator animator;
        private AnimatorOverrideController overrideController;

        [SerializeField] private AnimationClip attackClip;
        [SerializeField] private List<PlayerAttackData> attacks = new List<PlayerAttackData>();
        [SerializeField, Min(0f)] private float comboResetDuration = 1f;
        [SerializeField] private LayerMask targetLayers;

        private readonly HashSet<Monster> hitTargetSet = new HashSet<Monster>();
        private readonly List<Collider2D> hitResults = new List<Collider2D>(16);
        private ContactFilter2D hitFilter;

        private PlayerAttackData currentAttack;
        private int attackIndex;
        private int attackDirection = 1;
        private float lastAttackTime = float.NegativeInfinity;
        private bool bIsAttacking;
        private bool bCanReserveNextAttack;
        private bool bIsReservedNextAttack;
        private bool hitEventPending;

        public bool IsAttacking => bIsAttacking;

        private bool CanContinueCombo => Time.time - lastAttackTime < comboResetDuration;

        private void Awake()
        {
            character = GetComponent<PlayerCharacter>();
            animator = GetComponent<Animator>();
            if (animator != null && animator.runtimeAnimatorController != null && attackClip != null)
            {
                overrideController = new AnimatorOverrideController(animator.runtimeAnimatorController);
                animator.runtimeAnimatorController = overrideController;
            }

            hitFilter = new ContactFilter2D();
            hitFilter.SetLayerMask(targetLayers);
            hitFilter.useTriggers = true;
        }

        private void OnDisable() => CancelAttack();

        private void OnDestroy()
        {
            if (overrideController != null)
                Destroy(overrideController);
        }

        private bool CanAttack() =>
            !character.IsDead && character.IsGrounded &&
            !character.IsRolling && !character.IsParrying && overrideController != null &&
            animator.isActiveAndEnabled && attacks.Count > 0;

        public bool TryStartAttack()
        {
            //이미 예약이 되었으면 자동으로 실행될거임.
            if (!CanAttack() || bIsReservedNextAttack)
                return false;

            if (!IsAttacking)
                return StartAttack(CanContinueCombo ? attackIndex : 0);
            
            //3타 -> 1타로는 예약 불가
            //1타->2타->3타가 한묶음 
            if (!bCanReserveNextAttack || attackIndex >= attacks.Count)
                return false;

            //예약 걸기
            bIsReservedNextAttack = true;
            
            return true;
        }

        //다음 공격 예약
        public void OpenComboBuffer()
        {
            if (IsAttacking)
                bCanReserveNextAttack = true;
        }

        private bool StartAttack(int index)
        {
            index %= attacks.Count;
            
            PlayerAttackData nextAttack = attacks[index];
            if (nextAttack == null || nextAttack.AnimationClip == null)
                return false;
            
            attackIndex = index;
            currentAttack = nextAttack;
            attackDirection = character.FacingDirection;
            hitEventPending = true;

            //클립교체
            if (overrideController[attackClip] != nextAttack.AnimationClip)
                overrideController[attackClip] = nextAttack.AnimationClip;

            //공격상태로 애니메이션 전환될거임
            bIsAttacking = true;

            // 콤보를 이어갈 때는 공격 상태 안에서 처음부터 재생한다.
            if (animator.GetCurrentAnimatorStateInfo(0).IsTag("Attack"))
            {
                //attack_StateMachine의 재생시간을 설정
                animator.Play(0, 0, 0f);
                
                //0초 시간대의 모션으로 즉시 상태 업데이트
                animator.Update(0f);
            }

            return true;
        }

        // 실제 공격판정 이벤트가 들어올때
        public void ApplyAttackHit()
        {
            if (!isActiveAndEnabled || character.IsDead || !hitEventPending || currentAttack == null)
                return;

            hitEventPending = false;
            
            float damage = currentAttack.Damage;
            if (damage <= 0f)
            {
                attackIndex = 0;
                return;
            }

            Physics2D.OverlapBox(GetHitboxCenter(), currentAttack.HitboxSize, 0f, hitFilter, hitResults);
            hitTargetSet.Clear();

            foreach (Collider2D hit in hitResults)
            {
                Monster target = hit.GetComponentInParent<Monster>();
                if (target == null || !target.isActiveAndEnabled || target.IsDead || !hitTargetSet.Add(target))
                    continue;

                target.TakeDamage(damage);
            }

            //실제 몬스터를 때린경우에만 콤보 이어지도록
            attackIndex = hitTargetSet.Count > 0 ? attackIndex + 1 : 0;
        }

        // 공격 클립 끝의 Animation Event에서 호출한다.
        public void EndAttackMotion()
        {
            if (!bIsAttacking)
                return;

            bIsAttacking = false;
            lastAttackTime = Time.time;
            hitEventPending = false;
            
            //공격모션이 끝나면 예약 막기
            bCanReserveNextAttack = false;
            
            //공격이 끝나기전에 이미 예약되어있었다면 바로 다음 공격 실행
            //EndAttackMotion이 종료되기 전까지는 단일스레드라서
            //PlayerAnimation에서의 bIsAttacking = true인 상태 => Idle로 상태 전환이 안됨 아직
            if (bIsReservedNextAttack && CanAttack())
            {
                bIsReservedNextAttack = false;
                
                //상태전환 전에 바로 다시 공격모션을 실행
                //=> AttackMotion -> Idle -> AttackMotion 되는 상황 방지
                //바로 AttackMotion -> AttackMotion으로
                StartAttack(attackIndex);
            }
        }

        public void CancelAttack()
        {
            bIsAttacking = false;
            bCanReserveNextAttack = false;
            bIsReservedNextAttack = false;
            hitEventPending = false;
            attackIndex = 0;
            currentAttack = null;
            lastAttackTime = float.NegativeInfinity;
        }

        private Vector2 GetHitboxCenter()
        {
            Vector2 offset = currentAttack.HitboxOffset;
            return (Vector2)transform.position + new Vector2(offset.x * attackDirection, offset.y);
        }

        private void OnDrawGizmos()
        {
            if (!IsAttacking || currentAttack == null)
                return;

            Color previousColor = Gizmos.color;
            Gizmos.color = hitEventPending ? Color.yellow : Color.red;
            Gizmos.DrawWireCube(GetHitboxCenter(), currentAttack.HitboxSize);
            Gizmos.color = previousColor;
        }
    }
}
