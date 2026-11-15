using System;
using System.Collections;
using UnityEngine;
using Parry.AnimationClass;

namespace Parry.PlayerClass
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerParryGauge))]
    public sealed class PlayerBerseker : MonoBehaviour
    {
        [Tooltip("버서커 진입 시 필요한 게이지이자 소비량입니다.")]
        [SerializeField, Min(1f)] private float gaugeCost = 70f;
        [Tooltip("버서커 지속시간(초)입니다. 현재 기본값은 조절용 임시값입니다.")]
        [SerializeField, Min(1)] private int duration = 10;
        [Tooltip("버서커 종료 후 다음 진입까지의 대기시간(초)입니다.")]
        [SerializeField, Min(0f)] private float cooldown = 3f;
        [SerializeField] private GameObject auraPrefab;
        [SerializeField] private Vector3 auraOffset;
        [SerializeField] private AnimationClip roarClip;
        [SerializeField] private bool roaring;

        private PlayerParryGauge gauge;
        private PlayerCharacter character;
        private PlayerMovement movement;
        private AnimationClipPlayer clipPlayer;
        private Coroutine countdownCoroutine;
        private GameObject auraInstance;
        private float timeScaleBeforeRoar;
        private float nextBerserkerTime;
        private WaitForSeconds waitBersekerTime = new WaitForSeconds(1.0f);

        public event Action OnBurstStoredSlashes;
        public event Action OnDiscardStoredSlashes;

        public bool IsBerserking { get; private set; }
        public int RemainingSeconds { get; private set; }
        public bool IsRoaring => roaring;

        private void Awake()
        {
            gauge = GetComponent<PlayerParryGauge>();
            character = GetComponent<PlayerCharacter>();
            movement = GetComponent<PlayerMovement>();
            clipPlayer = GetComponent<AnimationClipPlayer>();
        }

        private void OnDisable() => EndBerserker(false);
        
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

            if (gauge.TryConsume(gaugeCost))
            {
                roaring = true;
                
                timeScaleBeforeRoar = Time.timeScale;
                clipPlayer.Play(roarClip, false, true);
                Time.timeScale = 0f;
                
                IsBerserking = true;
                RemainingSeconds = duration;
                
                auraInstance = ObjectPoolManager.Instance.SpawnObject(
                    auraPrefab, 
                    transform.position + auraOffset, 
                    transform);
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
            countdownCoroutine = StartCoroutine(Countdown());
        }

        // 정상 완료와 비활성화 정리에서 공통으로 포효 재생과 게임 정지를 해제한다.
        private void StopRoar()
        {
            roaring = false;
            Time.timeScale = timeScaleBeforeRoar;
            clipPlayer.Stop();
        }

        private IEnumerator Countdown()
        {
            while (RemainingSeconds > 0)
            {
                yield return waitBersekerTime;
                RemainingSeconds--;
            }

            countdownCoroutine = null;
            EndBerserker(false);
        }

        
        //버서커 Off
        public void EndBerserker(bool releaseSlashes)
        {
            if (!IsBerserking)
                return;

            IsBerserking = false;
            nextBerserkerTime = Time.time + cooldown;
            
            if (roaring)
                StopRoar();
            
            RemainingSeconds = 0;

            ObjectPoolManager.Instance.Release(auraInstance);
            auraInstance = null;

            if (countdownCoroutine != null)
            {
                StopCoroutine(countdownCoroutine);
                countdownCoroutine = null;
            }

            //스스로 R을 눌러 참격을 터뜨린 경우에만 피해를 입힘
            if (releaseSlashes)
                BurstStoredSlashes();
            else
                DiscardStoredSlashes();
        }

        private void BurstStoredSlashes()
        {
            OnBurstStoredSlashes?.Invoke();
        }

        private void DiscardStoredSlashes()
        {
            OnDiscardStoredSlashes?.Invoke();
        }
    }
}
