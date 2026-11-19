using Parry.MonsterClass;
using Parry.AnimationClass;
using UnityEngine;

namespace Parry.BossClass
{
    public sealed class BossFlinchState : IMonsterState
    {
        private readonly BossBrain brain;
        private readonly AnimationClipPlayer clipPlayer;
        private readonly BossFlinchData data;
        private float endTime;
        private bool isWakingUp;

        public BossFlinchState(BossBrain brain, BossFlinchData data)
        {
            this.brain = brain;
            this.data = data;
            clipPlayer = brain.GetComponent<AnimationClipPlayer>();
        }

        public void Enter()
        {
            brain.Attack.Stop();
            brain.Movement.Stop();
            isWakingUp = false;
            
            endTime = Time.time + data.duration;
            clipPlayer.Play(data.faintClip, true);
        }

        public void Tick()
        {
            if (!isWakingUp)
            {
                if (Time.time < endTime)
                    return;

                isWakingUp = true;
                clipPlayer.Play(data.wakeUpClip);
            }

            if (clipPlayer.IsPlayingClip(data.wakeUpClip))
                return;

            brain.Boss.ResetFlinchResistance();
            brain.ChangeState(brain.WanderState);
        }

        public void Exit() => clipPlayer.Stop();
    }
}
