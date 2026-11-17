using UnityEngine;
using UnityEngine.UI;

namespace Parry.UIClass
{
    public sealed class BerserkerGaugeUI : MonoBehaviour
    {
        [SerializeField] private Slider slider;
        [SerializeField] private Image gaugeGlow;

        public void SetValue(float normalizedTime)
        {
            slider.SetValueWithoutNotify(normalizedTime);
            gaugeGlow.gameObject.SetActive(normalizedTime > 0f);
        }
    }
}
