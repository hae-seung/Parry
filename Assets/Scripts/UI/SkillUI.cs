using UnityEngine;
using Parry.PlayerClass;

namespace Parry.UIClass
{
    public sealed class SkillUI : MonoBehaviour
    {
        [SerializeField] private SkillIconUI parrySkill;
        [SerializeField] private SkillIconUI berserkerEnterSkill;
        [SerializeField] private SkillIconUI berserkerExitSkill;

        private PlayerParry parry;
        private PlayerBerseker berseker;
        private PlayerParryGauge gauge;
        private float requiredGaugeRatio;

        public void Init(PlayerCharacter character)
        {
            parry = character.PlayerParryComponent;
            berseker = character.PlayerBerserker;
            gauge = character.ParryGauge;
            
            requiredGaugeRatio = berseker.GaugeCost / gauge.MaxGauge;

            parry.OnCooldownChanged += parrySkill.SetCooldown;
            berseker.OnCooldownChanged += berserkerEnterSkill.SetCooldown;
            
            berseker.EnterBerserker += EnterBerserker;
            berseker.ExitBerserker += ExitBerserker;
            
            gauge.OnGaugeChanged += UpdateGaugeAvailability;

            berserkerExitSkill.SetCooldown(0f);
            berserkerExitSkill.SetResourceReady(true);
            
            parry.InitUI();
            berseker.InitUI();
            gauge.InitUI();

            if (berseker.IsBerserking)
                EnterBerserker();
            else
                ExitBerserker();
        }

        private void OnDestroy() => Unbind();

        private void Unbind()
        {
            if (parry != null)
                parry.OnCooldownChanged -= parrySkill.SetCooldown;
            
            if (berseker != null)
            {
                berseker.OnCooldownChanged -= berserkerEnterSkill.SetCooldown;
                berseker.EnterBerserker -= EnterBerserker;
                berseker.ExitBerserker -= ExitBerserker;
            }
            
            if (gauge != null)
                gauge.OnGaugeChanged -= UpdateGaugeAvailability;

            parry = null;
            berseker = null;
            gauge = null;
        }

        private void UpdateGaugeAvailability(float normalizedGauge)
        {
            berserkerEnterSkill.SetResourceReady(normalizedGauge >= requiredGaugeRatio);
        }

        private void EnterBerserker()
        {
            berserkerEnterSkill.gameObject.SetActive(false);
            berserkerExitSkill.gameObject.SetActive(true);
        }

        private void ExitBerserker()
        {
            berserkerEnterSkill.gameObject.SetActive(true);
            berserkerExitSkill.gameObject.SetActive(false);
        }
    }
}
