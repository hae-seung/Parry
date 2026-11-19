using UnityEngine;

namespace Parry.PlayerClass
{
    [RequireComponent(typeof(Rigidbody2D),
        typeof(CapsuleCollider2D), 
        typeof(SpriteRenderer))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        private PlayerMovementData data;

        private readonly RaycastHit2D[] groundHits = new RaycastHit2D[8];
        private Rigidbody2D body;
        private CapsuleCollider2D capsule;
        private SpriteRenderer sr;
        private PlayerCharacter character;
        private PlayerAttack attack;
        private ContactFilter2D groundFilter;
        private float horizontalInput;
        private bool jumpRequested;
        private float knockbackSpeed;
        private float knockbackStartTime;
        private float knockbackDuration;

        public bool IsGrounded { get; private set; }
        public int FacingDirection { get; private set; } = 1;
        public Vector2 Velocity => body.linearVelocity;
        internal bool HasPendingJump => jumpRequested;

        private float KnockbackVelocity
        {
            get
            {
                float progress = knockbackDuration > 0f ? (Time.time - knockbackStartTime) / knockbackDuration : 1f;
                return Mathf.Lerp(knockbackSpeed, 0f, progress);
            }
        }

        public void Initialize(PlayerMovementData data)
        {
            this.data = data;
            body = GetComponent<Rigidbody2D>();
            capsule = GetComponent<CapsuleCollider2D>();
            sr = GetComponent<SpriteRenderer>();
            character = GetComponent<PlayerCharacter>();
            attack = GetComponent<PlayerAttack>();
            groundFilter.SetLayerMask(data.groundLayers);
            groundFilter.useTriggers = false;
        }

        public void Teleport(Vector2 position)
        {
            horizontalInput = 0f;
            jumpRequested = false;
            knockbackSpeed = 0f;
            knockbackDuration = 0f;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.position = position;
            transform.position = new Vector3(position.x, position.y, transform.position.z);
            IsGrounded = false;
        }

        internal void Move(float horizontal) => horizontalInput = horizontal;
        internal void Jump() => jumpRequested = true;
        internal void CancelJump() => jumpRequested = false;

        private void FixedUpdate()
        {
            CheckPlayerIsOnGround();
            
            Vector2 velocity = body.linearVelocity;

            if (character.IsDead)
            {
                velocity.x = 0f;
                body.linearVelocity = velocity;
                return;
            }

            // 피격 중에는 입력 이동 대신 감속하는 밀림만 적용하고, 수직 물리는 유지한다.
            if (character.IsHitReacting)
            {
                velocity.x = KnockbackVelocity;
                body.linearVelocity = velocity;
                CancelJump();
                return;
            }

            if (character.IsRolling)
            {
                velocity.x = FacingDirection * data.rollSpeed;
                body.linearVelocity = velocity;
                return;
            }

            // 포효 중에는 이전부터 누르던 이동 입력도 적용하지 않는다.
            if (character.IsRoaring)
            {
                velocity.x = 0f;
                body.linearVelocity = velocity;
                CancelJump();
                return;
            }

            if (attack.IsAttacking)
            {
                velocity.x = 0f;
                body.linearVelocity = velocity;
                CancelJump();
                return;
            }

            MovePlayer(ref velocity);
            FaceMoveInput();
        }

        internal void FaceMoveInput()
        {
            if (Mathf.Abs(horizontalInput) > 0.01f)
            {
                FacingDirection = horizontalInput > 0f ? 1 : -1;
                sr.flipX = FacingDirection < 0;
            }
        }

        internal void StartKnockback(int direction, float distance, float duration)
        {
            knockbackStartTime = Time.time;
            knockbackDuration = duration;
            
            knockbackSpeed = duration > 0f ? direction * distance * 2f / duration : 0f;
        }

        internal void StopKnockback()
        {
            knockbackDuration = 0f;
            knockbackSpeed = 0f;
            
            body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
        }

        private void CheckPlayerIsOnGround()
        {
            IsGrounded = false;
            
            int hitCount = capsule.Cast(
                Vector2.down,         // 아래 방향으로
                groundFilter,         // 지정한 레이어만, Trigger 제외
                groundHits,           // 결과를 기존 배열에 저장
                data.groundCheckDistance   // 0.06 유닛만큼 검사
            );
            
            for (int i = 0; i < hitCount; i++)
            {
                //경사체크
                if (groundHits[i].normal.y > 0.65f && body.linearVelocity.y <= 0.1f)
                {
                    IsGrounded = true;
                    break;
                }
            }
        }

        private void MovePlayer(ref Vector2 velocity)
        {
            velocity.x = horizontalInput * data.moveSpeed + KnockbackVelocity;
            if (jumpRequested && IsGrounded)
            {
                velocity.y = data.jumpSpeed;
                IsGrounded = false;
            }
            jumpRequested = false;
            body.linearVelocity = velocity;
        }
    }
}
