using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Parry.PlayerClass;
using Parry.MonsterClass;
using Parry.CombatClass;

public static class VerifyBerserkerEvents
{
    public static async Task<object> Run()
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        bool background = Application.runInBackground;
        Application.runInBackground = true;
        GameObject source = null, target = null;
        try
        {
            source = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/Player.prefab"));
            target = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Monster/TrainingDummy.prefab"));
            source.transform.position = new Vector3(-100, 0, 0);
            target.transform.position = new Vector3(-90, 0, 0);
            var mode = source.GetComponent<PlayerBerseker>();
            var monster = target.GetComponent<Monster>();
            typeof(Monster).GetField("hitFlashDuration", flags).SetValue(monster, 0f);
            typeof(Monster).GetField("burstHitInterval", flags).SetValue(monster, 0f);
            monster.Initialize(new MonsterBaseStats { maxHp = 1000f });
            var context = new MonsterHitContext(100f, PlayerAttackType.Normal, false, true, 50f, 2);
            context.Berseker = mode;
            monster.TakeDamage(context);
            context = new MonsterHitContext(80f, PlayerAttackType.Normal, false, true, 40f, 1);
            context.Berseker = mode;
            monster.TakeDamage(context);
            var burstEvent = typeof(PlayerBerseker).GetField("OnBurstStoredSlashes", flags);
            var discardEvent = typeof(PlayerBerseker).GetField("OnDiscardStoredSlashes", flags);
            if (((Delegate)burstEvent.GetValue(mode)).GetInvocationList().Length != 1 || ((Delegate)discardEvent.GetValue(mode)).GetInvocationList().Length != 1) throw new Exception("Duplicate subscription");
            if (monster.Hp != 820f || monster.TotalDamage != 90f || monster.Count != 3) throw new Exception("Storage mismatch");
            typeof(PlayerBerseker).GetMethod("BurstStoredSlashes", flags).Invoke(mode, null);
            for (int i = 0; i < 100 && monster.Hp != 730f; i++) await Task.Delay(20);
            if (monster.Hp != 730f || monster.TotalDamage != 0f || monster.Count != 0 || burstEvent.GetValue(mode) != null || discardEvent.GetValue(mode) != null) throw new Exception("Burst or unsubscribe failed");
            monster.TakeDamage(context);
            float before = monster.Hp;
            typeof(PlayerBerseker).GetMethod("DiscardStoredSlashes", flags).Invoke(mode, null);
            if (monster.Hp != before || monster.Count != 0 || burstEvent.GetValue(mode) != null) throw new Exception("Discard failed");
            monster.TakeDamage(context);
            target.SetActive(false);
            if (monster.Count != 0 || burstEvent.GetValue(mode) != null || discardEvent.GetValue(mode) != null) throw new Exception("Disable cleanup failed");
            return "Passed: repeat hits subscribe once; 90 stored damage / 3 stacks deals 30 x 3; burst clears subscription before damage; discard deals no damage; new storage resubscribes; disable unsubscribes.";
        }
        finally
        {
            if (target != null) UnityEngine.Object.Destroy(target);
            if (source != null) UnityEngine.Object.Destroy(source);
            Application.runInBackground = background;
        }
    }
}
