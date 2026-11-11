using UnityEngine;

namespace Parry.MonsterClass
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    [RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
    public sealed class MonsterMovement : MonoBehaviour
    {
        private Rigidbody2D body;
        private SpriteRenderer spriteRenderer;
        private Animator animator;
        private Collider2D bodyCollider;

        private float moveSpeed;
        private float destinationX;
        private bool hasDestination;
        private ContactFilter2D groundFilter;
        private readonly Collider2D[] obstacleHits = new Collider2D[1];
        private readonly RaycastHit2D[] groundHits = new RaycastHit2D[1];
        private static readonly int SpeedId = Animator.StringToHash("Speed");

        public float FeetY => bodyCollider.bounds.min.y;
        public Vector2 Position => body.position;
        public int FacingDirection { get; private set; } = 1;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<Collider2D>();
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            animator = GetComponent<Animator>();
        }

        public void Initialize(float speed, LayerMask groundLayers)
        {
            moveSpeed = speed;
            groundFilter = new ContactFilter2D();
            groundFilter.SetLayerMask(groundLayers);
            groundFilter.useTriggers = false;
        }
        
        private void FixedUpdate()
        {
            float distance = destinationX - body.position.x;
            
            //가까워지면 속도 점점 느리게
            float speed = hasDestination
                ? Mathf.Clamp(distance / Time.fixedDeltaTime, -moveSpeed, moveSpeed)
                : 0f;
            
            body.linearVelocity = new Vector2(speed, body.linearVelocity.y);
            
            if (Mathf.Abs(speed) > 0.01f)
            {
                FacingDirection = speed > 0f ? 1 : -1;
                spriteRenderer.flipX = FacingDirection < 0;
            }
        }

        private void Update()
        {
            animator.SetFloat(SpeedId, Mathf.Abs(body.linearVelocity.x));
        }

        private void OnDisable() => Stop();
        

        // 배회 중 목표 방향으로 한 걸음 더 걸을 수 있는지 확인한다.
        public bool CanWalkTowards(float targetX)
        {
            float direction = Mathf.Sign(targetX - body.position.x);
            
            // 몸을 감싸는 경계의 앞쪽 끝에 폭 0.15인 검사 박스를 붙인다.
            // 박스 중심은 폭의 절반(0.075)만큼 앞에 두고, 바닥에 걸리지 않도록 높이를 조금 줄인다.
            Bounds bounds = bodyCollider.bounds;
            Vector2 front = new Vector2(bounds.center.x + direction * (bounds.extents.x + 0.075f), bounds.center.y);
            Vector2 frontSize = new Vector2(0.15f, Mathf.Max(0.01f, bounds.size.y - 0.04f));
            
            // 몸 앞에 벽이나 턱이 있으면 그 방향으로 걸을 수 없다.
            if (Physics2D.OverlapBox(front, frontSize, 0f, groundFilter, obstacleHits) > 0)
                return false;

            // 앞이 비어 있으면 발끝보다 0.1 앞, 발바닥보다 0.1 위에서 아래로 쏜다.
            // 0.3 안에 지면이 잡히면 걸을 수 있고, 잡히지 않으면 낭떠러지로 판단한다.
            Vector2 edge = new Vector2(bounds.center.x + direction * (bounds.extents.x + 0.1f), bounds.min.y + 0.1f);
            
            return Physics2D.Raycast(edge, Vector2.down, groundFilter, groundHits, 0.3f) > 0;
        }

        public void Face(float targetX)
        {
            float direction = targetX - body.position.x;
            
            //겹쳐있을땐 와리가리 방지
            if (Mathf.Abs(direction) <= 0.01f)
                return;
            
            FacingDirection = direction > 0f ? 1 : -1;
            
            spriteRenderer.flipX = FacingDirection < 0;
        }

        public void MoveTo(float x)
        {
            destinationX = x;
            hasDestination = true;
        }

        public void Stop()
        {
            hasDestination = false;
            if (body != null)
                body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
        }
    }
}
