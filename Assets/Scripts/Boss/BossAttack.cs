using System.Collections.Generic;
using Parry.AnimationClass;
using Parry.CombatClass;
using Parry.MonsterClass;
using Parry.PlayerClass;
using UnityEngine;

namespace Parry.BossClass
{
    [RequireComponent(typeof(Boss),
        typeof(AnimationClipPlayer), 
        typeof(MonsterMovement))]
    public sealed class BossAttack : MonoBehaviour
    {
        private Boss boss;
        private AnimationClipPlayer clipPlayer;
        private MonsterMovement movement;
        private MonsterSensor sensor;
        private List<BossAttackData> attacks;
        private readonly List<float> attackReadyTimes = new List<float>();
        private readonly List<int> availableIndices = new List<int>();
        private readonly List<Collider2D> hitResults = new List<Collider2D>();
        private readonly HashSet<PlayerCharacter> hitTargets = new HashSet<PlayerCharacter>();
        private ContactFilter2D hitFilter;
        private int attackDirection;

        public BossAttackData CurrentAttack { get; private set; }
        public bool IsMotionPlaying => clipPlayer.IsPlaying;

        private void Awake()
        {
            boss = GetComponent<Boss>();
            clipPlayer = GetComponent<AnimationClipPlayer>();
            movement = GetComponent<MonsterMovement>();
            sensor = GetComponent<MonsterSensor>();
            
        }

        public void Initialize(List<BossAttackData> attackData, LayerMask targetLayers)
        {
            hitFilter = new ContactFilter2D();
            hitFilter.SetLayerMask(targetLayers);
            hitFilter.useTriggers = true;
            attacks = attackData;
            attackReadyTimes.Clear();
            
            for (int i = 0; i < attacks.Count; i++)
                attackReadyTimes.Add(0f);
        }

        public bool TryStartAttack(float distanceSquared)
        {
            if (boss.IsDead || CurrentAttack != null || !isActiveAndEnabled)
                return false;
            
            availableIndices.Clear();
            
            for (int i = 0; i < attacks.Count; i++)
            {
                BossAttackData attack = attacks[i];
                
                bool ready = Time.time >= attackReadyTimes[i];
                if (ready && attack.CanUseAtDistance(distanceSquared))
                    availableIndices.Add(i);
            }

            if (availableIndices.Count == 0)
                return false;

            int selectedIndex = availableIndices[Random.Range(0, availableIndices.Count)];
            BossAttackData selected = attacks[selectedIndex];
            
            if (!clipPlayer.Play(selected.AnimationClip))
                return false;

            CurrentAttack = selected;
            attackDirection = movement.FacingDirection;
            attackReadyTimes[selectedIndex] = Time.time + selected.Cooldown;
            
            return true;
        }

        public void FaceTarget()
        {
            if (CurrentAttack == null || !IsMotionPlaying || boss.IsDead)
                return;

            if (sensor.HasTarget)
                movement.Face(sensor.Target.transform.position.x);

            attackDirection = movement.FacingDirection;
        }

        public void SpawnAttackEffect(BossAttackEffectData effectData)
        {
            if (!isActiveAndEnabled || CurrentAttack == null || !IsMotionPlaying || boss.IsDead)
                return;

            Vector2 offset = effectData.Offset;
            Vector3 position = transform.position +
                               new Vector3(offset.x * movement.FacingDirection, offset.y, 0f);
            ObjectPoolManager.Instance.SpawnObject(effectData.Prefab, position, transform);
        }

        public void ApplyAttackHit(BossAttackHitData hitData)
        {
            if (!isActiveAndEnabled || CurrentAttack == null || !IsMotionPlaying || boss.IsDead)
                return;

            Vector2 offset = hitData.HitboxOffset;
            Vector2 center = (Vector2)transform.position + new Vector2(offset.x * attackDirection, offset.y);
            
            Physics2D.OverlapBox(center, hitData.HitboxSize, 0f, hitFilter, hitResults);
            
            hitTargets.Clear();
            
            var context = new AttackHitContext(hitData, boss, attackDirection);
            
            foreach (Collider2D hit in hitResults)
            {
                PlayerCharacter target = hit.GetComponentInParent<PlayerCharacter>();
                if (target != null && target.isActiveAndEnabled && !target.IsDead && hitTargets.Add(target))
                    target.TakeDamage(ref context);
            }
        }

        public void Stop()
        {
            CurrentAttack = null;
            
            if (clipPlayer != null)
                clipPlayer.Stop();
        }

        private void OnDisable() => Stop();
    }
}
