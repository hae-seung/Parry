using Unity.Cinemachine;
using UnityEngine;

namespace Parry.CameraClass
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CinemachineImpulseSource))]
    public sealed class CameraFeedback : Singleton<CameraFeedback>
    {
        [SerializeField] private float impulseIntensityWithPlayerHit = 1.0f;
        [SerializeField] private float impulseIntensityWithPlayerAttack = 1.0f;
        private CinemachineImpulseSource impulseSource;

        protected override void Awake()
        {
            base.Awake();
            impulseSource = GetComponent<CinemachineImpulseSource>();
        }

        //플레이어가 맞았을때
        public void ShakePlayerHit()
        {
            impulseSource.GenerateImpulseWithForce(impulseIntensityWithPlayerHit);
        }

        //플레이어가 공격할때
        public void ShakePlayerAttack()
        {
            impulseSource.GenerateImpulseWithForce(impulseIntensityWithPlayerAttack);
        }
    }
}
