using UnityEngine;
using Parry.AnimationClass;

namespace Parry.MonsterClass
{
    public sealed class MonsterFlinchState : IMonsterState
    {
        private readonly MonsterBrain brain;
        private readonly AnimationClipPlayer clipPlayer;
        private readonly MonsterFlinchData data;
        private float endTime;

        public MonsterFlinchState(MonsterBrain brain, MonsterFlinchData data)
        {
            this.brain = brain;
            this.data = data;
            clipPlayer = brain.GetComponent<AnimationClipPlayer>();
        }

        public void Enter()
        {
            brain.Attack.Stop();
            brain.Movement.Stop();
            clipPlayer.Play(data.clip, true);
            endTime = Time.time + data.duration;
        }

        public void Tick()
        {
            if (Time.time >= endTime)
            {
                brain.Monster.ResetFlinchResistance();
                brain.ChangeState(brain.WanderState);
            }
        }

        public void Exit() => clipPlayer.Stop();
    }
}
