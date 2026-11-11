using UnityEngine;

namespace Parry.MonsterClass
{
    public sealed class MonsterChaseState : IMonsterState
    {
        private readonly MonsterBrain brain;
        private readonly MonsterChaseSettings settings;

        public MonsterChaseState(MonsterBrain brain, MonsterChaseSettings settings)
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

            // 공격 가능한 거리에서는 추적을 멈추고 공격 상태로 전환한다.
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
