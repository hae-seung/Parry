using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Parry.MonsterClass;

namespace Parry.PlayerClass
{
    [RequireComponent(typeof(PlayerController))]
    public sealed class PlayerAttack : MonoBehaviour
    {
        public enum AttackPhase { Idle, Windup, Active, Recovery }

        [System.Serializable]
        private sealed class ComboAttack
        {
            [Min(0f)] public float damage = 20f;
            [Min(0.01f)] public float windupDuration = 0.12f;
            [Min(0.01f)] public float activeDuration = 0.1f;
            [Min(0.01f)] public float recoveryDuration = 0.25f;
            public Vector2 hitboxOffset = new Vector2(0.9f, 0f);
            public Vector2 hitboxSize = new Vector2(1.2f, 1.1f);

            public float GetDamage() => damage;
        }

        [Header("First attack — prototype values")]
        [SerializeField] private ComboAttack firstAttack = new ComboAttack();
        [Header("Second attack")]
        [SerializeField] private ComboAttack secondAttack = new ComboAttack();
        [Header("Third attack")]
        [SerializeField] private ComboAttack thirdAttack = new ComboAttack
        {
            damage = 35f,
            windupDuration = 0.18f,
            activeDuration = 0.12f,
            recoveryDuration = 0.4f
        };
        
        [Header("Hit detection")]
        [SerializeField] private LayerMask targetLayers;
        [SerializeField] private SpriteRenderer attackVisual;

        private readonly HashSet<Monster> hitTargets = new HashSet<Monster>();
        private Collider2D[] hitResults = new Collider2D[16];
        private PlayerController controller;
        private ContactFilter2D hitFilter;
        private float elapsed;
        private int attackDirection = 1;
        private int comboIndex;
        private bool nextAttackQueued;
        private ComboAttack currentAttack;
        
        

        public AttackPhase Phase { get; private set; }
        public bool IsAttacking => Phase != AttackPhase.Idle;
        public int ComboStep => IsAttacking ? comboIndex + 1 : 0;
        public bool HasQueuedAttack => nextAttackQueued;

        private void Awake()
        {
            controller = GetComponent<PlayerController>();
            currentAttack = firstAttack;
            
            hitFilter = new ContactFilter2D();
            hitFilter.SetLayerMask(targetLayers);
            hitFilter.useTriggers = true;
            
            SetVisual(false);
        }

        private void OnDisable() => CancelAttack();
        
        private void FixedUpdate()
        {
            if (!IsAttacking)
                return;

            float previousElapsed = elapsed;
            elapsed += Time.fixedDeltaTime;
            
            float activeEnd = currentAttack.windupDuration + currentAttack.activeDuration;

            //물리 업데이트라 이전 경과시간이 공격을 실제로 한 시간을 비교함. 현재랑 비교하면 놓칠 수 있음
            if (elapsed >= currentAttack.windupDuration && previousElapsed < activeEnd)
                ApplyHits();

            if (elapsed >= activeEnd + currentAttack.recoveryDuration)
            {
                if (nextAttackQueued && comboIndex < 2 && controller.IsGrounded)
                    StartStrike(comboIndex + 1);
                else
                    CancelAttack();
            }
            else if (elapsed >= activeEnd)
                Phase = AttackPhase.Recovery;
            else if (elapsed >= currentAttack.windupDuration)
                Phase = AttackPhase.Active;
        }
        
        //공격영역 표시용
        private void LateUpdate()
        {
            if (attackVisual == null) 
                return;
            
            attackVisual.transform.position = GetHitboxCenter();
            var hitboxSize = currentAttack.hitboxSize;
            attackVisual.transform.localScale = new Vector3(hitboxSize.x, hitboxSize.y, 1f);
            SetVisual(Phase == AttackPhase.Active);
        }

        public void OnAttack(InputAction.CallbackContext context)
        {
            if (context.performed)
                TryStartAttack();
        }

        public bool TryStartAttack()
        {
            if (!isActiveAndEnabled || !controller.IsGrounded)
                return false;

            if (IsAttacking)
            {
                if (comboIndex >= 2 || nextAttackQueued)
                    return false;
                // Store one click for the next strike, never the entire combo at once.
                nextAttackQueued = true;
                return true;
            }

            StartStrike(0);
            return true;
        }

        private void StartStrike(int index)
        {
            comboIndex = index;
            currentAttack = index switch
            {
                0 => firstAttack,
                1 => secondAttack,
                2 => thirdAttack,
                _ => throw new System.ArgumentOutOfRangeException(nameof(index))
            };
            nextAttackQueued = false;
            attackDirection = controller.FacingDirection;
            elapsed = 0f;
            hitTargets.Clear();
            Phase = AttackPhase.Windup;
            
            Debug.Log($"{comboIndex + 1} 번 공격");
        }
        
        
        private void ApplyHits()
        {
            Vector2 center = GetHitboxCenter();
            int count = 0;
            
            while (true)
            {
                count = Physics2D.OverlapBox(
                    center,
                    currentAttack.hitboxSize, 
                    0f,
                    hitFilter, 
                    hitResults);
                
                if (count < hitResults.Length)
                    break;
                
                System.Array.Resize(ref hitResults, hitResults.Length * 2);
            }

            for (int i = 0; i < count; i++)
            {
                var target = hitResults[i].GetComponentInParent<Monster>();
                if (target != null && !target.IsDead && hitTargets.Add(target))
                    target.TakeDamage(currentAttack.GetDamage());
            }
        }

        // Future defensive actions call this only after their execution conditions pass.
        public void CancelAttack()
        {
            Phase = AttackPhase.Idle;
            elapsed = 0f;
            comboIndex = 0;
            currentAttack = firstAttack;
            nextAttackQueued = false;
            hitTargets.Clear();
            SetVisual(false);
        }

        private Vector2 GetHitboxCenter()
        {
            int direction = IsAttacking ? attackDirection : (controller != null ? controller.FacingDirection : 1);
            Vector2 hitboxOffset = (currentAttack ?? firstAttack).hitboxSize;
            return (Vector2)transform.position + new Vector2(hitboxOffset.x * direction, hitboxOffset.y);
        }

        private void SetVisual(bool visible)
        {
            if (attackVisual != null) 
                attackVisual.enabled = visible;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Phase == AttackPhase.Active ? Color.red : Color.yellow;
            Gizmos.DrawWireCube(GetHitboxCenter(), (currentAttack ?? firstAttack).hitboxSize);
        }
    }
}
