using System;
using System.Collections.Generic;
using Parry.MonsterClass;
using Parry.UIClass;
using UnityEngine;

namespace Parry.BossClass
{
    [RequireComponent(typeof(Boss), 
        typeof(MonsterMovement), 
        typeof(MonsterSensor))]
    [RequireComponent(typeof(BossAttack))]
    public sealed class BossBrain : Brain
    {
        [SerializeField] private List<BossPhaseData> phases = new List<BossPhaseData>();
        private int phaseIndex;
        private BossPhaseTransitionState phaseTransitionState;
        private MonsterSensor sensor;
        private MonsterCombatPolicy combatPolicy;
        private BossDialogueUI dialogueUI;
        private BossStatUI statUI;

        public BossPhaseData Data => phases[phaseIndex];
        public Boss Boss { get; private set; }
        public MonsterMovement Movement { get; private set; }
        public BossAttack Attack { get; private set; }
        public BossAttackState AttackState { get; private set; }
        public BossWanderState WanderState { get; private set; }
        public BossChaseState ChaseState { get; private set; }
        public BossDeadState DeadState { get; private set; }
        public BossFlinchState FlinchState { get; private set; }
        public bool IsTargetVisible => sensor.IsTargetVisible && sensor.HasTarget;
        public bool IsPhaseTransitioning => State == phaseTransitionState;
        public Vector2 LastSeenPosition => sensor.LastSeenPosition;

        public bool ShouldEndCombat() => combatPolicy.ShouldEndCombat();

        private void Awake()
        {
            phaseIndex = 0;
            
            Boss = GetComponent<Boss>();
            Movement = GetComponent<MonsterMovement>();
            sensor = GetComponent<MonsterSensor>();
            Attack = GetComponent<BossAttack>();
            dialogueUI = GetComponentInChildren<BossDialogueUI>(true);
            dialogueUI.OnDialogueCompleted += OnDialogueCompleted;
            statUI = GetComponentInChildren<BossStatUI>(true);
            statUI.OnHealthFillCompleted += OnHealthFillCompleted;
            
            AttackState = new BossAttackState(this);
            DeadState = new BossDeadState(this);
            phaseTransitionState = new BossPhaseTransitionState(this);
            combatPolicy = new MonsterCombatPolicy(Data.Combat, sensor, Movement);
            ChaseState = new BossChaseState(this, Data.Chase);
            WanderState = new BossWanderState(this, Data.Wander);
            FlinchState = new BossFlinchState(this, Data.Flinch);
        }

        private void OnEnable()
        {
            phaseIndex = 0;
            InitComponents();
            ChangeState(WanderState);
            Boss.InitUI();
        }
        
        protected override void OnDisable()
        {
            base.OnDisable();
            dialogueUI.Stop();
            Movement.Stop();
        }

        private void OnDestroy()
        {
            if (dialogueUI != null)
                dialogueUI.OnDialogueCompleted -= OnDialogueCompleted;
            if (statUI != null)
                statUI.OnHealthFillCompleted -= OnHealthFillCompleted;
        }

        private void InitComponents()
        {
            MonsterBaseStats stats = Data.BaseStats;
            Boss.Initialize(stats, Data.Flinch, Data.Settings);
            Movement.Initialize(stats.moveSpeed, Data.GroundLayers);
            sensor.Initialize(Data.Detection);
            Attack.Initialize(Data.Attacks, Data.TargetLayers);
        }

        public void EnterNextPhaseOrDeadState()
        {
            ChangeState(phaseTransitionState);
        }

        public void StartDialogue()
        {
            dialogueUI.Show(Data.Dialogues);
        }

        private void OnDialogueCompleted()
        {
            if (State != phaseTransitionState)
                return;

            bool hasNextPhase = phaseIndex + 1 < phases.Count;

            if (hasNextPhase)
            {
                phaseIndex += 1;
                InitComponents();
                Boss.InitUI();
                statUI.FillHealth();
            }
            else
            {
                ChangeState(DeadState);
            }
        }

        private void OnHealthFillCompleted()
        {
            if (IsPhaseTransitioning)
                phaseTransitionState.StartWakeUp();
        }

        public void PhaseTransitionComplete() => ChangeState(WanderState);
        
        public bool TryStartAttack()
        {
            if (!IsTargetVisible)
                return false;

            Vector2 delta = (Vector2)sensor.Target.transform.position - Movement.Position;
            
            Movement.Face(sensor.Target.transform.position.x);

            if (!Attack.TryStartAttack(delta.sqrMagnitude))
                return false;

            ChangeState(AttackState);
            return true;
        }
    }
}
