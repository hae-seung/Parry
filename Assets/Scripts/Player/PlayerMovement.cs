using UnityEngine;

namespace Parry.PlayerClass
{
    [RequireComponent(typeof(Rigidbody2D),
        typeof(CapsuleCollider2D), 
        typeof(SpriteRenderer))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float moveSpeed = 6f;
        [SerializeField, Min(0f)] private float jumpSpeed = 12f;
        [SerializeField, Min(0f)] private float groundCheckDistance = 0.06f;
        [SerializeField] private LayerMask groundLayers = 1;
        [SerializeField, Min(0f)] private float rollSpeed = 9f;

        private readonly RaycastHit2D[] groundHits = new RaycastHit2D[8];
        private Rigidbody2D body;
        private CapsuleCollider2D capsule;
        private SpriteRenderer sr;
        private PlayerCharacter character;
        private PlayerAttack attack;
        private ContactFilter2D groundFilter;
        private float horizontalInput;
        private bool jumpRequested;

        public bool IsGrounded { get; private set; }
        public int FacingDirection { get; private set; } = 1;
        public Vector2 Velocity => body.linearVelocity;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            capsule = GetComponent<CapsuleCollider2D>();
            sr = GetComponent<SpriteRenderer>();
            character = GetComponent<PlayerCharacter>();
            attack = GetComponent<PlayerAttack>();
            groundFilter.SetLayerMask(groundLayers);
            groundFilter.useTriggers = false;
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
            
            if (character.IsRolling)
            {
                velocity.x = FacingDirection * rollSpeed;
                body.linearVelocity = velocity;
                return;
            }

            if (attack != null && attack.IsAttacking)
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

        private void CheckPlayerIsOnGround()
        {
            IsGrounded = false;
            
            int hitCount = capsule.Cast(
                Vector2.down,         // 아래 방향으로
                groundFilter,         // 지정한 레이어만, Trigger 제외
                groundHits,           // 결과를 기존 배열에 저장
                groundCheckDistance   // 0.06 유닛만큼 검사
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
            velocity.x = horizontalInput * moveSpeed;
            if (jumpRequested && IsGrounded)
            {
                velocity.y = jumpSpeed;
                IsGrounded = false;
            }
            jumpRequested = false;
            body.linearVelocity = velocity;
        }
    }
}
