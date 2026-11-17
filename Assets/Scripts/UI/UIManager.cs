using UnityEngine;
using Parry.PlayerClass;

namespace Parry.UIClass
{
    [DisallowMultipleComponent]
    public sealed class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [SerializeField] private PlayerStatUI playerStatUI;
        [SerializeField] private ParryUI parryUI;
        [SerializeField] private SkillUI skillUI;

        private void Awake()
        {
            Instance = this;
        }

        public void Init(PlayerCharacter character)
        {
            if (playerStatUI != null)
                playerStatUI.Init(character);
            
            if (parryUI != null)
                parryUI.Init(character);
            if (skillUI != null)
                skillUI.Init(character);
        }

        private void OnDestroy()
        {
            Instance = null;
        }
        
    }
}
