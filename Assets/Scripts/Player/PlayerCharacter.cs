using Parry.CombatClass;
using Parry.AnimationClass;
using Parry.CameraClass;
using UnityEngine;

namespace Parry.PlayerClass
{
    [RequireComponent(typeof(PlayerMovement), typeof(PlayerStatComponent))]
    [RequireComponent(typeof(PlayerParry), typeof(PlayerHitReaction))]
    [RequireComponent(typeof(AnimationClipPlayer))]
    [RequireComponent(typeof(PlayerBerseker))]
    public sealed class PlayerCharacter : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float rollCooldown = 0.3f;
        [Tooltip("공격 적중 후 남은 공격 애니메이션의 재생 배율입니다. 1은 정상 속도입니다.")]
        [SerializeField, Range(0.01f, 1f)] private float attackHitPlaybackSpeed = 0.7f;
        [SerializeField] private AnimationClip rollClip;
        [Tooltip("구르기 시작부터 완벽회피로 피해와 피격 반응을 막는 시간(초)입니다. 구르기가 끝나면 무적도 종료됩니다.")]
        [SerializeField, Min(0f)] private float rollInvulnerabilityDuration = 0.1f;

        private float lastRollTime;
        private float rollInvulnerabilityEndTime;
        private PlayerMovement movement;
        private PlayerAttack attack;
        private PlayerParry parry;
        private PlayerBerseker berseker;
        private PlayerHitReaction hitReaction;
        private AnimationClipPlayer clipPlayer;
        public PlayerStatComponent Stats { get; private set; }
        public bool IsDead => Stats.IsDead;

        public bool IsGrounded => movement.IsGrounded;
        public int FacingDirection => movement.FacingDirection;
        public Vector2 Velocity => movement.Velocity;
        public bool IsRolling { get; private set; }
        public bool IsRollInvulnerable => IsRolling && Time.time < rollInvulnerabilityEndTime;
        public bool IsParrying => parry.IsParrying;
        public bool IsAttacking => attack.IsAttacking;
        public bool IsHitReacting => hitReaction.IsReacting;
        public bool IsFainted => hitReaction.IsFainted;
        public bool IsRoaring => berseker.IsRoaring;

        private void Awake()
        {
            movement = GetComponent<PlayerMovement>();
            Stats = GetComponent<PlayerStatComponent>();
            attack = GetComponent<PlayerAttack>();
            parry = GetComponent<PlayerParry>();
            berseker = GetComponent<PlayerBerseker>();
            hitReaction = GetComponent<PlayerHitReaction>();
            clipPlayer = GetComponent<AnimationClipPlayer>();
        }

        private void OnDisable()
        {
            if (IsRolling)
                EndRoll();
        }

        public void Move(float horizontal)
        {
            movement.Move(horizontal);
        }

        public void Jump()
        {
            bool isActionLocked = IsDead || IsHitReacting || IsRoaring;
            bool isBusy = IsRolling || IsParrying || IsAttacking;

            if (isActionLocked || isBusy)
                return;

            movement.Jump();
        }

        public void Attack() => attack.TryStartAttack();
        public void Parry() => parry.TryStartParry();
        public void Skill() => berseker.ToggleBerserker();
        

        // 적의 공격을 받으면 플레이어가 방어 여부를 판단한 뒤 체력에 반영한다.
        public void TakeDamage(AttackHitContext context)
        {
            bool validHit = isActiveAndEnabled && !IsDead && context.Damage > 0f && !float.IsInfinity(context.Damage);
            
            if (!validHit)
                return;

            if (parry.TryParryHit())
            {
                parry.PlaySuccessEffects(context.Attacker.transform.position);
                
                context.Attacker.TakeDamage(
                    Stats.CreateMonsterHit(Stats.ParryCounterDamage, PlayerAttackType.ParryCounter));
                
                
                return;
            }

            //구르기중에서도 완벽회피 판정 가능(일정시간동안만)
            if (IsRollInvulnerable)
            {
                Debug.Log("perfect avoid");
                return;
            }

            //쳐맞고 무적이 된 상태
            if (hitReaction.IsInvulnerable)
            {
                Debug.Log("무적중..피격 안되지");
                return;
            }
            

            Stats.TakeDamage(context.Damage);
            CameraFeedback.Instance.ShakePlayerHit();
            
            bool interruptsAction = context.CanKnockback || context.CausesFaint || IsDead;
            if (interruptsAction)
            {
                attack.CancelAttack();
                parry.CancelParry();
                if (IsRolling)
                    EndRoll();
            }

            if (IsDead)
            {
                berseker.EndBerserker(false);
                hitReaction.CancelReaction();
                return;
            }

            hitReaction.StartReaction(context);
        }

        public void PlayHitStop()
        {
            // PlayerAttack이 실제 공격 적중을 확인한 뒤 호출한다.
            clipPlayer.SetSpeed(attackHitPlaybackSpeed);
            CameraFeedback.Instance.ShakePlayerAttack();
        }

        public void Roll()
        {
            bool isActionLocked = IsDead || IsHitReacting || IsRoaring;
            // 공격 중 구르기는 허용하고, 구르기·패링 중에는 막는다.
            bool isBusy = IsRolling || IsParrying;

            if (isActionLocked)
                return;
            if (!IsGrounded || isBusy)
                return;

            bool cooldownReady = Time.time >= lastRollTime + rollCooldown;
            bool animationReady = clipPlayer.CanPlay && rollClip != null;
            if (!cooldownReady || !animationReady)
                return;

            //공격을 캔슬하고 구르기 가능
            if (attack.IsAttacking)
                attack.CancelAttack();
            
            clipPlayer.Play(rollClip);

            movement.FaceMoveInput();
            IsRolling = true;
            rollInvulnerabilityEndTime = Time.time + rollInvulnerabilityDuration;
            movement.CancelJump();
        }

        public void EndRoll()
        {
            if (clipPlayer.IsPlayingClip(rollClip))
                clipPlayer.Stop();
            
            IsRolling = false;
            rollInvulnerabilityEndTime = 0f;
            lastRollTime = Time.time;
        }

    }
}
