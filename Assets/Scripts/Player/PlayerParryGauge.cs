using System;
using UnityEngine;

namespace Parry.PlayerClass
{
    [DisallowMultipleComponent]
    public sealed class PlayerParryGauge : MonoBehaviour
    {
        private PlayerParryGaugeData data;
        [SerializeField] private float currentGauge;

        public event Action<float> OnGaugeChanged;

        public float CurrentGauge => currentGauge;
        public float MaxGauge => data.maxGauge;
        public float NormalizedGauge => currentGauge / data.maxGauge;
        public float AttackMultiplier => 1f + NormalizedGauge * data.maxAttackBonus;

        public void Initialize(PlayerParryGaugeData data)
        {
            this.data = data;
            currentGauge = data.initialGauge;
        }

        private void Update()
        {
            float nextGauge = Mathf.Max(0f, currentGauge - data.decayPerSecond * Time.deltaTime);
            if (nextGauge == currentGauge)
                return;

            currentGauge = nextGauge;
            OnGaugeChanged?.Invoke(NormalizedGauge);
        }


        public void InitUI()
        {
            OnGaugeChanged?.Invoke(NormalizedGauge);
        }
        
        public void GainOnParrySuccess()
        {
            currentGauge = Mathf.Min(data.maxGauge, currentGauge + data.gaugePerParry);
            OnGaugeChanged?.Invoke(NormalizedGauge);
        }

        // 부족한 경우 일부만 소비하지 않고 실패한다.
        public bool TryConsume(float amount)
        {
            if (amount <= 0f || currentGauge < amount)
                return false;

            currentGauge -= amount;
            OnGaugeChanged?.Invoke(NormalizedGauge);
            return true;
        }
    }
}
