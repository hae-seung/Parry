using Parry.MonsterClass;

namespace Parry.BossClass
{
    public sealed class BossDeadState : IMonsterState
    {
        private readonly BossBrain brain;

        public BossDeadState(BossBrain brain) => this.brain = brain;

        public void Enter()
        {
            brain.Attack.Stop();
            brain.Movement.Stop();
            brain.Boss.PlayDeathAnimation();
        }

        public void Tick() { }
        public void Exit() { }
    }
}
