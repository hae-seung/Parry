using System;
using UnityEngine;
using Parry.AnimationClass;

namespace Parry.PlayerClass
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerParryGauge))]
    public sealed class PlayerBerseker : MonoBehaviour
    {
        private PlayerBerserkerData data;

        [SerializeField] private bool roaring;

        private PlayerParryGauge gauge;
        private PlayerCharacter character;
        private PlayerMovement movement;
        private AnimationClipPlayer clipPlayer;
        private GameObject auraInstance;
        private float timeScaleBeforeRoar;
        private float nextBerserkerTime;

        public event Action<GameObject> OnBurstStoredSlashes;
        public event Action OnDiscardStoredSlashes;
        public event Action EnterBerserker;
        public event Action ExitBerserker;
        public event Action<float> OnRemainingTimeChanged;
        public event Action<float> OnCooldownChanged;

        public bool IsBerserking { get; private set; }
        public float GaugeCost => data.gaugeCost;
        public float RemainingSeconds { get; private set; }
        public float NormalizedRemainingTime => RemainingSeconds / data.duration;
        public bool IsRoaring => roaring;
        public GameObject ExecutionEffectPrefab => data.burserkerExecutionVfx;

        public void Initialize(PlayerBerserkerData data)
        {
            this.data = data;
            gauge = GetComponent<PlayerParryGauge>();
            character = GetComponent<PlayerCharacter>();
            movement = GetComponent<PlayerMovement>();
            clipPlayer = GetComponent<AnimationClipPlayer>();
        }

        private void OnDisable() => EndBerserker(false);

        private void Update()
        {
            if (character.IsDead)
                return;
            
            if (!IsBerserking || roaring)
                return;

            //버서커모드 남은 지속시간
            RemainingSeconds = Mathf.Max(0f, RemainingSeconds - Time.deltaTime);
            OnRemainingTimeChanged?.Invoke(NormalizedRemainingTime);

            if (RemainingSeconds <= 0f)
                EndBerserker(false);
        }
        
        public void InitUI()
        {
            OnRemainingTimeChanged?.Invoke(NormalizedRemainingTime);
            OnCooldownChanged?.Invoke(nextBerserkerTime);
        }

        public void ReduceTimeOnHit()
        {
            if (!IsBerserking)
                return;

            RemainingSeconds = Mathf.Max(0f, RemainingSeconds - data.timeLossOnHit);
            OnRemainingTimeChanged?.Invoke(NormalizedRemainingTime);

            if (RemainingSeconds <= 0f)
                EndBerserker(false);
        }

        public void RecoverTimeOnParry()
        {
            RemainingSeconds = Mathf.Min(data.duration, RemainingSeconds + data.timeRecoveryOnParry);
            OnRemainingTimeChanged?.Invoke(NormalizedRemainingTime);
        }

        //버서커 On/Off
        public void ToggleBerserker()
        {
            if (!isActiveAndEnabled || character.IsDead)
                return;

            // 진입은 이미 성공했지만, 포효가 끝나기 전에는 R로 종료할 수 없다.
            if (roaring)
                return;

            if (IsBerserking)
            {
                EndBerserker(true);
                return;
            }

            //버서커 진입
            if (!CanEnterBerserker())
                return;

            if (gauge.TryConsume(data.gaugeCost))
            {
                roaring = true;
                
                timeScaleBeforeRoar = Time.timeScale;
                clipPlayer.Play(data.roarClip, false, true);
                Time.timeScale = 0f;
                
                IsBerserking = true;
                RemainingSeconds = data.duration;
                OnRemainingTimeChanged?.Invoke(NormalizedRemainingTime);
                
                auraInstance = ObjectPoolManager.Instance.SpawnObject(
                    data.auraVfx, 
                    transform.position + data.auraOffset, 
                    transform);
                
                EnterBerserker?.Invoke();
            }
        }

        private bool CanEnterBerserker()
        {
            bool cooldownReady = Time.time >= nextBerserkerTime;
            if (!cooldownReady)
                return false;

            if (character.IsRolling || character.IsAttacking || character.IsParrying)
                return false;

            if (!character.IsGrounded
                || movement.HasPendingJump
                || Mathf.Abs(character.Velocity.y) > 0.01f)
                return false;

            if (character.IsHitReacting)
                return false;

            bool animationReady = clipPlayer.CanPlay;
            if (Time.timeScale == 0f || !animationReady)
                return false;

            return true;
        }

        // 포효 클립 마지막 Animation Event에서 입력 잠금을 해제한다.
        public void EndRoar()
        {
            StopRoar();
        }

        // 정상 완료와 비활성화 정리에서 공통으로 포효 재생과 게임 정지를 해제한다.
        private void StopRoar()
        {
            roaring = false;
            Time.timeScale = timeScaleBeforeRoar;
            clipPlayer.Stop();
        }

        
        //버서커 Off
        public void EndBerserker(bool releaseSlashes)
        {
            if (!IsBerserking)
                return;

            IsBerserking = false;
            nextBerserkerTime = Time.time + data.cooldown;
            OnCooldownChanged?.Invoke(nextBerserkerTime);
            
            RemainingSeconds = 0;
            OnRemainingTimeChanged?.Invoke(NormalizedRemainingTime);

            if (auraInstance != null)
                auraInstance.GetComponent<PooledObject>().Release();
            auraInstance = null;

            //스스로 R을 눌러 참격을 터뜨린 경우에만 피해를 입힘
            if (releaseSlashes)
                BurstStoredSlashes();
            else
                DiscardStoredSlashes();
            
            ExitBerserker?.Invoke();
        }

        private void BurstStoredSlashes()
        {
            OnBurstStoredSlashes?.Invoke(data.burserkerHitVfx);
        }

        private void DiscardStoredSlashes()
        {
            OnDiscardStoredSlashes?.Invoke();
        }
    }
}
