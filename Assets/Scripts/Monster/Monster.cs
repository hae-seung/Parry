using System.Collections;
using UnityEngine;
using Parry.CombatClass;
using Parry.UIClass;
using Parry.PlayerClass;


namespace Parry.MonsterClass
{
    public class Monster : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float hitFlashDuration = 0.12f;
        [SerializeField] private Color hitColor = Color.red;
        [SerializeField] private Transform damageSpawnPos;
        
        [Tooltip("중첩 발동 시 각 타격 사이의 시간(초)입니다.")]
        [SerializeField, Min(0f)] private float burstHitInterval = 0.1f;

        private PlayerBerseker storedBerseker;
        private Collider2D bodyCollider;
        private SpriteRenderer spriteRenderer;
        private Color originalColor;
        private Coroutine hitFlashCoroutine;
        private float hp;
        [SerializeField] private float totalDamage;
        [SerializeField] private int count;

        public float Hp => hp;
        public float TotalDamage => totalDamage;
        public int Count => count;
        public bool IsDead => hp <= 0f;
        
        private void OnDisable()
        {
            StopAllCoroutines();
            StopHitFlash();
            ClearStoredDamage();
        }

        private void Awake()
        {
            bodyCollider = GetComponent<Collider2D>();
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            
            if (spriteRenderer != null) 
                originalColor = spriteRenderer.color;
        }

        public void Initialize(MonsterBaseStats stats)
        {
            StopAllCoroutines();
            StopHitFlash();
            hp = stats.maxHp;
            ClearStoredDamage();
        }

        public void TakeDamage(MonsterHitContext context)
        {
            float damage = context.Damage;
            if (IsDead || !isActiveAndEnabled || damage <= 0f || float.IsNaN(damage) || float.IsInfinity(damage))
                return;

            if (context.IsBerserking)
                TakeBerserkerDamage(context);
            else
                TakeNormalDamage(context);
        }

        private void TakeBerserkerDamage(MonsterHitContext context)
        {
            TakeNormalDamage(context);
            if (IsDead || context.StoredDamage <= 0f)
                return;

            if (storedBerseker == null)
            {
                storedBerseker = context.Berseker;
                storedBerseker.OnBurstStoredSlashes += BurstStoredDamage;
                storedBerseker.OnDiscardStoredSlashes += ClearStoredDamage;
            }

            totalDamage += context.StoredDamage;
            count += context.SlashStackCount;
        }

        private void TakeNormalDamage(MonsterHitContext context)
        {
            ApplyDamage(context, damageSpawnPos.position);
        }

        private void ApplyDamage(MonsterHitContext context, Vector3 numberPosition)
        {
            hp = Mathf.Max(0f, hp - context.Damage);

            DamageNumberSpawner.Instance.Spawn(context, numberPosition);
            
            if (IsDead)
            {
                // Prototype death: remove visuals and collision together; no respawn system yet.
                gameObject.SetActive(false);
                return;
            }

            StopHitFlash();
            
            if (spriteRenderer != null && hitFlashDuration > 0f)
                hitFlashCoroutine = StartCoroutine(HitFlash());
        }
        
        //플레이어가 누적된 데미지를 폭발시키는 이벤트 호출로 실행
        private void BurstStoredDamage()
        {
            int burstCount = Mathf.Min(count, 10);
            float burstDamage = totalDamage / burstCount;
            
            ClearStoredDamage();
            StartCoroutine(BurstDamageRoutine(burstDamage, burstCount));
        }

        private IEnumerator BurstDamageRoutine(float damage, int burstCount)
        {
            var wait = new WaitForSeconds(burstHitInterval);
            for (int i = 0; i < burstCount; i++)
            {
                TakeBurstDamage(damage);
                if (IsDead)
                    yield break;

                if (i < burstCount - 1)
                    yield return wait;
            }
        }

        private void TakeBurstDamage(float damage)
        {
            // 저장한 최종 피해를 사용하며, 중첩 저장이나 난수 계산을 다시 하지 않는다.
            Bounds bounds = bodyCollider.bounds;
            
            Vector3 randomPosition = new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(bounds.min.y, bounds.max.y),
                0.0f);
            
            ApplyDamage(new MonsterHitContext(
                    damage, 
                    PlayerAttackType.Normal),
                    randomPosition);
        }
        
        
        
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
