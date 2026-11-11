namespace Parry.MonsterClass
{
    public sealed class MonsterStateMachine
    {
        public IMonsterState CurrentState { get; private set; }

        public void ChangeState(IMonsterState nextState)
        {
            if (CurrentState == nextState)
                return;

            CurrentState?.Exit();
            CurrentState = nextState;
            CurrentState?.Enter();
        }

        public void Tick() => CurrentState?.Tick();

        public void Clear() => ChangeState(null);
    }
}
