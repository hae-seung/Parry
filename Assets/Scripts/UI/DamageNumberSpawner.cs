using DamageNumbersPro;
using Parry.CombatClass;
using UnityEngine;

namespace Parry.UIClass
{
    public sealed class DamageNumberSpawner : Singleton<DamageNumberSpawner>
    {
        [SerializeField] private DamageNumber normalDamagePrefab;
        [SerializeField] private DamageNumber criticalDamagePrefab;
        [SerializeField] private DamageNumber parryDamagePrefab;
        [SerializeField] private DamageNumber parryCriticalDamagePrefab;

        private void Start()
        {
            normalDamagePrefab.PrewarmPool();
            criticalDamagePrefab.PrewarmPool();
        }

        public void Spawn(ref MonsterHitContext context, Vector3 position)
        {
            Spawn(context.Damage, position, context.AttackType, context.IsCritical);
        }

        public void Spawn(float damage, Vector3 position,
            PlayerAttackType attackType = PlayerAttackType.Normal, bool isCritical = false)
        {
            DamageNumber prefab;
            
            if (attackType == PlayerAttackType.Normal)
                prefab = isCritical ? criticalDamagePrefab : normalDamagePrefab;
            else
                prefab = isCritical ? parryCriticalDamagePrefab : parryDamagePrefab;
            
            prefab.Spawn(position, damage);
        }
    }
}
