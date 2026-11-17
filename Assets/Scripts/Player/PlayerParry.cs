using System;
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
        private PlayerBerseker berseker;
        
        private PlayerParryData data;

        private bool isParrying;
        private float parryEndTime;
        private float nextParryTime;

        public event Action<float> OnCooldownChanged;

        public void InitUI()
        {
            OnCooldownChanged?.Invoke(nextParryTime);
        }

        // 타격이 들어오는 순간에도 시간을 확인해 Update 실행 순서에 영향을 받지 않는다.
        public bool IsParrying => isParrying && !character.IsDead && Time.time < parryEndTime;

        public void Initialize(PlayerParryData data)
        {
            this.data = data;
            character = GetComponent<PlayerCharacter>();
            movement = GetComponent<PlayerMovement>();
            attack = GetComponent<PlayerAttack>();
            clipPlayer = GetComponent<AnimationClipPlayer>();
            gauge = GetComponent<PlayerParryGauge>();
            berseker = GetComponent<PlayerBerseker>();
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

            clipPlayer.Play(data.parryClip);

            isParrying = true;
            parryEndTime = Time.time + data.parryDuration;
            nextParryTime = Time.time + data.parryCooldown;
            OnCooldownChanged?.Invoke(nextParryTime);
            
            movement.CancelJump();
            
            return true;
        }

        public bool TryParryHit()
        {
            if (!isActiveAndEnabled || !IsParrying)
                return false;

            // 성공 순간부터 다음 패링까지의 쿨타임을 짧게 줄인다.
            nextParryTime = Time.time + data.successCooldown;
            OnCooldownChanged?.Invoke(nextParryTime);
            
            if (berseker.IsBerserking)
                berseker.RecoverTimeOnParry();
            else
                gauge.GainOnParrySuccess();
            
            return true;
        }
        
        public void PlaySuccessEffects(Vector3 monsterPosition)
        {
            //플레이어 패링 성공 방패 이펙트
            ObjectPoolManager.Instance.SpawnObject(
                data.shieldBubblePrefab,
                transform.position + data.shieldEffectOffset,
                transform);

            //패링 성공 몬스터에게 데미지 이펙트
            ObjectPoolManager.Instance.SpawnObject(data.parryHitPrefab, monsterPosition);
        }

        public void CancelParry()
        {
            isParrying = false;
            
            // 패링 종료가 이후에 시작한 다른 행동의 클립을 끊지 않도록 한다.
            if (clipPlayer.IsPlayingClip(data.parryClip))
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

            bool animationReady = clipPlayer.CanPlay && data.parryClip != null;
            return isActiveAndEnabled && animationReady;
        }
    }
}
