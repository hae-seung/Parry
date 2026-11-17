using UnityEngine;
using Parry.MonsterClass;

namespace Parry.UIClass
{
    public sealed class MonsterStatUI : MonoBehaviour
    {
        [SerializeField] private MonsterHealthUI healthUI;
        [SerializeField] private MonsterFlinchUI flinchUI;
        [SerializeField] private MonsterExecutionUI executionUI;
        private Monster monster;

        public void Init(Monster target)
        {
            monster = target;
            monster.OnHpChanged += healthUI.SetHealth;
            monster.OnFlinchResistanceChanged += flinchUI.SetResistance;
            monster.OnStoredDamageChanged += executionUI.SetValue;
            
            monster.InitUI();
            healthUI.ResetWhiteHealth();
        }

        private void OnDestroy()
        {
            if (monster == null)
                return;

            monster.OnHpChanged -= healthUI.SetHealth;
            monster.OnFlinchResistanceChanged -= flinchUI.SetResistance;
            monster.OnStoredDamageChanged -= executionUI.SetValue;
        }
    }
}
