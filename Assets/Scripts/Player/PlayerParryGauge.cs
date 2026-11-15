using UnityEngine;

namespace Parry.PlayerClass
{
    [DisallowMultipleComponent]
    public sealed class PlayerParryGauge : MonoBehaviour
    {
        [Tooltip("게이지 최대량입니다. 현재 수치는 플레이 검증용 임시값입니다.")]
        [SerializeField, Min(1f)] private float maxGauge = 100f;
        [Tooltip("패링 성공 한 번에 획득하는 게이지입니다.")]
        [SerializeField, Min(0f)] private float gaugePerParry = 15f;
        [Tooltip("초당 자연 감소량입니다.")]
        [SerializeField, Min(0f)] private float decayPerSecond = 3f;
        [Tooltip("최대 게이지에서 증가하는 일반 공격 피해 비율입니다. 0.5는 50% 증가입니다.")]
        [SerializeField, Min(0f)] private float maxAttackBonus = 0.5f;
        [Tooltip("런타임 게이지 확인용입니다. 시작 시 0으로 초기화합니다.")]
        [SerializeField] private float currentGauge;

        public float CurrentGauge => currentGauge;
        public float MaxGauge => maxGauge;
        public float NormalizedGauge => currentGauge / maxGauge;
        public float AttackMultiplier => 1f + NormalizedGauge * maxAttackBonus;

        private void Awake()
        { 
            currentGauge = 0f;
        }

        private void Update()
        {
            currentGauge = Mathf.Max(0f, currentGauge - decayPerSecond * Time.deltaTime);
        }

        public void GainOnParrySuccess()
        {
            currentGauge = Mathf.Min(maxGauge, currentGauge + gaugePerParry);
        }

        // 부족한 경우 일부만 소비하지 않고 실패한다.
        public bool TryConsume(float amount)
        {
            if (amount <= 0f || currentGauge < amount)
                return false;

            currentGauge -= amount;
            return true;
        }
    }
}
