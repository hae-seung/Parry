using Parry.PlayerClass;
using UnityEngine;

namespace Parry.MonsterClass
{
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(MonsterMovement), typeof(Collider2D))]
    public sealed class MonsterSensor : MonoBehaviour
    {
        private MonsterMovement movement;
        private Collider2D bodyCollider;
        private Collider2D targetCollider;

        private PlayerCharacter target;
        private MonsterDetectionSettings settings;
        private bool initialized;
        private readonly RaycastHit2D[] sightHits = new RaycastHit2D[32];
        private ContactFilter2D sightFilter;

        public PlayerCharacter Target => target;
        public bool HasTarget => target != null && target.isActiveAndEnabled && !target.IsDead;
        public bool IsTargetVisible { get; private set; }
        public float LastSeenTime { get; private set; } = float.NegativeInfinity;
        public Vector2 LastSeenPosition { get; private set; }

        private void Awake()
        {
            movement = GetComponent<MonsterMovement>();
            bodyCollider = GetComponent<Collider2D>();

            if (target == null)
            {
                target = FindAnyObjectByType<PlayerCharacter>();
            }
            
            targetCollider = target.GetComponent<Collider2D>();
        }

        private void OnEnable()
        {
            IsTargetVisible = false;
            LastSeenTime = float.NegativeInfinity;
            LastSeenPosition = default;
        }
        
        public void Initialize(MonsterDetectionSettings detectionSettings)
        {
            settings = detectionSettings;
            
            sightFilter = new ContactFilter2D();
            sightFilter.SetLayerMask(settings.obstacleLayers);
            sightFilter.useTriggers = false;
            initialized = true;
        }

        private void Update()
        {
            if (!initialized)
                return;

            IsTargetVisible = CanSeeTarget();
            if (IsTargetVisible)
            {
                LastSeenTime = Time.time;
                LastSeenPosition = target.transform.position;
            }
        }

        private void OnDisable() => IsTargetVisible = false;
        

        private bool CanSeeTarget()
        {
            if (!HasTarget || targetCollider == null)
                return false;

            if (!IsWithinDetectionDistance())
                return false;

            if (!IsAtDetectableHeight())
                return false;

            return HasNoObstacleToTarget();
        }

        private bool IsWithinDetectionDistance()
        {
            Vector2 delta = (Vector2)target.transform.position - movement.Position;
            
            return delta.sqrMagnitude <= settings.distance * settings.distance;
        }

        private bool IsAtDetectableHeight()
        {
            float heightDifference = Mathf.Abs(targetCollider.bounds.min.y - movement.FeetY);
            
            return heightDifference <= settings.heightTolerance;
        }

        private bool HasNoObstacleToTarget()
        {
            Vector2 origin = bodyCollider.bounds.center;
            Vector2 direction = (Vector2)targetCollider.bounds.center - origin;
            
            int count = Physics2D.Raycast(origin, direction.normalized, sightFilter, sightHits, direction.magnitude);
            for (int i = 0; i < count; i++)
            {
                var hit = sightHits[i].collider;
                if (hit.CompareTag("Monster") || hit.CompareTag("Player"))
                    continue;
                
                return false;
            }
            
            // A full buffer may hide an occluder; don't claim visibility in that case.
            return count < sightHits.Length;
        }
    }
}
