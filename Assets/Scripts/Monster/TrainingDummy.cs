using UnityEngine;

namespace Parry.MonsterClass
{
    [RequireComponent(typeof(Monster))]
    public sealed class TrainingDummy : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float maxHp = 1000000f;

        private void OnEnable() => ResetHealth();

        // Inspector의 컴포넌트 메뉴에서도 테스트 체력을 초기화할 수 있다.
        [ContextMenu("Reset Health")]
        private void ResetHealth()
        {
            GetComponent<Monster>().Initialize(new MonsterBaseStats { maxHp = maxHp });
        }
    }
}
