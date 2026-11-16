using System.Collections.Generic;
using Parry.CombatClass;
using Parry.AnimationClass;
using Parry.PlayerClass;
using UnityEngine;

namespace Parry.MonsterClass
{
    [RequireComponent(typeof(AnimationClipPlayer), typeof(MonsterMovement), typeof(Monster))]
    public sealed class MonsterAttack : MonoBehaviour
    {
        private AnimationClipPlayer clipPlayer;
        private MonsterMovement movement;
        private Monster monster;

        [Tooltip("공격 판정에서 플레이어를 검사할 레이어입니다.")]
        [SerializeField] private LayerMask targetLayers;

        private List<MonsterAttackData> attacks;
        private List<float> attackReadyTimes;
        private readonly List<int> availableIndices = new List<int>();
        private readonly List<Collider2D> hitResults = new List<Collider2D>(16);
        private readonly HashSet<PlayerCharacter> hitTargets = new HashSet<PlayerCharacter>();
        private ContactFilter2D hitFilter;
        private int attackDirection;
        private bool hitEventPending;

        public MonsterAttackData CurrentAttack { get; private set; }
        public bool IsMotionPlaying => clipPlayer.IsPlaying;

        private void Awake()
        {
            clipPlayer = GetComponent<AnimationClipPlayer>();
            movement = GetComponent<MonsterMovement>();
            monster = GetComponent<Monster>();
            
            hitFilter = new ContactFilter2D();
            hitFilter.SetLayerMask(targetLayers);
            hitFilter.useTriggers = true;
        }

        private void OnDisable() => Stop();

        public void Initialize(List<MonsterAttackData> attackData)
        {
            attacks = new List<MonsterAttackData>(attackData);
            attackReadyTimes = new List<float>(attacks.Count);
            availableIndices.Clear();
            availableIndices.Capacity = attacks.Count;
            
            for (int i = 0; i < attacks.Count; i++)
            {
                attackReadyTimes.Add(0f);
            }
        }

        public bool TryStartAttack(float distanceSquared)
        {
            if (monster.IsExecuting || monster.IsDead || monster.IsFlinching)
                return false;

            // 거리 조건을 만족하고 쿨타임이 끝난 공격의 인덱스를 모은다.
            availableIndices.Clear();
            
            for (int i = 0; i < attacks.Count; i++)
            {
                bool ready = Time.time >= attackReadyTimes[i];
                
                if (!ready || !CanUse(attacks[i], distanceSquared))
                    continue;

                availableIndices.Add(i);
            }

            // 사용할 수 있는 공격이 없으면 추적 상태에서 다음 기회를 기다린다.
            if (availableIndices.Count == 0)
                return false;

            // 목록에서 무작위 위치를 고르고, 저장된 실제 공격 인덱스를 가져온다.
            int randomIndex = Random.Range(0, availableIndices.Count);
            int selectedIndex = availableIndices[randomIndex];
            
            MonsterAttackData selectedAttack = attacks[selectedIndex];
            
            //공격 애니메이션 실행
            if (!clipPlayer.Play(selectedAttack.AnimationClip))
                return false;

            CurrentAttack = selectedAttack;
            attackDirection = movement.FacingDirection;
            hitEventPending = true;
            
            // 모든 공격은 시작 순간부터 쿨타임을 계산한다. 0이면 즉시 재사용 가능하다.
            attackReadyTimes[selectedIndex] = Time.time + selectedAttack.Cooldown;

            return true;
        }

        // 공격 클립의 ApplyAttackHit 이벤트가 호출하는 순간 한 번만 판정한다.
        public void ApplyAttackHit()
        {
            bool canApplyHit = isActiveAndEnabled && !monster.IsDead &&
                CurrentAttack != null && IsMotionPlaying && hitEventPending;
            if (!canApplyHit)
                return;

            hitEventPending = false;
            
            Physics2D.OverlapBox(
                GetHitboxCenter(CurrentAttack, attackDirection), 
                CurrentAttack.HitboxSize, 
                0f, 
                hitFilter,
                hitResults);
            
            hitTargets.Clear();
            var context = new AttackHitContext(CurrentAttack, monster, attackDirection);
            
            foreach (Collider2D hit in hitResults)
            {
                PlayerCharacter target = hit.GetComponentInParent<PlayerCharacter>();
                bool canDamage = target != null && target.isActiveAndEnabled && !target.IsDead;
                
                if (!canDamage || !hitTargets.Add(target))
                    continue;

                // 같은 플레이어의 콜라이더가 여러 개 잡혀도 피해는 한 번만 준다.
                target.TakeDamage(context);
            }
        }

        public void Stop()
        {
            hitEventPending = false;
            CurrentAttack = null;
            
            if (clipPlayer != null)
                clipPlayer.Stop();
        }

        private bool CanUse(MonsterAttackData attack, float distanceSquared)
        {
            return attack != null && attack.AnimationClip != null &&
                attack.CanUseAtDistance(distanceSquared);
        }

        private Vector2 GetHitboxCenter(MonsterAttackData attack, int direction)
        {
            Vector2 offset = attack.HitboxOffset;
            
            return (Vector2)transform.position + new Vector2(offset.x * direction, offset.y);
        }

        private void OnDrawGizmosSelected()
        {
            Color previousColor = Gizmos.color;
            if (CurrentAttack != null)
            {
                // 실행 중인 공격은 시작할 때 고정한 방향으로 표시한다.
                Gizmos.color = hitEventPending ? Color.yellow : Color.red;
                DrawAttackHitbox(CurrentAttack, attackDirection);
            }
            else if (!Application.isPlaying && TryGetComponent<MonsterBrain>(out var brain) && brain.Data != null)
            {
                // 편집 중에는 등록된 공격들의 판정 범위를 함께 표시한다.
                var renderer = GetComponentInChildren<SpriteRenderer>();
                int direction = renderer != null && renderer.flipX ? -1 : 1;
                Gizmos.color = Color.cyan;
                foreach (MonsterAttackData attack in brain.Data.Attacks)
                {
                    if (attack != null)
                        DrawAttackHitbox(attack, direction);
                }
            }
            Gizmos.color = previousColor;
        }

        private void DrawAttackHitbox(MonsterAttackData attack, int direction)
        {
            Vector2 center = GetHitboxCenter(attack, direction);
            Gizmos.DrawWireCube(center, attack.HitboxSize);
            Gizmos.DrawSphere(center, 0.04f);
        }
    }
}
