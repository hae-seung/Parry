using Parry.BossClass;
using UnityEngine;

namespace Parry.UIClass
{
    public sealed class BossPostureUI : MonoBehaviour
    {
        [SerializeField] private Boss boss;
        [SerializeField] private MonsterFlinchUI flinchUI;

        private void Start() => Init(boss);

        public void Init(Boss target)
        {
            boss = target;
            boss.OnFlinchResistanceChanged += flinchUI.SetResistance;
            boss.OnDeath += Hide;
            boss.InitUI();
        }

        private void Hide() => gameObject.SetActive(false);

        private void OnDestroy()
        {
            if (boss == null)
                return;
            boss.OnFlinchResistanceChanged -= flinchUI.SetResistance;
            boss.OnDeath -= Hide;
        }
    }
}
