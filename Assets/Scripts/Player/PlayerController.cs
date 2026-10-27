using System;
using UnityEngine;
using UnityEngine.InputSystem;


namespace Parry.PlayerClass
{
    [RequireComponent(
        typeof(Rigidbody2D), 
        typeof(CapsuleCollider2D),
        typeof(PlayerInput))]
    public sealed class PlayerController : MonoBehaviour
    {
        [Header("Movement — prototype values")]
        [SerializeField, Min(0f)] private float moveSpeed = 6f;
        [SerializeField, Min(0f)] private float jumpSpeed = 12f;
        [SerializeField, Min(0f)] private float groundCheckDistance = 0.06f;
        [SerializeField] private LayerMask groundLayers = 1;

        private SpriteRenderer sr;
        private readonly RaycastHit2D[] groundHits = new RaycastHit2D[8];
        private Rigidbody2D body;
        private CapsuleCollider2D capsule;
        private ContactFilter2D groundFilter;
        private float horizontalInput;
        private bool jumpRequested;
        private PlayerAttack attack;

        public bool IsGrounded { get; private set; }
        public int FacingDirection { get; private set; } = 1;
        
        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            
            body = GetComponent<Rigidbody2D>();
            capsule = GetComponent<CapsuleCollider2D>();
            attack = GetComponent<PlayerAttack>();
            
            groundFilter = new ContactFilter2D();
            groundFilter.SetLayerMask(groundLayers);
            groundFilter.useTriggers = false;
        }
        
        private void OnEnable()
        {
            
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            if (context.performed || context.canceled)
            {
                horizontalInput = context.ReadValue<Vector2>().x;
            }
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                jumpRequested = true;
            }
        }

        private void FixedUpdate()
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
            
            Vector2 velocity = body.linearVelocity;
            velocity.x = horizontalInput * moveSpeed;
            if (jumpRequested && IsGrounded)
            {
                velocity.y = jumpSpeed;
                IsGrounded = false;
            }
            jumpRequested = false;
            body.linearVelocity = velocity;

            //캐릭터 뒤집기
            if (Mathf.Abs(horizontalInput) > 0.01f && (attack == null || !attack.IsAttacking))
            {
                FacingDirection = horizontalInput > 0f ? 1 : -1;
                sr.flipX = FacingDirection < 0;
            }
        }
    }
}
