using UnityEngine;

namespace Parry.MonsterClass
{
    [RequireComponent(typeof(Monster), 
        typeof(MonsterMovement), 
        typeof(MonsterSensor))]
    [RequireComponent(typeof(MonsterAttack))]
    public sealed class MonsterBrain : MonoBehaviour
    {
        private readonly MonsterStateMachine stateMachine = new MonsterStateMachine();

        private Monster monster;
        private MonsterMovement movement;
        private MonsterSensor sensor;
        private MonsterAttack attack;

        [SerializeField] private MonsterData data;
        private MonsterCombatPolicy combatPolicy;

        public IMonsterState State => stateMachine.CurrentState;
        public MonsterData Data => data;
        public MonsterChaseState ChaseState { get; private set; }
        public MonsterWanderState WanderState { get; private set; }
        public MonsterAttackState AttackState { get; private set; }
        public MonsterExecutionState ExecutionState { get; private set; }
        public MonsterDeadState DeadState { get; private set; }
        public MonsterFlinchState FlinchState { get; private set; }
        public Monster Monster => monster;
        public MonsterAttack Attack => attack;
        public MonsterMovement Movement => movement;
        public bool IsTargetVisible => sensor.IsTargetVisible && sensor.HasTarget;
        public Vector2 LastSeenPosition => sensor.LastSeenPosition;

        private void Awake()
        {
            monster = GetComponent<Monster>();
            movement = GetComponent<MonsterMovement>();
            sensor = GetComponent<MonsterSensor>();
            attack = GetComponent<MonsterAttack>();
            
            if (data == null)
            {
                Debug.LogError("MonsterBrain requires a MonsterData asset.", this);
                enabled = false;
                return;
            }
            
            // 공유 데이터의 기본 스탯을 각 몬스터의 런타임 상태에 적용한다.
            MonsterBaseStats stats = data.BaseStats;
            monster.Initialize(stats, data.Flinch);
            movement.Initialize(stats.moveSpeed, data.Detection.obstacleLayers);
            sensor.Initialize(data.Detection);
            attack.Initialize(data.Attacks);
            
            ChaseState = new MonsterChaseState(this, data.Chase);
            WanderState = new MonsterWanderState(this, data.Wander);
            AttackState = new MonsterAttackState(this);
            ExecutionState = new MonsterExecutionState(this);
            DeadState = new MonsterDeadState(this);
            FlinchState = new MonsterFlinchState(this, data.Flinch);
            combatPolicy = new MonsterCombatPolicy(data.Combat, sensor, movement);
        }

        private void OnEnable()
        {
            stateMachine.ChangeState(WanderState);
        }

        private void Update()
        {
            if (Time.timeScale == 0f)
                return;

            stateMachine.Tick();
        }

        private void OnDisable()
        {
            stateMachine.Clear();
            movement.Stop();
        }

        public void ChangeState(IMonsterState nextState)
        {
            stateMachine.ChangeState(nextState);
        }

        public bool ShouldEndCombat() => combatPolicy.ShouldEndCombat();

        public bool TryStartAttack()
        {
            // 보이는 실제 대상과의 2D 거리로 공격을 선택한다.
            if (!IsTargetVisible)
                return false;

            Vector2 delta = (Vector2)sensor.Target.transform.position - movement.Position;
            movement.Face(sensor.Target.transform.position.x);
            
            if (!attack.TryStartAttack(delta.sqrMagnitude))
                return false;

            ChangeState(AttackState);
            return true;
        }
    }
}
