using System;
using UnityEngine;

namespace Parry.BossClass
{
    [Serializable]
    public struct BossFlinchData
    {
        public AnimationClip faintClip;
        public AnimationClip wakeUpClip;
        [Tooltip("쓰러지는 모션이 끝난 뒤 마지막 자세를 유지할 시간입니다.")]
        [Min(0f)] public float duration;
        [Tooltip("패링 반격마다 1씩 감소하며 0이 되면 기절합니다.")]
        [Min(1)] public int resistance;
    }
}
