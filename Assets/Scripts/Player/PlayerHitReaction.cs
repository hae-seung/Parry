using System;
using System.Collections;
using Parry.AnimationClass;
using Parry.CombatClass;
using Parry.MonsterClass;
using UnityEngine;

namespace Parry.PlayerClass
{
    [Serializable]
    public struct PlayerHitReactionSettings
    {
        [Tooltip("넉백과 밀림의 지속 시간(초)입니다. 넉백 여부와 밀림 거리는 받은 공격 데이터에 따릅니다.")]
        [Min(0.01f)] public float duration;
    }

    
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AnimationClipPlayer), typeof(PlayerMovement))]
    public sealed class PlayerHitReaction : MonoBehaviour
    {
        private PlayerMovement movement;
        private AnimationClipPlayer clipPlayer;

        private PlayerHitReactionData data;

        private Coroutine reactionCoroutine;
        private float hitInvulnerabilityEndTime;
        private bool bIsInvulnerableWhileFainted;

        public bool IsReacting { get; private set; }
        public bool IsFainted { get; private set; }
        public bool IsInvulnerable => bIsInvulnerableWhileFainted || Time.time < hitInvulnerabilityEndTime;

        public void Initialize(PlayerHitReactionData data)
        {
            this.data = data;
            movement = GetComponent<PlayerMovement>();
            clipPlayer = GetComponent<AnimationClipPlayer>();
        }

        private void OnDisable() => CancelReaction();

        public void StartReaction(ref AttackHitContext context)
        {
            //CancelReaction();
            
            PlayerHitReactionSettings settings = context.AttackType == MonsterAttackType.Heavy ? data.heavyHit : data.normalHit;
            
            IsReacting = context.CanKnockback || context.CausesFaint;
            
            //강공으로 인한 기절 상태시 다시 일어날때까지 무적을 유지해야함.
            bIsInvulnerableWhileFainted = context.CausesFaint;
            hitInvulnerabilityEndTime = Time.time + settings.duration + data.extraInvulnerability;
            
            if (IsReacting)
                movement.CancelJump();

            // 공격자에게서 멀어지는 방향으로 밀고, 겹쳤을 때는 공격 방향을 사용한다.
            float deltaX = transform.position.x - context.Attacker.transform.position.x;
            int direction = context.AttackDirection;

            if (Mathf.Abs(deltaX) > 0.01f)
                direction = deltaX > 0f ? 1 : -1;
            
            //넉백이 있다면 넉백
            if(context.CanKnockback)
                movement.StartKnockback(direction, context.KnockbackDistance, settings.duration);

            // 넉백이나 기절이 있으면 반응이 끝난 뒤 행동 잠금을 해제한다.
            if (IsReacting)
            {
                reactionCoroutine = StartCoroutine(
                    ReactionRoutine(settings.duration, context.CanKnockback, context.CausesFaint));
            }
        }

        private IEnumerator ReactionRoutine(float duration, bool canKnockback, bool causesFaint)
        {
            // 흰색 프레임을 제외한 넉백 자세를 유지하며 설정한 경직 시간을 기다린다.
            if (canKnockback)
            {
                clipPlayer.Play(data.hurtClip, true);
            }
            
            yield return new WaitForSeconds(duration);
            
            movement.StopKnockback();

            
            
            if (causesFaint)
            {
                IsFainted = true;
                clipPlayer.Play(data.faintClip, true);
                
                yield return new WaitForSeconds(data.faintClip.length + data.faintHoldDuration);

                // 기상 클립이 완전히 끝날 때까지 행동 금지와 무적을 유지한다.
                clipPlayer.Play(data.getUpClip);
                
                while (clipPlayer.IsPlayingClip(data.getUpClip))
                {
                    yield return null;
                }
            }

            FinishReaction();
        }

        //죽었을때 실행됨
        public void CancelReaction()
        {
            if (reactionCoroutine != null)
                StopCoroutine(reactionCoroutine);
            
            hitInvulnerabilityEndTime = 0f;
            
            FinishReaction();
        }

        private void FinishReaction()
        {
            reactionCoroutine = null;
            IsReacting = false;
            IsFainted = false;
            //무적 종료
            bIsInvulnerableWhileFainted = false;
            
            movement.StopKnockback();

            bool playingReaction = clipPlayer.IsPlayingClip(data.hurtClip) ||
                clipPlayer.IsPlayingClip(data.faintClip) || clipPlayer.IsPlayingClip(data.getUpClip);
            
            if (playingReaction)
                clipPlayer.Stop();
        }
    }
}
