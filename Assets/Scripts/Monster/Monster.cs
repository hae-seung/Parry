using System;
using System.Collections;
using UnityEngine;
using Parry.CombatClass;
using Parry.UIClass;
using Parry.PlayerClass;
using Parry.AnimationClass;


namespace Parry.MonsterClass
{
    [RequireComponent(typeof(AnimationClipPlayer))]
    public class Monster : MonoBehaviour, ICombatTarget
    {
        [SerializeField, Min(0f)] private float hitFlashDuration = 0.12f;
        [SerializeField] private Color hitColor = Color.red;
        
        [Tooltip("중첩 발동 시 각 타격 사이의 시간(초)입니다.")]
        [SerializeField, Min(0f)] private float burstHitInterval = 0.1f;

        [SerializeField] private AnimationClip executionClip;
        [SerializeField] private AnimationClip deathClip;

        private MonsterBrain brain;
        private AnimationClipPlayer clipPlayer;
        private Rigidbody2D body;
        private PlayerBerseker storedBerseker;
        private Collider2D bodyCollider;
        private SpriteRenderer spriteRenderer;
        private Color originalColor;
        private Coroutine hitFlashCoroutine;
        private float hp;
        private MonsterFlinchData flinchData;
        [SerializeField] private int currentFlinchResistance;
        [SerializeField] private float totalDamage;
        [SerializeField] private int count;

        public float Hp => hp;
        public float MaxHp { get; private set; }
        public event Action<float, float> OnHpChanged;
        public event Action<int, int> OnFlinchResistanceChanged;
        public event Action<float, float> OnStoredDamageChanged;

        public void InitUI()
        {
            OnHpChanged?.Invoke(hp, MaxHp);
            OnFlinchResistanceChanged?.Invoke(currentFlinchResistance, flinchData.resistance);
            OnStoredDamageChanged?.Invoke(totalDamage, hp);
        }
        public int CurrentFlinchResistance => currentFlinchResistance;
        public float TotalDamage => totalDamage;
        public int Count => count;
        public bool IsDead => hp <= 0f;
        public bool CanTakeDamage => isActiveAndEnabled && !IsDead && !IsExecuting;
        public bool IsExecuting { get; private set; }
        public bool IsFlinching => brain != null && brain.State is MonsterFlinchState;
        
        private void OnDisable()
        {
            StopAllCoroutines();
            StopHitFlash();
            ClearStoredDamage();
        }

        private void Awake()
        {
            brain = GetComponent<MonsterBrain>();
            clipPlayer = GetComponent<AnimationClipPlayer>();
            body = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<Collider2D>();
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            
            if (spriteRenderer != null) 
                originalColor = spriteRenderer.color;
        }

        private void Start()
        {
            MonsterStatUI statUI = GetComponentInChildren<MonsterStatUI>(true);
            if (statUI != null)
                statUI.Init(this);
        }

        public void Initialize(MonsterBaseStats stats, MonsterFlinchData flinchData = default)
        {
            hp = stats.maxHp;
            MaxHp = stats.maxHp;
            this.flinchData = flinchData;
            currentFlinchResistance = flinchData.resistance;
            IsExecuting = false;
            totalDamage = 0f;
            count = 0;
        }

        public void TakeDamage(ref MonsterHitContext context)
        {
            float damage = context.Damage;
            
            if (IsDead || IsExecuting || !isActiveAndEnabled)
                return;

            //반격 데미지로 피가 0이되면 기절 상태로 안들어가고 바로 죽도록
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
            bool bCanExecute = totalDamage + context.StoredDamage > remainingHp;
            if (bCanExecute)
            {
                //처형 상태 진입
                EnterExecutionState();
                
                //처형 터지는 이펙트 소환
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

            totalDamage += context.StoredDamage;
            count += context.SlashStackCount;
            OnStoredDamageChanged?.Invoke(totalDamage, hp);

            if (bCanExecute)
                StartBurstStoredDamage();
        }

        private void TakeNormalDamage(ref MonsterHitContext context)
        {
            DamageNumberSpawner.Instance.Spawn(ref context, GetDamageSpawnPosition());
            
            ApplyDamage(context.Damage);
        }

        //실제 데미지 적용
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
            
            OnStoredDamageChanged?.Invoke(totalDamage, hp);
            
            if (IsDead && !IsExecuting)
            {
                EnterDeadState();
                return;
            }

            StopHitFlash();
            
            if (spriteRenderer != null && hitFlashDuration > 0f)
                hitFlashCoroutine = StartCoroutine(HitFlash());
        }
        
        //플레이어가 누적된 데미지를 폭발시키는 이벤트 호출로 실행
        private void BurstStoredDamage(GameObject buerserkerHitVfx)
        {
            //폭발 이펙트 소환 1회
            ObjectPoolManager.Instance.SpawnObject(buerserkerHitVfx, transform.position);
            
            StartBurstStoredDamage();
        }

        private void StartBurstStoredDamage()
        {
            int burstCount = Mathf.Min(count, 10);
            float burstDamage = totalDamage / burstCount;

            ClearStoredDamage();
            StartCoroutine(BurstDamageRoutine(burstDamage, burstCount));
        }

        private IEnumerator BurstDamageRoutine(float damage, int burstCount)
        {
            WaitForSeconds wait = new WaitForSeconds(burstHitInterval);
            
            for (int i = 0; i < burstCount; i++)
            {
                TakeBurstDamage(damage);

                yield return wait;
            }

            if (IsExecuting)
                EnterDeadState();
        }

        private void TakeBurstDamage(float damage)
        {
            //데미지 스킨 스폰
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
            if (IsDead || IsExecuting || IsFlinching)
                return;

            currentFlinchResistance--;
            OnFlinchResistanceChanged?.Invoke(currentFlinchResistance, flinchData.resistance);
            
            if (currentFlinchResistance == 0)
                Flinch();
        }

        public void ResetFlinchResistance()
        {
            currentFlinchResistance = flinchData.resistance;
            OnFlinchResistanceChanged?.Invoke(currentFlinchResistance, flinchData.resistance);
        }

        public void Flinch()
        {
            if (brain == null || IsDead || IsExecuting)
                return;

            brain.ChangeState(brain.FlinchState);
        }

        private void EnterExecutionState()
        {
            IsExecuting = true;
            StopHitFlash();
            
            if (brain != null)
                brain.ChangeState(brain.ExecutionState);
            //트레이닝 더미용
            else
                PlayExecutionAnimation();
        }

        public void PlayExecutionAnimation()
        {
            clipPlayer.Play(executionClip, true);
        }

        private void EnterDeadState()
        {
            IsExecuting = false;
            
            ClearStoredDamage();
            StopHitFlash();
            
            if (brain != null)
                brain.ChangeState(brain.DeadState);
            else
                PlayDeathAnimation();
        }

        public void PlayDeathAnimation()
        {
            body.simulated = false;
            clipPlayer.Play(deathClip, true);
        }

        // 사망 클립 마지막 Animation Event에서 호출한다.
        public void EndDeath() => Destroy(gameObject);

        private void ClearStoredDamage()
        {
            if (storedBerseker != null)
            {
                storedBerseker.OnBurstStoredSlashes -= BurstStoredDamage;
                storedBerseker.OnDiscardStoredSlashes -= ClearStoredDamage;
                storedBerseker = null;
            }

            totalDamage = 0f;
            count = 0;
            OnStoredDamageChanged?.Invoke(totalDamage, hp);
        }

        private IEnumerator HitFlash()
        {
            spriteRenderer.color = hitColor;
            yield return new WaitForSeconds(hitFlashDuration);

            if (spriteRenderer != null)
                spriteRenderer.color = originalColor;
            hitFlashCoroutine = null;
        }

        private void StopHitFlash()
        {
            if (hitFlashCoroutine != null)
            {
                StopCoroutine(hitFlashCoroutine);
                hitFlashCoroutine = null;
            }
            if (spriteRenderer != null) 
                spriteRenderer.color = originalColor;
        }
    }

}
