using System;
using UnityEngine;
using Parry.PlayerClass;
using UnityEngine.UI;

namespace Parry.UIClass
{
    public sealed class ParryUI : MonoBehaviour
    {
        [SerializeField] private ParryGaugeUI parryGaugeUI;
        [SerializeField] private BerserkerGaugeUI berserkerGaugeUI;

        private PlayerParryGauge gauge;
        private PlayerBerseker berseker;


        private void Awake()
        {
            parryGaugeUI.gameObject.SetActive(true);
            berserkerGaugeUI.gameObject.SetActive(false);
        }

        public void Init(PlayerCharacter character)
        {
            gauge = character.ParryGauge;
            berseker = character.PlayerBerserker;
            
            parryGaugeUI.SetGaugeMark(berseker.GaugeCost, gauge.MaxGauge);
            
            gauge.OnGaugeChanged += UpdateParryGauge;
            berseker.OnRemainingTimeChanged += UpdateBerserkerGauge;
            berseker.EnterBerserker += EnterBerserker;
            berseker.ExitBerserker += ExitBerserker;

            gauge.InitUI();
            berseker.InitUI();
        }

        private void OnDestroy() => Unbind();

        private void Unbind()
        {
            if (gauge != null)
                gauge.OnGaugeChanged -= UpdateParryGauge;
            if (berseker != null)
            {
                berseker.EnterBerserker -= EnterBerserker;
                berseker.ExitBerserker -= ExitBerserker;
                berseker.OnRemainingTimeChanged -= UpdateBerserkerGauge;
            }
            gauge = null;
            berseker = null;
        }

        public void UpdateParryGauge(float normalizedGauge)
        {
            parryGaugeUI.SetValue(normalizedGauge);
        }

        public void UpdateBerserkerGauge(float normalizedTime)
        {
            berserkerGaugeUI.SetValue(normalizedTime);
        }

        public void EnterBerserker()
        {
            parryGaugeUI.gameObject.SetActive(false);
            berserkerGaugeUI.gameObject.SetActive(true);
        }

        public void ExitBerserker()
        {
            parryGaugeUI.gameObject.SetActive(true);
            berserkerGaugeUI.gameObject.SetActive(false);
        }
    }
}
