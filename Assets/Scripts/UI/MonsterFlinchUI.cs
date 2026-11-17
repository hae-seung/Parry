using UnityEngine;
using UnityEngine.UI;

namespace Parry.UIClass
{
    public sealed class MonsterFlinchUI : MonoBehaviour
    {
        [SerializeField] private Slider slider;

        public void SetResistance(int current, int max)
        {
            // 기절 저항이 없는 트레이닝 더미는 빈 게이지로 표시한다.
            float value = (float)current / max;
            slider.SetValueWithoutNotify(value);
        }
    }
}
