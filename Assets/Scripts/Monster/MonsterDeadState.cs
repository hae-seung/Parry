namespace Parry.MonsterClass
{
    public sealed class MonsterDeadState : IMonsterState
    {
        private readonly MonsterBrain brain;

        public MonsterDeadState(MonsterBrain brain) => this.brain = brain;

        public void Enter()
        {
            brain.Attack.Stop();
            brain.Movement.Stop();
            brain.Monster.PlayDeathAnimation();
        }

        public void Tick() { }
        public void Exit() { }
    }
}
