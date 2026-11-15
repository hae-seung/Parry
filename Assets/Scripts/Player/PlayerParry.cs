using Parry.AnimationClass;
using UnityEngine;

namespace Parry.PlayerClass
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AnimationClipPlayer))]
    [RequireComponent(typeof(PlayerParryGauge))]
    public sealed class PlayerParry : MonoBehaviour
    {
        private PlayerCharacter character;
        private PlayerMovement movement;
        private PlayerAttack attack;
        private AnimationClipPlayer clipPlayer;
        private PlayerParryGauge gauge;
        
        [SerializeField] private GameObject shieldBubblePrefab;
        [SerializeField] private GameObject parryHitPrefab;
        [SerializeField] private Vector3 shieldEffectOffset;
        

        [Tooltip("패링 입력 시 클립 재생기로 재생할 애니메이션입니다.")]
        [SerializeField] private AnimationClip parryClip;
        [Tooltip("패링 시작 순간부터 적의 타격을 패링할 수 있는 시간(초)입니다. 클립 길이와 별개로 계산합니다.")]
        [SerializeField, Min(0f)] private float parryDuration = 0.3f;
        [Tooltip("패링 시도 시작부터 다음 패링까지의 시간(초)입니다.")]
        [SerializeField, Min(0f)] private float parryCooldown = 3f;
        [Tooltip("패링 성공 순간부터 다음 패링까지의 시간(초)입니다.")]
        [SerializeField, Min(0f)] private float successCooldown = 0.2f;

        private bool isParrying;
        private float parryEndTime;
        private float nextParryTime;

        // 타격이 들어오는 순간에도 시간을 확인해 Update 실행 순서에 영향을 받지 않는다.
        public bool IsParrying => isParrying && !character.IsDead && Time.time < parryEndTime;

        private void Awake()
        {
            character = GetComponent<PlayerCharacter>();
            movement = GetComponent<PlayerMovement>();
            attack = GetComponent<PlayerAttack>();
            clipPlayer = GetComponent<AnimationClipPlayer>();
            gauge = GetComponent<PlayerParryGauge>();
        }

        private void Update()
        {
            if (isParrying && !IsParrying)
                CancelParry();
        }

        private void OnDisable() => CancelParry();

        public bool TryStartParry()
        {
            bool canStart = CheckCanParry();
            
            if (!canStart)
                return false;

            // 일반 공격을 취소하고 같은 클립 재생기에 패링 클립을 연결한다.
            if (attack.IsAttacking)
                attack.CancelAttack();

            clipPlayer.Play(parryClip);

            isParrying = true;
            parryEndTime = Time.time + parryDuration;
            nextParryTime = Time.time + parryCooldown;
            
            movement.CancelJump();
            
            return true;
        }

        public bool TryParryHit()
        {
            if (!isActiveAndEnabled || !IsParrying)
                return false;

            // 성공 순간부터 다음 패링까지의 쿨타임을 짧게 줄인다.
            nextParryTime = Time.time + successCooldown;
            gauge.GainOnParrySuccess();
            
            return true;
        }
        
        public void PlaySuccessEffects(Vector3 monsterPosition)
        {
            //플레이어 패링 성공 방패 이펙트
            ObjectPoolManager.Instance.SpawnObject(
                shieldBubblePrefab,
                transform.position + shieldEffectOffset,
                transform);

            //패링 성공 몬스터에게 데미지 이펙트
            ObjectPoolManager.Instance.SpawnObject(parryHitPrefab, monsterPosition);
        }

        public void CancelParry()
        {
            isParrying = false;
            
            // 패링 종료가 이후에 시작한 다른 행동의 클립을 끊지 않도록 한다.
            if (clipPlayer.IsPlayingClip(parryClip))
                clipPlayer.Stop();
        }

        private bool CheckCanParry()
        {
            bool isActionLocked = character.IsDead || character.IsHitReacting || character.IsRoaring;
            // 일반 공격 취소 패링은 허용한다.
            bool isBusy = character.IsRolling || IsParrying;

            if (isActionLocked || isBusy)
                return false;

            bool cooldownReady = Time.time >= nextParryTime;
            if (!cooldownReady)
                return false;

            bool animationReady = clipPlayer.CanPlay && parryClip != null;
            return isActiveAndEnabled && animationReady;
        }
    }
}
