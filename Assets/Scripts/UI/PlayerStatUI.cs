using UnityEngine;
using Parry.PlayerClass;
using UnityEngine.UI;

namespace Parry.UIClass
{
    public sealed class PlayerStatUI : MonoBehaviour
    {
        [SerializeField] private Slider healthSlider;
        [SerializeField] private Slider staminaSlider;

        private PlayerStatComponent stats;

        public void Init(PlayerCharacter character)
        {
            Unbind();
            
            stats = character.Stats;
            stats.OnHpChanged += UpdateHealth;
            stats.OnStaminaChanged += UpdateStamina;
            
            stats.InitUI();
        }

        private void OnDestroy() => Unbind();

        private void Unbind()
        {
            if (stats == null)
                return;

            stats.OnHpChanged -= UpdateHealth;
            stats.OnStaminaChanged -= UpdateStamina;
            stats = null;
        }

        public void UpdateHealth(float current, float max)
        {
            //OnSliderChanged 호출없이 변경
            healthSlider.SetValueWithoutNotify(current / max);
        }

        public void UpdateStamina(float current, float max)
        {
            //OnSliderChanged 호출없이 변경
            staminaSlider.SetValueWithoutNotify(current / max);
        }
    }
}
