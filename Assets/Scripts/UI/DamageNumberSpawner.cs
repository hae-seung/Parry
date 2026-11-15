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

        public void Spawn(MonsterHitContext context, Vector3 position)
        {
            DamageNumber prefab;
            
            if (context.AttackType == PlayerAttackType.Normal)
                prefab = context.IsCritical ? criticalDamagePrefab : normalDamagePrefab;
            else
                prefab = context.IsCritical ? parryCriticalDamagePrefab : parryDamagePrefab;
            
            prefab.Spawn(position, context.Damage);
        }
    }
}
