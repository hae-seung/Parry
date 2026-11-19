using System;
using UnityEngine;

namespace Parry.UIClass
{
    public sealed class MonsterHealthUI : MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.Slider healthSlider;
        [SerializeField] private UnityEngine.UI.Slider whiteHealthSlider;
        [Tooltip("피격 후 흰색 체력바가 감소하기까지의 대기시간입니다.")]
        [SerializeField, Min(0f)] private float whiteHealthDelay = 0.2f;
        [Tooltip("초당 감소할 흰색 체력바 비율입니다. 1이면 전체 체력바 길이를 1초에 줄입니다.")]
        [SerializeField, Min(0.01f)] private float whiteHealthSpeed = 0.8f;
        [Tooltip("체력 충전 연출 시 초당 채울 체력바 비율입니다.")]
        [SerializeField, Min(0.01f)] private float healthFillSpeed = 1f;
        
        
        private float whiteHealthStartTime;
        private float healthFillTarget;
        private bool isFillingHealth;
        public event Action OnHealthFillCompleted;
        

        private void Update()
        {
            if (isFillingHealth)
            {
                healthSlider.SetValueWithoutNotify(Mathf.MoveTowards(
                    healthSlider.value, healthFillTarget, healthFillSpeed * Time.deltaTime));
                
                whiteHealthSlider.SetValueWithoutNotify(healthSlider.value);
                
                isFillingHealth = healthSlider.value < healthFillTarget;
                if (!isFillingHealth)
                    OnHealthFillCompleted?.Invoke();
                return;
            }

            if (Time.time < whiteHealthStartTime || whiteHealthSlider.value <= healthSlider.value)
                return;

            whiteHealthSlider.SetValueWithoutNotify(Mathf.MoveTowards(
                whiteHealthSlider.value, healthSlider.value, whiteHealthSpeed * Time.deltaTime));
        }
        
        
        public void SetHealth(float current, float max)
        {
            isFillingHealth = false;
            float value = current / max;
            
            whiteHealthStartTime = Time.time + whiteHealthDelay;
            healthSlider.SetValueWithoutNotify(value);

            // 체력 회복되었을 때
            if (value > whiteHealthSlider.value)
                whiteHealthSlider.SetValueWithoutNotify(value);
        }

        public void FillHealth(float current, float max)
        {
            healthFillTarget = current / max;
            healthSlider.SetValueWithoutNotify(0f);
            whiteHealthSlider.SetValueWithoutNotify(0f);
            isFillingHealth = true;
        }

        private void OnDisable() => isFillingHealth = false;

        public void ResetWhiteHealth() => whiteHealthSlider.SetValueWithoutNotify(healthSlider.value);
    }
}
