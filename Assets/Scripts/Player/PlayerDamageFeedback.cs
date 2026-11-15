using UnityEngine;
using Parry.CameraClass;

namespace Parry.PlayerClass
{
    [DisallowMultipleComponent]
    public sealed class PlayerDamageFeedback : MonoBehaviour
    {
        public void PlayHitFeedback()
        {
            CameraFeedback.Instance.ShakePlayerHit();
        }
    }
}
