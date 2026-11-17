using UnityEngine;
using UnityEngine.UI;

namespace Parry.UIClass
{
    public sealed class ParryGaugeUI : MonoBehaviour
    {
        [SerializeField] private Slider slider;
        [SerializeField] private Image gaugeMark;
        [SerializeField] private Image gaugeGlow;

        private float readyThreshold;
        
        public void SetGaugeMark(float gaugeCost, float maxGauge)
        {
            readyThreshold = gaugeCost / maxGauge;
            
            RectTransform mark = gaugeMark.rectTransform;
            mark.anchorMin = new Vector2(0.5f, Mathf.Clamp01(readyThreshold));
            mark.anchorMax = mark.anchorMin;
            mark.anchoredPosition = Vector2.zero;
            
            gaugeMark.gameObject.SetActive(readyThreshold <= 1f);
        }

        public void SetValue(float normalizedGauge)
        {
            slider.SetValueWithoutNotify(normalizedGauge);
            gaugeGlow.gameObject.SetActive(normalizedGauge >= readyThreshold);
        }
    }
}
