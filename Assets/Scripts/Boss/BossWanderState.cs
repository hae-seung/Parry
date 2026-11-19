using Parry.MonsterClass;
using UnityEngine;

namespace Parry.BossClass
{
    public sealed class BossWanderState : IMonsterState
    {
        private readonly BossBrain brain;
        private readonly MonsterWanderSettings settings;
        private float centerX;
        private float destinationX;
        private float waitUntil;
        private float travelDeadline;
        private bool walking;

        public BossWanderState(BossBrain brain, MonsterWanderSettings settings)
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
            if (brain.IsTargetVisible)
            {
                brain.ChangeState(brain.ChaseState);
                return;
            }

            if (walking)
            {
                if (ShouldStopWalking())
                    Wait();
                
                return;
            }

            if (Time.time < waitUntil)
                return;

            destinationX = centerX + Random.Range(-settings.radius, settings.radius);
            if (!CanStartWalking())
            {
                Wait();
                
                return;
            }

            walking = true;
            travelDeadline = Time.time + settings.travelTimeout;
            brain.Movement.MoveTo(destinationX);
        }

        public void Exit()
        {
            brain.Movement.Stop();
        }
        
        
        
        private bool HasArrived()
        {
            return Mathf.Abs(destinationX - brain.Movement.Position.x) <= settings.arrivalDistance;
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

       
    }
}
