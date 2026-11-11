using System.Collections;
using UnityEngine;


namespace Parry.MonsterClass
{
    public class Monster : MonoBehaviour
    {
        [SerializeField] private float hp = 100f;
        [SerializeField, Min(0f)] private float hitFlashDuration = 0.12f;
        [SerializeField] private Color hitColor = Color.red;

        private SpriteRenderer spriteRenderer;
        private Color originalColor;
        private Coroutine hitFlashCoroutine;

        public float Hp { get; private set; }
        public bool IsDead => hp <= 0f;
        
        private void OnDisable() => StopHitFlash();

        private void Awake()
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            
            if (spriteRenderer != null) 
                originalColor = spriteRenderer.color;
        }

        public void TakeDamage(float damage)
        {
            if (IsDead || !isActiveAndEnabled || damage <= 0f || float.IsNaN(damage) || float.IsInfinity(damage))
                return;

            hp = Mathf.Max(0f, hp - damage);
            if (IsDead)
            {
                // Prototype death: remove visuals and collision together; no respawn system yet.
                gameObject.SetActive(false);
                return;
            }

            StopHitFlash();
            if (spriteRenderer != null && hitFlashDuration > 0f)
                hitFlashCoroutine = StartCoroutine(nameof(HitFlash));
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
