using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using Parry.PlayerClass;
using Parry.CombatClass;
public static class VerifyPlayerParryGauge
{
    public static void Configure()
    {
        const string path = "Assets/Prefabs/Player/Player.prefab";
        var player = PrefabUtility.LoadPrefabContents(path);
        try
        {
            if (!player.GetComponent<PlayerParryGauge>()) player.AddComponent<PlayerParryGauge>();
            PrefabUtility.SaveAsPrefabAsset(player, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(player); }
    }
    private static void Set(object target, string name, object value) => target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    public static async Task<object> Run()
    {
        Check(Application.isPlaying, "Play mode required");
        var player = UnityEngine.Object.FindAnyObjectByType<PlayerCharacter>();
        var gauge = player.GetComponent<PlayerParryGauge>();
        var parry = player.GetComponent<PlayerParry>();
        Set(gauge, "decayPerSecond", 0f);
        Check(gauge.CurrentGauge == 0, "Initial gauge not zero");
        Check(!parry.TryParryHit() && gauge.CurrentGauge == 0, "Failed parry gained gauge");
        Check(parry.TryStartParry(), "Parry did not start");
        Check(parry.TryParryHit() && gauge.CurrentGauge == 15, "Success did not gain 15");
        Check(parry.TryParryHit() && gauge.CurrentGauge == 30, "Consecutive success did not gain gauge");
        for (int i = 0; i < 10; i++) gauge.GainOnParrySuccess();
        Check(gauge.CurrentGauge == 100, "Max clamp failed");
        Set(player.Stats, "criticalChance", 0f);
        Set(player.Stats, "damageVariancePercent", 0f);
        Check(player.Stats.CreateMonsterHit(100, PlayerAttackType.Normal).Damage == 150, "Full gauge damage failed");
        Check(player.Stats.CreateMonsterHit(100, PlayerAttackType.ParryCounter).Damage == 100, "Counter unexpectedly boosted");
        Check(!gauge.TryConsume(101) && gauge.CurrentGauge == 100, "Insufficient consume changed gauge");
        Check(!gauge.TryConsume(-1) && gauge.CurrentGauge == 100, "Negative consume changed gauge");
        Check(gauge.TryConsume(50) && gauge.CurrentGauge == 50, "Consumption failed");
        Check(player.Stats.CreateMonsterHit(100, PlayerAttackType.Normal).Damage == 125, "Damage did not fall with gauge");
        Set(gauge, "decayPerSecond", 1000f);
        await Task.Delay(600);
        Check(gauge.CurrentGauge == 0 && gauge.AttackMultiplier == 1, "Decay clamp or multiplier reset failed");
        return "Passed: success-only gain, consecutive parries, max clamp, consumption, damage scaling, counter isolation, timed decay to zero.";
    }
}
