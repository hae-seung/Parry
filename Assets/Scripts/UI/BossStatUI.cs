using System;
using Parry.BossClass;
using TMPro;
using UnityEngine;

namespace Parry.UIClass
{
    public sealed class BossStatUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private MonsterHealthUI healthUI;
        [SerializeField] private MonsterFlinchUI flinchUI;
        [SerializeField] private MonsterExecutionUI executionUI;
        private Boss boss;
        public event Action OnHealthFillCompleted;

        public void Init(Boss target)
        {
            boss = target;
            boss.OnNameChanged += SetName;
            boss.OnHpChanged += healthUI.SetHealth;
            boss.OnFlinchResistanceChanged += flinchUI.SetResistance;
            boss.OnStoredDamageChanged += executionUI.SetValue;
            boss.OnDeath += Hide;
            healthUI.OnHealthFillCompleted += HealthFillCompleted;

            boss.InitUI();
            healthUI.ResetWhiteHealth();
        }

        private void SetName(string value)
        {
            gameObject.SetActive(true);
            nameText.text = value;
        }

        public void FillHealth() => healthUI.FillHealth(boss.Hp, boss.MaxHp);

        private void HealthFillCompleted() => OnHealthFillCompleted?.Invoke();

        private void Hide() => gameObject.SetActive(false);

        private void OnDestroy()
        {
            if (boss == null)
                return;

            boss.OnNameChanged -= SetName;
            boss.OnHpChanged -= healthUI.SetHealth;
            boss.OnFlinchResistanceChanged -= flinchUI.SetResistance;
            boss.OnStoredDamageChanged -= executionUI.SetValue;
            boss.OnDeath -= Hide;
            healthUI.OnHealthFillCompleted -= HealthFillCompleted;
        }
    }
}
