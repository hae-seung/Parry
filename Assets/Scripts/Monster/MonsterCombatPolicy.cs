using UnityEngine;

namespace Parry.MonsterClass
{
    public sealed class MonsterCombatPolicy
    {
        private readonly MonsterCombatSettings settings;
        private readonly MonsterSensor sensor;
        private readonly MonsterMovement movement;

        public MonsterCombatPolicy(MonsterCombatSettings settings, MonsterSensor sensor, MonsterMovement movement)
        {
            this.settings = settings;
            this.sensor = sensor;
            this.movement = movement;
        }

        public bool ShouldEndCombat()
        {
            if (!sensor.HasTarget)
                return true;

            //플레이어 - 몬스터 거리
            Vector2 delta = (Vector2)sensor.Target.transform.position - movement.Position;
            
            /*거리가 전투종료거리 이상 멀어지거나
             || 플레이어가 거리내에 있지만 보이지가 않으면서 동시에 시야에서 놓친 시간이 임계값을 넘으면 전투종료 판정*/
            return delta.sqrMagnitude > settings.combatExitDistance * settings.combatExitDistance ||
                (!sensor.IsTargetVisible && Time.time - sensor.LastSeenTime >= settings.sightLossDuration);
        }
    }
}
