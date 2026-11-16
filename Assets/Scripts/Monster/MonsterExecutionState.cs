namespace Parry.MonsterClass
{
    public sealed class MonsterExecutionState : IMonsterState
    {
        private readonly MonsterBrain brain;

        public MonsterExecutionState(MonsterBrain brain) => this.brain = brain;

        public void Enter()
        {
            brain.Attack.Stop();
            brain.Movement.Stop();
            brain.Monster.PlayExecutionAnimation();
        }

        public void Tick() { }
        public void Exit() { }
    }
}
