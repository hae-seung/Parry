using Parry.CombatClass;
using Parry.UIClass;
using Parry.AnimationClass;
using Parry.PlayerClass;
using Parry.MonsterClass;
using UnityEngine;
using System;
using System.Collections;

namespace Parry.BossClass
{
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(AnimationClipPlayer), typeof(Rigidbody2D))]
    public sealed class Boss : MonoBehaviour, ICombatTarget
    {
        [SerializeField] private float hp;
        private BossSettings settings;
        private PlayerBerseker storedBerseker;
        private float storedDamage;
        private int storedSlashCount;
        private Collider2D bodyCollider;
        private Rigidbody2D body;
        private AnimationClipPlayer clipPlayer;
        private BossBrain brain;
        private BossFlinchData flinchData;
        [SerializeField] private int currentFlinchResistance;

        public event Action<string> OnNameChanged;
        public event Action<float, float> OnHpChanged;
        public event Action<float, float> OnStoredDamageChanged;
        public event Action<int, int> OnFlinchResistanceChanged;
        public event Action OnDeath;

        public float Hp => hp;
        public float MaxHp { get; private set; }
        public bool IsDead => hp <= 0f;
        public bool CanTakeDamage => isActiveAndEnabled && !IsDead && !IsExecuting && !brain.IsPhaseTransitioning;
        public bool IsExecuting { get; private set; }
        public int CurrentFlinchResistance => currentFlinchResistance;
        public bool IsFlinching => brain != null && brain.State is BossFlinchState;

        public void Initialize(MonsterBaseStats stats, BossFlinchData flinchData, BossSettings settings)
        {
            hp = stats.maxHp;
            MaxHp = stats.maxHp;
            this.flinchData = flinchData;
            this.settings = settings;
            currentFlinchResistance = flinchData.resistance;
            IsExecuting = false;
            storedDamage = 0f;
            storedSlashCount = 0;
        }

        public void InitUI()
        {
            OnNameChanged?.Invoke(brain.Data.BossName);
            OnHpChanged?.Invoke(hp, MaxHp);
            OnFlinchResistanceChanged?.Invoke(currentFlinchResistance, flinchData.resistance);
            OnStoredDamageChanged?.Invoke(storedDamage, hp);
        }

        private void Awake()
        {
            bodyCollider = GetComponent<Collider2D>();
            body = GetComponent<Rigidbody2D>();
            clipPlayer = GetComponent<AnimationClipPlayer>();
            brain = GetComponent<BossBrain>();
        }

        private void Start()
        {
            BossStatUI statUI = GetComponentInChildren<BossStatUI>(true);
            if (statUI != null)
                statUI.Init(this);
        }

        public void TakeDamage(ref MonsterHitContext context)
        {
            if (!CanTakeDamage)
                return;

            if (context.AttackType == PlayerAttackType.ParryCounter && context.Damage < hp)
                ReduceFlinchResistance();

            if (context.IsBerserking)
                TakeBerserkerDamage(ref context);
            else
                TakeNormalDamage(ref context);
        }

        private void TakeBerserkerDamage(ref MonsterHitContext context)
        {
            float remainingHp = Mathf.Max(0f, hp - context.Damage);
            
            bool canExecute = storedDamage + context.StoredDamage > remainingHp;
            
            if (canExecute)
            {
                EnterExecutionState();
                
                ObjectPoolManager.Instance.SpawnObject(
                    context.Berseker.ExecutionEffectPrefab, transform.position);
            }

            TakeNormalDamage(ref context);

            if (!IsExecuting && (IsDead || context.StoredDamage <= 0f))
                return;

            if (storedBerseker == null)
            {
                storedBerseker = context.Berseker;
                storedBerseker.OnBurstStoredSlashes += BurstStoredDamage;
                storedBerseker.OnDiscardStoredSlashes += ClearStoredDamage;
            }

            storedDamage += context.StoredDamage;
            storedSlashCount += context.SlashStackCount;
            OnStoredDamageChanged?.Invoke(storedDamage, hp);

            if (canExecute)
                StartBurstStoredDamage();
        }

        private void TakeNormalDamage(ref MonsterHitContext context)
        {
            DamageNumberSpawner.Instance.Spawn(ref context, GetDamageSpawnPosition());
            ApplyDamage(context.Damage);
        }

        private void ApplyDamage(float damage)
        {
            if (IsDead)
                return;
            
            hp = Mathf.Max(0f, hp - damage);
            OnHpChanged?.Invoke(hp, MaxHp);
            
            if (IsDead)
            {
                currentFlinchResistance = 0;
                OnFlinchResistanceChanged?.Invoke(currentFlinchResistance, flinchData.resistance);
                
            }

            OnStoredDamageChanged?.Invoke(storedDamage, hp);

            if (IsDead && !IsExecuting)
            {
                ClearStoredDamage();
                brain.EnterNextPhaseOrDeadState();
            }
        }

        private void BurstStoredDamage(GameObject hitEffectPrefab)
        {
            ObjectPoolManager.Instance.SpawnObject(hitEffectPrefab, transform.position);
            
            StartBurstStoredDamage();
        }


        private void StartBurstStoredDamage()
        {
            int burstCount = Mathf.Min(storedSlashCount, 10);
            float damage = storedDamage / burstCount;
            
            ClearStoredDamage();
            StartCoroutine(BurstDamageRoutine(damage, burstCount));
        }

        private IEnumerator BurstDamageRoutine(float damage, int burstCount)
        {
            bool isExecution = IsExecuting;
            var wait = new WaitForSeconds(settings.burstHitInterval);
            
            for (int i = 0; i < burstCount; i++)
            {
                TakeBurstDamage(damage);
                
                yield return wait;
            }

            if (isExecution)
                EndExecution();
        }

        private void TakeBurstDamage(float damage)
        {
            DamageNumberSpawner.Instance.Spawn(damage, GetDamageSpawnPosition());
            
            ApplyDamage(damage);
        }

        private Vector3 GetDamageSpawnPosition()
        {
            Bounds bounds = bodyCollider.bounds;
            return new Vector3(
                UnityEngine.Random.Range(bounds.min.x, bounds.max.x),
                UnityEngine.Random.Range(bounds.min.y, bounds.max.y),
                bounds.center.z);
        }

        private void ReduceFlinchResistance()
        {
            if (IsFlinching)
                return;

            currentFlinchResistance = Mathf.Max(0, currentFlinchResistance - 1);
            OnFlinchResistanceChanged?.Invoke(currentFlinchResistance, flinchData.resistance);
            
            if (currentFlinchResistance == 0)
                Flinch();
        }

        private void Flinch() => brain.ChangeState(brain.FlinchState);

        private void EnterExecutionState()
        {
            IsExecuting = true;
            brain.ChangeState(null);
        }

        private void EndExecution()
        {
            IsExecuting = false;
            
            if (IsDead)
                brain.EnterNextPhaseOrDeadState();
            else
                brain.ChangeState(brain.WanderState);
        }

        private void ClearStoredDamage()
        {
            if (storedBerseker != null)
            {
                storedBerseker.OnBurstStoredSlashes -= BurstStoredDamage;
                storedBerseker.OnDiscardStoredSlashes -= ClearStoredDamage;
                storedBerseker = null;
            }

            storedDamage = 0f;
            storedSlashCount = 0;
            OnStoredDamageChanged?.Invoke(storedDamage, hp);
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            IsExecuting = false;
            ClearStoredDamage();
        }

        public void ResetFlinchResistance()
        {
            currentFlinchResistance = flinchData.resistance;
            OnFlinchResistanceChanged?.Invoke(currentFlinchResistance, flinchData.resistance);
        }

        public void PlayDeathAnimation()
        {
            body.simulated = false;
            clipPlayer.Play(settings.deathClip, true);
        }

        public void EndDeath()
        {
            OnDeath?.Invoke();
            Destroy(gameObject);
        }
    }
}
