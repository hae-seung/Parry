using UnityEngine;

namespace Parry.MonsterClass
{
    public sealed class MonsterWanderState : IMonsterState
    {
        private readonly MonsterBrain brain;
        private readonly MonsterWanderSettings settings;
        private float centerX;
        private float destinationX;
        private float waitUntil;
        private float travelDeadline;
        private bool walking;

        public MonsterWanderState(MonsterBrain brain, MonsterWanderSettings settings)
        {
            this.brain = brain;
            this.settings = settings;
        }

        public void Enter()
        {
            centerX = brain.transform.position.x;
            Wait();
        }

        public void Tick()
        {
            // 플레이어를 발견하면 배회를 중단하고 추적한다.
            if (brain.IsTargetVisible)
            {
                brain.ChangeState(brain.ChaseState);
                return;
            }

            // 선택한 목적지로 이동 중인 상태.
            if (walking)
            {
                // 도착했거나, 이동 시간이 지났거나, 앞이 막히면 멈추고 대기한다.
                if (ShouldStopWalking())
                {
                    Wait();
                }
                
                return;
            }
            
            // 배회 중 정지 상태: 대기 시간이 남아 있으면 계속 기다린다.
            if (Time.time < waitUntil)
                return;
            
            
            // 대기가 끝났으므로 배회 중심의 반경 안에서 다음 목적지를 고른다.
            destinationX = centerX + Random.Range(-settings.radius, settings.radius);
            if (!CanStartWalking())
            {
                // 목적지가 너무 가깝거나 이동할 수 없는 방향이면 다시 대기한다.
                Wait();
                return;
            }

            // 이동을 시작하고, 목적지에 도착하지 못할 경우를 위한 제한 시간을 설정한다.
            walking = true;
            travelDeadline = Time.time + settings.travelTimeout;
            brain.Movement.MoveTo(destinationX);
        }

        private bool HasArrived()
        {
            float distance = Mathf.Abs(destinationX - brain.Movement.Position.x);
            
            return distance <= settings.arrivalDistance;
        }

        private bool ShouldStopWalking()
        {
            return HasArrived() || Time.time >= travelDeadline ||
                !brain.Movement.CanWalkTowards(destinationX);
        }

        private bool CanStartWalking()
        {
            return !HasArrived() && brain.Movement.CanWalkTowards(destinationX);
        }

        private void Wait()
        {
            walking = false;
            brain.Movement.Stop();
            waitUntil = Time.time + settings.waitDuration;
        }
        
        public void Exit() => brain.Movement.Stop();
    }
}
