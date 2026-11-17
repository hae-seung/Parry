using System.Globalization;
using TMPro;
using UnityEngine;

namespace Parry.UIClass
{
    public sealed class SkillIconUI : MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.Image blindImage;
        [SerializeField] private TMP_Text cooldownText;

        private float nextAvailableTime;
        private bool resourceReady = true;
        private int displayedTime = -1;
        private bool displayedTenths;

        
        private void Update()
        {
            if (nextAvailableTime > 0f)
                Refresh();
        }
        
        public void SetCooldown(float availableTime)
        {
            nextAvailableTime = availableTime;
            Refresh();
        }

        public void SetResourceReady(bool ready)
        {
            resourceReady = ready;
            Refresh();
        }
        

        private void Refresh()
        {
            float remaining = Mathf.Max(0f, nextAvailableTime - Time.time);
            bool coolingDown = remaining > 0f;
            
            blindImage.gameObject.SetActive(coolingDown || !resourceReady);
            cooldownText.gameObject.SetActive(coolingDown);

            if (!coolingDown)
            {
                nextAvailableTime = 0f;
                displayedTime = -1;
                return;
            }

            bool tenths = remaining <= 1f;
            int time = Mathf.CeilToInt(remaining * (tenths ? 10f : 1f));
            if (time == displayedTime && tenths == displayedTenths)
                return;

            displayedTime = time;
            displayedTenths = tenths;
            cooldownText.text = tenths
                ? (time * 0.1f).ToString("F1", CultureInfo.InvariantCulture)
                : time.ToString(CultureInfo.InvariantCulture);
        }
    }
}
