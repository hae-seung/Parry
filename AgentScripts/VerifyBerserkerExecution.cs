using System;
using System.Reflection;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Parry.PlayerClass;
using Parry.MonsterClass;
using Parry.CombatClass;
using Parry.AnimationClass;

public static class VerifyBerserkerExecution
{
    public static async Task<object> Run()
    {
        bool background = Application.runInBackground;
        Application.runInBackground = true;
        GameObject source = null, target = null, other = null;
        try
        {
            source = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/Player.prefab"));
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Monster/TrainingDummy.prefab");
            target = UnityEngine.Object.Instantiate(prefab);
            other = UnityEngine.Object.Instantiate(prefab);
            source.transform.position = new Vector3(-100, 0, 0);
            target.transform.position = new Vector3(-90, 0, 0);
            other.transform.position = new Vector3(-80, 0, 0);
            var mode = source.GetComponent<PlayerBerseker>();
            var monster = target.GetComponent<Monster>();
            var untouched = other.GetComponent<Monster>();
            typeof(Monster).GetField("hitFlashDuration", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(monster, 0f);
            monster.Initialize(new MonsterBaseStats { maxHp = 100f });
            untouched.Initialize(new MonsterBaseStats { maxHp = 1000f });
            var context = new MonsterHitContext(10f, PlayerAttackType.Normal, false, true, 5f, 1);
            context.Berseker = mode;
            untouched.TakeDamage(context);
            context = new MonsterHitContext(10f, PlayerAttackType.Normal, false, true, 90f, 2);
            context.Berseker = mode;
            monster.TakeDamage(context);
            if (monster.Hp != 90f || monster.Count != 2 || target.GetComponentInChildren<EffectLifetime>() != null) throw new Exception("Strict greater-than condition failed");
            context = new MonsterHitContext(1f, PlayerAttackType.Normal, false, true, 1f, 1);
            context.Berseker = mode;
            monster.TakeDamage(context);
            var effect = UnityEngine.Object.FindObjectsByType<EffectLifetime>()
                .Single(e => e.name.StartsWith("Execution_Effect") && Vector3.Distance(e.transform.position, target.transform.position) < .0001f);
            if (effect == null || !effect.name.StartsWith("Execution_Effect") || effect.transform.parent != null || Vector3.Distance(effect.transform.position, target.transform.position) > .0001f) throw new Exception("Execution effect world position or root parent mismatch");
            if (monster.Count != 0 || untouched.Count != 1 || untouched.TotalDamage != 5f) throw new Exception("Other target storage changed");
            for (int i = 0; i < 100 && !monster.IsDead; i++) await Task.Delay(20);
            if (!monster.IsDead || mode.IsBerserking || untouched.IsDead) throw new Exception("Targeted execution failed");
            if (!effect.gameObject.activeInHierarchy || effect.transform.parent != null)
                throw new Exception($"Death effect state: self={effect.gameObject.activeSelf}, hierarchy={effect.gameObject.activeInHierarchy}, parent={effect.transform.parent}, hp={monster.Hp}, timeScale={Time.timeScale}");
            for (int i = 0; i < 100 && effect.gameObject.activeSelf; i++) await Task.Delay(20);
            if (effect.gameObject.activeSelf) throw new Exception("Execution effect did not return to pool");

            target.SetActive(true);
            monster.Initialize(new MonsterBaseStats { maxHp = 10f });
            context = new MonsterHitContext(10f, PlayerAttackType.Normal, false, true, 5f, 1);
            context.Berseker = mode;
            monster.TakeDamage(context);
            if (!monster.IsDead || !effect.gameObject.activeInHierarchy || effect.transform.parent != null || Vector3.Distance(effect.transform.position, target.transform.position) > .0001f)
                throw new Exception("Direct lethal hit did not spawn and preserve execution effect");
            for (int i = 0; i < 100 && effect.gameObject.activeSelf; i++) await Task.Delay(20);
            if (effect.gameObject.activeSelf) throw new Exception("Direct lethal effect did not return to pool");
            return "Passed: effect precedes damage, survives death and returns to pool; direct lethal hit also spawns effect and reuses pool;  no execution at equality; greater stored damage bursts only the target; execution effect spawns once at monster position without a parent; stored damage kills target; other target and player mode unchanged.";
        }
        finally
        {
            if (target != null) UnityEngine.Object.Destroy(target);
            if (other != null) UnityEngine.Object.Destroy(other);
            if (source != null) UnityEngine.Object.Destroy(source);
            Application.runInBackground = background;
        }
    }
}

