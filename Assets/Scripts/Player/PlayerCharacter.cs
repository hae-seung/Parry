using UnityEngine;

namespace Parry.PlayerClass
{
    [RequireComponent(typeof(PlayerMovement), typeof(PlayerStatComponent))]
    public sealed class PlayerCharacter : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float rollCooldown = 0.3f;

        private float lastRollTime;
        private PlayerMovement movement;
        private PlayerAttack attack;
        public PlayerStatComponent Stats { get; private set; }
        public bool IsDead => Stats.IsDead;

        public bool IsGrounded => movement.IsGrounded;
        public int FacingDirection => movement.FacingDirection;
        public Vector2 Velocity => movement.Velocity;
        public bool IsRolling { get; private set; }
        public bool IsParrying { get; private set; }
        public bool IsAttacking => attack.IsAttacking;

        private void Awake()
        {
            movement = GetComponent<PlayerMovement>();
            Stats = GetComponent<PlayerStatComponent>();
            attack = GetComponent<PlayerAttack>();
        }

        public void Move(float horizontal)
        {
            movement.Move(horizontal);
        }

        public void Jump()
        {
            if (!IsDead && !IsRolling && !IsParrying && (attack == null || !attack.IsAttacking))
                movement.Jump();
        }

        public void Attack() => attack?.TryStartAttack();

        public void Parry()
        {
            if (IsDead || IsRolling || IsParrying)
                return;

            if (attack != null && attack.IsAttacking)
                attack.CancelAttack();

            IsParrying = true;
            movement.CancelJump();
        }

        public void Roll()
        {
            if (IsDead || !IsGrounded || IsRolling || IsParrying || Time.time < lastRollTime + rollCooldown)
                return;

            if (attack != null && attack.IsAttacking)
                attack.CancelAttack();

            movement.FaceMoveInput();
            IsRolling = true;
            movement.CancelJump();
        }

        internal void EndParry() => IsParrying = false;

        internal void EndRoll()
        {
            IsRolling = false;
            lastRollTime = Time.time;
        }
    }
}
