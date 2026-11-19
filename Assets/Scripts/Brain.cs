using Parry.MonsterClass;
using UnityEngine;

namespace Parry
{
    public abstract class Brain : MonoBehaviour
    {
        private readonly MonsterStateMachine stateMachine = new MonsterStateMachine();

        public IMonsterState State => stateMachine.CurrentState;

        protected virtual void Update()
        {
            if (Time.timeScale == 0f)
                return;

            stateMachine.Tick();
        }

        protected virtual void OnDisable() => stateMachine.Clear();

        public void ChangeState(IMonsterState nextState) => stateMachine.ChangeState(nextState);
    }
}
