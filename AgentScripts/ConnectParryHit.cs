using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using Parry.PlayerClass;
using Parry.MonsterClass;
using Parry.CombatClass;
using Parry.AnimationClass;
public static class ConnectParryHit
{
    public static void Configure()
    {
        var player = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Player/Player.prefab");
        try
        {
            var data = new SerializedObject(player.GetComponent<PlayerParry>());
            data.FindProperty("parryHitPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Effects/Hit_Parry.prefab");
            data.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(player, "Assets/Prefabs/Player/Player.prefab");
        }
        finally { PrefabUtility.UnloadPrefabContents(player); }
    }
    public static async Task<object> Verify()
    {
        if (!Application.isPlaying) throw new Exception("Play mode required");
        var player = UnityEngine.Object.FindAnyObjectByType<PlayerCharacter>();
        var parry = player.GetComponent<PlayerParry>();
        var monster = UnityEngine.Object.FindAnyObjectByType<TrainingDummy>().GetComponent<Monster>();
        var attack = AssetDatabase.LoadAssetAtPath<MonsterAttackData>("Assets/Data/Monster/Attacks/Goblin/Goblin_Attack1.asset");
        float playerHp = player.Stats.Hp;
        if (!parry.TryStartParry()) throw new Exception("Parry did not start");
        player.TakeDamage(new AttackHitContext(attack, monster, 1));
        var effect = UnityEngine.Object.FindObjectsByType<EffectLifetime>().Single(e => e.name == "Hit_Parry(Clone)");
        if (Vector3.Distance(effect.transform.position, monster.transform.position) > .001f) throw new Exception("Wrong effect position");
        if (player.Stats.Hp != playerHp) throw new Exception("Parry failed to block damage");

        await Task.Delay(3000);
        if (effect.gameObject.activeSelf) throw new Exception("Animation did not return effect");
        parry.PlaySuccessEffects(monster.transform.position);
        var reused = UnityEngine.Object.FindObjectsByType<EffectLifetime>().Single(e => e.name == "Hit_Parry(Clone)");
        if (reused != effect) throw new Exception("Pool did not reuse effect");
        return "Parry success: monster position, player damage blocked, animation event pool return and instance reuse passed.";
    }
}


