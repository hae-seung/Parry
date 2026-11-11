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
        [Tooltip("움찔과 밀림의 지속 시간(초)입니다. 움찔 여부와 밀림 거리는 받은 공격 데이터에 따릅니다.")]
        [Min(0.01f)] public float duration;
    }

    
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AnimationClipPlayer), typeof(PlayerMovement))]
    public sealed class PlayerHitReaction : MonoBehaviour
    {
        private PlayerMovement movement;
        private AnimationClipPlayer clipPlayer;

        [SerializeField] private AnimationClip hurtClip;
        [SerializeField] private AnimationClip knockdownClip;
        [SerializeField] private AnimationClip getUpClip;
        [SerializeField] private PlayerHitReactionSettings normalHit = new PlayerHitReactionSettings { duration = 0.2f };
        [SerializeField] private PlayerHitReactionSettings heavyHit = new PlayerHitReactionSettings { duration = 0.35f };
        [Tooltip("일반 피격 경직이 끝난 뒤 추가로 보호하는 시간(초)입니다. 피격 무적 중 추가 타격은 무적 시간을 연장하지 않습니다.")]
        [SerializeField, Min(0f)] private float extraInvulnerability = 0.15f;
        [Tooltip("무릎을 꿇는 모션이 끝난 뒤 일어나기 전 기다리는 시간(초)입니다.")]
        [SerializeField, Min(0f)] private float knockdownHoldDuration = 0.35f;

        private Coroutine reactionCoroutine;
        private float invulnerableUntil;
        private bool knockdownInvulnerable;

        public bool IsReacting { get; private set; }
        public bool IsKnockedDown { get; private set; }
        public bool IsInvulnerable => knockdownInvulnerable || Time.time < invulnerableUntil;

        private void Awake()
        {
            movement = GetComponent<PlayerMovement>();
            clipPlayer = GetComponent<AnimationClipPlayer>();
        }

        private void OnDisable() => CancelReaction();

        public void StartReaction(AttackHitContext context)
        {
            CancelReaction();
            
            PlayerHitReactionSettings settings = context.AttackType == MonsterAttackType.Heavy ? heavyHit : normalHit;
            
            IsReacting = context.CausesStagger || context.KnocksDown;
            
            knockdownInvulnerable = context.KnocksDown;
            invulnerableUntil = Time.time + settings.duration + extraInvulnerability;
            if (IsReacting)
                movement.CancelJump();

            // 공격자에게서 멀어지는 방향으로 밀고, 겹쳤을 때는 공격 방향을 사용한다.
            float deltaX = context.Attacker != null ? transform.position.x - context.Attacker.transform.position.x : 0f;
            int direction = context.AttackDirection != 0 ? context.AttackDirection : -movement.FacingDirection;
            if (Mathf.Abs(deltaX) > 0.01f)
                direction = deltaX > 0f ? 1 : -1;
            movement.StartKnockback(direction, context.KnockbackDistance, settings.duration);
            reactionCoroutine = StartCoroutine(React(settings.duration, context.CausesStagger, context.KnocksDown));
        }

        private IEnumerator React(float duration, bool causesStagger, bool knocksDown)
        {
            // 흰색 프레임을 제외한 움찔 자세를 유지하며 설정한 경직 시간을 기다린다.
            if (causesStagger)
                clipPlayer.Play(hurtClip, true);
            yield return new WaitForSeconds(duration);
            movement.StopKnockback();

            if (knocksDown)
            {
                IsKnockedDown = true;
                clipPlayer.Play(knockdownClip, true);
                yield return new WaitForSeconds(knockdownClip.length);
                yield return new WaitForSeconds(knockdownHoldDuration);

                // 기상 클립이 완전히 끝날 때까지 행동 금지와 무적을 유지한다.
                clipPlayer.Play(getUpClip);
                while (clipPlayer.IsPlayingClip(getUpClip))
                    yield return null;
            }

            FinishReaction();
        }

        public void CancelReaction()
        {
            if (reactionCoroutine != null)
                StopCoroutine(reactionCoroutine);
            invulnerableUntil = 0f;
            FinishReaction();
        }

        private void FinishReaction()
        {
            reactionCoroutine = null;
            IsReacting = false;
            IsKnockedDown = false;
            knockdownInvulnerable = false;
            if (movement != null)
                movement.StopKnockback();
            if (clipPlayer == null)
                return;

            bool playingReaction = clipPlayer.IsPlayingClip(hurtClip) ||
                clipPlayer.IsPlayingClip(knockdownClip) || clipPlayer.IsPlayingClip(getUpClip);
            if (playingReaction)
                clipPlayer.Stop();
        }
    }
}
