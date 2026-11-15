using System;
using System.Collections;
using Parry.AnimationClass;
using Parry.CombatClass;
using Parry.MonsterClass;
using UnityEngine;
using UnityEngine.Serialization;

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

        [SerializeField] private AnimationClip hurtClip;
        [FormerlySerializedAs("knockdownClip")]
        [SerializeField] private AnimationClip faintClip;
        [SerializeField] private AnimationClip getUpClip;
        [SerializeField] private PlayerHitReactionSettings normalHit = new PlayerHitReactionSettings { duration = 0.2f };
        [SerializeField] private PlayerHitReactionSettings heavyHit = new PlayerHitReactionSettings { duration = 0.35f };
        [Tooltip("일반 피격 경직이 끝난 뒤 추가로 보호하는 시간(초)입니다. 피격 무적 중 추가 타격은 무적 시간을 연장하지 않습니다.")]
        [SerializeField, Min(0f)] private float extraInvulnerability = 0.15f;
        [Tooltip("무릎을 꿇는 모션이 끝난 뒤 일어나기 전 기다리는 시간(초)입니다.")]
        [FormerlySerializedAs("knockdownHoldDuration")]
        [SerializeField, Min(0f)] private float faintHoldDuration = 0.35f;

        private Coroutine reactionCoroutine;
        private float hitInvulnerabilityEndTime;
        private bool bIsInvulnerableWhileFainted;

        public bool IsReacting { get; private set; }
        public bool IsFainted { get; private set; }
        public bool IsInvulnerable => bIsInvulnerableWhileFainted || Time.time < hitInvulnerabilityEndTime;

        private void Awake()
        {
            movement = GetComponent<PlayerMovement>();
            clipPlayer = GetComponent<AnimationClipPlayer>();
        }

        private void OnDisable() => CancelReaction();

        public void StartReaction(AttackHitContext context)
        {
            //CancelReaction();
            
            PlayerHitReactionSettings settings = context.AttackType == MonsterAttackType.Heavy ? heavyHit : normalHit;
            
            IsReacting = context.CanKnockback || context.CausesFaint;
            
            //강공으로 인한 기절 상태시 다시 일어날때까지 무적을 유지해야함.
            bIsInvulnerableWhileFainted = context.CausesFaint;
            hitInvulnerabilityEndTime = Time.time + settings.duration + extraInvulnerability;
            
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
                clipPlayer.Play(hurtClip, true);
            }
            
            yield return new WaitForSeconds(duration);
            
            movement.StopKnockback();

            
            
            if (causesFaint)
            {
                IsFainted = true;
                clipPlayer.Play(faintClip, true);
                
                yield return new WaitForSeconds(faintClip.length + faintHoldDuration);

                // 기상 클립이 완전히 끝날 때까지 행동 금지와 무적을 유지한다.
                clipPlayer.Play(getUpClip);
                
                while (clipPlayer.IsPlayingClip(getUpClip))
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

            bool playingReaction = clipPlayer.IsPlayingClip(hurtClip) ||
                clipPlayer.IsPlayingClip(faintClip) || clipPlayer.IsPlayingClip(getUpClip);
            
            if (playingReaction)
                clipPlayer.Stop();
        }
    }
}
