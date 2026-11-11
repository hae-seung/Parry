using Parry.CombatClass;
using Parry.MonsterClass;
using UnityEngine;

namespace Parry.PlayerClass
{
    [RequireComponent(typeof(PlayerMovement), typeof(PlayerStatComponent))]
    [RequireComponent(typeof(PlayerParry), typeof(PlayerHitReaction))]
    public sealed class PlayerCharacter : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float rollCooldown = 0.3f;

        private float lastRollTime;
        private PlayerMovement movement;
        private PlayerAttack attack;
        private PlayerParry parry;
        private PlayerHitReaction hitReaction;
        public PlayerStatComponent Stats { get; private set; }
        public bool IsDead => Stats.IsDead;

        public bool IsGrounded => movement.IsGrounded;
        public int FacingDirection => movement.FacingDirection;
        public Vector2 Velocity => movement.Velocity;
        public bool IsRolling { get; private set; }
        public bool IsParrying => parry.IsParrying;
        public bool IsAttacking => attack.IsAttacking;
        public bool IsHitReacting => hitReaction.IsReacting;
        public bool IsKnockedDown => hitReaction.IsKnockedDown;

        private void Awake()
        {
            movement = GetComponent<PlayerMovement>();
            Stats = GetComponent<PlayerStatComponent>();
            attack = GetComponent<PlayerAttack>();
            parry = GetComponent<PlayerParry>();
            hitReaction = GetComponent<PlayerHitReaction>();
        }

        public void Move(float horizontal)
        {
            movement.Move(horizontal);
        }

        public void Jump()
        {
            if (!IsDead && !IsHitReacting && !IsRolling && !IsParrying && (attack == null || !attack.IsAttacking))
                movement.Jump();
        }

        public void Attack() => attack?.TryStartAttack();

        public void Parry() => parry.TryStartParry();

        // 적의 공격을 받으면 플레이어가 방어 여부를 판단한 뒤 체력에 반영한다.
        // attacker는 패링 성공 시 반격 피해나 성공 알림을 보낼 공격자다.
        public void TakeDamage(float damage, Monster attacker = null)
        {
            TakeDamage(new AttackHitContext(damage, attacker));
        }

        public void TakeDamage(AttackHitContext context)
        {
            bool validHit = isActiveAndEnabled && !IsDead && context.Damage > 0f && !float.IsInfinity(context.Damage);
            if (!validHit)
                return;

            if (parry.TryParryHit())
                return;

            if (hitReaction.IsInvulnerable)
                return;

            Stats.TakeDamage(context.Damage);
            bool interruptsAction = context.CausesStagger || context.KnocksDown || IsDead;
            if (interruptsAction)
            {
                attack.CancelAttack();
                parry.CancelParry();
                if (IsRolling)
                    EndRoll();
            }

            if (IsDead)
            {
                hitReaction.CancelReaction();
                return;
            }

            hitReaction.StartReaction(context);
        }

        public void Roll()
        {
            if (IsDead || IsHitReacting || !IsGrounded || IsRolling || IsParrying || 
                Time.time < lastRollTime + rollCooldown)
                return;

            if (attack != null && attack.IsAttacking)
                attack.CancelAttack();

            movement.FaceMoveInput();
            IsRolling = true;
            movement.CancelJump();
        }

        internal void EndRoll()
        {
            IsRolling = false;
            lastRollTime = Time.time;
        }
    }
}
