using UnityEngine;
using Parry.MonsterClass;

namespace Parry.BossClass
{
    public sealed class BossChaseState : IMonsterState
    {
        private readonly BossBrain brain;
        private readonly MonsterChaseSettings settings;

        public BossChaseState(BossBrain brain, MonsterChaseSettings settings)
        {
            this.brain = brain;
            this.settings = settings;
        }

        public void Enter() => Tick();

        public void Tick()
        {
            if (brain.ShouldEndCombat())
            {
                brain.ChangeState(brain.WanderState);
                return;
            }

            if (brain.TryStartAttack())
                return;

            brain.Movement.Face(brain.LastSeenPosition.x);
            
            float distance = brain.LastSeenPosition.x - brain.Movement.Position.x;
            
            if (Mathf.Abs(distance) <= settings.stopDistance)
                brain.Movement.Stop();
            else
                brain.Movement.MoveTo(brain.LastSeenPosition.x - Mathf.Sign(distance) * settings.stopDistance);
        }

        public void Exit()
        {
            brain.Movement.Stop();
        }
    }
}
