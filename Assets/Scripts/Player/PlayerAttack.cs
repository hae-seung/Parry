using System.Collections.Generic;
using Parry.MonsterClass;
using Parry.AnimationClass;
using Parry.CombatClass;
using UnityEngine;

namespace Parry.PlayerClass
{
    [RequireComponent(typeof(PlayerCharacter), typeof(AnimationClipPlayer))]
    public sealed class PlayerAttack : MonoBehaviour
    {
        private PlayerCharacter character;
        private AnimationClipPlayer clipPlayer;
        private PlayerBerseker berseker;

        private PlayerAttackSetData data;

        private readonly HashSet<ICombatTarget> hitTargetSet = new HashSet<ICombatTarget>();
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

        private bool CanContinueCombo => Time.time - lastAttackTime < data.comboResetDuration;

        public void Initialize(PlayerAttackSetData data)
        {
            this.data = data;
            character = GetComponent<PlayerCharacter>();
            clipPlayer = GetComponent<AnimationClipPlayer>();
            berseker = GetComponent<PlayerBerseker>();

            hitFilter = new ContactFilter2D();
            hitFilter.SetLayerMask(data.targetLayers);
            hitFilter.useTriggers = true;
        }

        private void OnDisable() => CancelAttack();

        private bool CanAttack()
        {
            bool isActionLocked = character.IsDead || character.IsHitReacting || character.IsRoaring;
            
            bool isBusy = character.IsRolling || character.IsParrying;

            if (isActionLocked)
                return false;
            
            if (!character.IsGrounded || isBusy)
                return false;

            bool hasAttacks = data.attacks.Count > 0;
            
            return isActiveAndEnabled && clipPlayer.CanPlay && hasAttacks;
        }

        public bool TryStartAttack()
        {
            //이미 예약이 되었으면 자동으로 실행될거임.
            if (!CanAttack() || bIsReservedNextAttack)
                return false;

            if (!IsAttacking)
            {
                return StartAttack(CanContinueCombo ? attackIndex : 0);
            }

            //3타 -> 1타로는 예약 불가
            //1타->2타->3타가 한묶음
            if (!bCanReserveNextAttack || attackIndex >= data.attacks.Count)
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
            index %= data.attacks.Count;

            // 공격 시작 시 선택한 데이터는 모드가 끝나도 현재 공격이 끝날 때까지 유지한다.
            PlayerAttackData nextAttack = berseker.IsBerserking ? data.berserkerAttacks[index] : data.attacks[index];
            if (nextAttack == null || nextAttack.AnimationClip == null)
                return false;

            attackIndex = index;
            currentAttack = nextAttack;
            attackDirection = character.FacingDirection;
            hitEventPending = true;

            bIsAttacking = true;
            bCanReserveNextAttack = false;
            clipPlayer.Play(nextAttack.AnimationClip);

            return true;
        }

        // 실제 공격판정을 내리라고 애니메이션으로부터 이벤트가 들어올때
        public void ApplyAttackHit()
        {
            if (!isActiveAndEnabled || character.IsDead || !hitEventPending)
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
                ICombatTarget target = hit.GetComponentInParent<ICombatTarget>();
                
                if (target == null || !target.CanTakeDamage || !hitTargetSet.Add(target))
                    continue;
                
                MonsterHitContext hitContext = character.Stats.CreateMonsterHit(damage, PlayerAttackType.Normal);
                if (berseker.IsBerserking)
                {
                    AddBerserkerInfo(ref hitContext);
                }

                target.TakeDamage(ref hitContext);

                // 3타가 실제로 적중한 대상마다 한 번만 표시한다.
                if (attackIndex == 2)
                {
                    MakeThirdAttackEffect(hit.bounds.center);
                }
            }

            //실제 몬스터를 때린경우에만 콤보 이어지도록
            if (hitTargetSet.Count > 0)
            {
                attackIndex += 1;
                character.PlayHitStop();
            }
            else
            {
                attackIndex = 0;
            }
        }

        private void AddBerserkerInfo(ref MonsterHitContext context)
        {
            context.IsBerserking = true;
            context.Berseker = berseker;

            context.SlashStackCount = data.berserkerAttacks[attackIndex].SlashStackCount;
            context.StoredDamage = context.Damage * data.berserkerStoredDamagePercent * 0.01f;
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

            bool reservedNextAttack = bIsReservedNextAttack;
            bIsReservedNextAttack = false;
            if (reservedNextAttack && CanAttack() && StartAttack(attackIndex))
                return;

            clipPlayer.Stop();
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
            
            clipPlayer.Stop();
        }

        private void MakeThirdAttackEffect(Vector3 pos)
        {
            GameObject effect = ObjectPoolManager.Instance.SpawnObject(data.thirdHitEffectPrefab, pos);
            
            effect.GetComponent<SpriteRenderer>().flipX = attackDirection < 0;
        }
        
        private Vector2 GetHitboxCenter()
        {
            Vector2 offset = currentAttack.HitboxOffset;
            return (Vector2)transform.position + new Vector2(offset.x * attackDirection, offset.y);
        }

        private void OnDrawGizmos()
        {
            if (!IsAttacking)
                return;

            Color previousColor = Gizmos.color;
            Gizmos.color = hitEventPending ? Color.yellow : Color.red;
            Gizmos.DrawWireCube(GetHitboxCenter(), currentAttack.HitboxSize);
            Gizmos.color = previousColor;
        }
    }
}
