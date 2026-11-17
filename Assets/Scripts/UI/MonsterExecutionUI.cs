using UnityEngine;
using UnityEngine.UI;

namespace Parry.UIClass
{
    public sealed class MonsterExecutionUI : MonoBehaviour
    {
        [SerializeField] private Image fillImage;

        public void SetValue(float storedDamage, float remainingHp)
        {
            gameObject.SetActive(storedDamage > 0f);

            if (storedDamage <= 0f)
            {
                fillImage.fillAmount = 0f;
                return;
            }

            // 처형 피해로 체력이 0이 된 경우에는 가득 찬 상태로 표시한다.
            fillImage.fillAmount = remainingHp > 0f ? storedDamage / remainingHp : 1f;
        }
    }
}
