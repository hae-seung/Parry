using Parry.MonsterClass;
using UnityEngine;

namespace Parry.BossClass
{
    public sealed class BossAttackState : IMonsterState
    {
        private readonly BossBrain brain;
        private bool isWaitingForAttackEndDelay;
        private float attackEndDelayEndTime;

        public BossAttackState(BossBrain brain) => this.brain = brain;

        public void Enter()
        {
            brain.Movement.Stop();
            isWaitingForAttackEndDelay = false;
        }

        public void Tick()
        {
            if (!isWaitingForAttackEndDelay)
            {
                if (brain.Attack.IsMotionPlaying)
                    return;

                isWaitingForAttackEndDelay = true;
                attackEndDelayEndTime = Time.time + brain.Attack.CurrentAttack.AttackEndDelayDuration;
            }

            if (Time.time < attackEndDelayEndTime)
                return;

            brain.ChangeState(brain.ShouldEndCombat() ? brain.WanderState : brain.ChaseState);
        }

        public void Exit() => brain.Attack.Stop();
    }
}
