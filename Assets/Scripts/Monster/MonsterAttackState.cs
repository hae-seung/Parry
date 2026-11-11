using UnityEngine;

namespace Parry.MonsterClass
{
    public sealed class MonsterAttackState : IMonsterState
    {
        private readonly MonsterBrain brain;
        private bool isWaitingForAttackEndDelay;
        private float attackEndDelayEndTime;

        public MonsterAttackState(MonsterBrain brain) => this.brain = brain;

        public void Enter()
        {
            // 시작할 때 정한 방향을 유지하며 모션과 후딜레이 동안 정지한다.
            brain.Movement.Stop();
            isWaitingForAttackEndDelay = false;
        }

        public void Tick()
        {
            // 모션이 끝나면 기본 Animator로 돌아가 후딜레이 종료를 기다린다.
            if (!isWaitingForAttackEndDelay)
            {
                //애니메이션 재생중이면 return
                if (brain.Attack.IsMotionPlaying)
                    return;

                //애니메이션이 종료되면 후딜레이 적용
                isWaitingForAttackEndDelay = true;
                attackEndDelayEndTime = Time.time + brain.Attack.CurrentAttack.AttackEndDelayDuration;
            }

            //후딜중
            if (Time.time < attackEndDelayEndTime)
                return;

            // 후딜레이까지 끝난 뒤 전투 종료 여부를 판단한다.
            if (brain.ShouldEndCombat())
                brain.ChangeState(brain.WanderState);
            else
                brain.ChangeState(brain.ChaseState);
        }

        public void Exit() => brain.Attack.Stop();
    }
}
