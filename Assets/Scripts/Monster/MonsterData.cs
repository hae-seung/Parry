using System;
using System.Collections.Generic;
using UnityEngine;

namespace Parry.MonsterClass
{
    [Serializable]
    public struct MonsterDetectionSettings
    {
        [Tooltip("시야를 가리는 장애물과 이동 시 벽·지면을 검사할 레이어입니다. Trigger는 검사에서 제외합니다.")]
        public LayerMask obstacleLayers;
        [Tooltip("플레이어를 감지할 수 있는 최대 2D 거리입니다. 월드 단위입니다.")]
        [Min(0f)] public float distance;
        [Tooltip("다른 높이의 발판에 있는 플레이어를 감지 못하도록 하기 위함")]
        //RayCast로 몬스터와 플레이어 사이의 벽은 감지하는데 다른 높이이지만 범위 내의 플레이어가 발판 끝에 있으면
        //닿지는 못하는데 계속 감지를 하게 될거임. 이를 방지함
        [Min(0f)] public float heightTolerance;
    }

    [Serializable]
    public struct MonsterChaseSettings
    {
        [Tooltip("마지막으로 목격한 플레이어 위치와의 수평 거리가 이 값 이하면 추적 이동을 멈춥니다. 월드 단위입니다.")]
        [Min(0f)] public float stopDistance;
    }

    [Serializable]
    public struct MonsterCombatSettings
    {
        [Tooltip("실제 플레이어와의 2D 거리가 이 값을 넘으면 전투 종료로 판단합니다. 공격 중에는 후딜레이 종료 후 적용합니다.")]
        [Min(0f)] public float combatExitDistance;
        [Tooltip("플레이어를 연속으로 놓쳐도 전투를 유지하는 시간(초)입니다. 다시 감지하면 놓친 시간이 초기화됩니다.")]
        [Min(0f)] public float sightLossDuration;
    }

    [Serializable]
    public struct MonsterWanderSettings
    {
        [Tooltip("배회 상태에 진입한 위치를 중심으로 좌우 목적지를 고르는 반경입니다. 월드 단위입니다.")]
        [Min(0f)] public float radius;
        [Tooltip("배회 시작 시와 한 번의 이동을 마친 뒤 다음 목적지를 고르기까지 기다리는 시간(초)입니다.")]
        [Min(0f)] public float waitDuration;
        [Tooltip("하나의 배회 목적지로 이동할 수 있는 최대 시간(초)입니다. 시간이 지나면 멈추고 대기합니다.")]
        [Min(0.1f)] public float travelTimeout;
        [Tooltip("목적지와의 수평 거리가 이 값 이하면 도착한 것으로 판단합니다. 월드 단위입니다.")]
        [Min(0.01f)] public float arrivalDistance;
    }

    
    
    
    
    [CreateAssetMenu(fileName = "MonsterData", menuName = "Parry/Monster Data")]
    public sealed class MonsterData : ScriptableObject
    {
        [Tooltip("추적과 배회에 사용하는 최대 수평 이동 속도입니다. 초당 월드 단위입니다.")]
        [SerializeField, Min(0f)] private float moveSpeed = 2f;
        [Tooltip("사용할 공격 데이터 목록입니다. 거리 조건과 쿨타임을 만족하는 공격 중 하나를 무작위로 선택합니다.")]
        [SerializeField] private List<MonsterAttackData> attacks = new List<MonsterAttackData>();
        [Tooltip("플레이어 감지 거리·높이와 장애물 검사 설정입니다.")]
        [SerializeField] private MonsterDetectionSettings detection = new MonsterDetectionSettings
        {
            obstacleLayers = 1, distance = 6f, heightTolerance = 0.5f
        };
        [Tooltip("플레이어의 마지막 목격 위치를 추적할 때 사용하는 설정입니다.")]
        [SerializeField] private MonsterChaseSettings chase = new MonsterChaseSettings { stopDistance = 1.2f };
        [Tooltip("거리 초과 또는 지속적인 감지 실패로 전투를 종료하는 기준입니다.")]
        [SerializeField] private MonsterCombatSettings combat = new MonsterCombatSettings
        {
            combatExitDistance = 9f, sightLossDuration = 3f
        };
        [Tooltip("배회 진입 위치 주변에서 이동과 대기를 반복하는 설정입니다.")]
        [SerializeField] private MonsterWanderSettings wander = new MonsterWanderSettings
        {
            radius = 2f, waitDuration = 1f, travelTimeout = 3f, arrivalDistance = 0.05f
        };

        public float MoveSpeed => moveSpeed;
        public List<MonsterAttackData> Attacks => attacks;
        public MonsterDetectionSettings Detection => detection;
        public MonsterChaseSettings Chase => chase;
        public MonsterCombatSettings Combat => combat;
        public MonsterWanderSettings Wander => wander;
    }
}
