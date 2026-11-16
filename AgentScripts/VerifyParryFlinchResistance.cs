using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Parry.PlayerClass;
using Parry.MonsterClass;
using Parry.CombatClass;

public static class VerifyParryFlinchResistance
{
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Set(object target, string field, object value) => target.GetType().GetField(field, Flags).SetValue(target, value);
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    static async Task Until(Func<bool> done)
    {
        for (int i = 0; i < 1000 && !done(); i++) await Task.Delay(10);
        Check(done(), "Timed out waiting for recovery");
    }
    static void Parry(PlayerCharacter player, Monster attacker, MonsterAttackData attack)
    {
        var parry = player.GetComponent<PlayerParry>();
        parry.CancelParry();
        Set(parry, "nextParryTime", 0f);
        Set(player.GetComponent<PlayerMovement>(), "<IsGrounded>k__BackingField", true);
        Check(parry.TryStartParry(), "Parry setup failed");
        player.TakeDamage(new AttackHitContext(attack, attacker, 1));
    }
    public static async Task<object> Run()
    {
        bool background = Application.runInBackground;
        Application.runInBackground = true;
        GameObject source = null, target = null, other = null;
        try
        {
            source = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/Player.prefab"));
            source.transform.position = new Vector3(-120, 0, 0);
            source.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            var player = source.GetComponent<PlayerCharacter>();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Monster/Goblin.prefab");
            target = UnityEngine.Object.Instantiate(prefab);
            other = UnityEngine.Object.Instantiate(prefab);
            target.transform.position = new Vector3(-100, 0, 0);
            other.transform.position = new Vector3(-90, 0, 0);
            target.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            other.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            var brain = target.GetComponent<MonsterBrain>();
            var otherBrain = other.GetComponent<MonsterBrain>();
            var monster = target.GetComponent<Monster>();
            var data = brain.Data;
            int initial = data.Flinch.resistance;
            Check(initial == 3 && monster.CurrentFlinchResistance == initial, "Data resistance not initialized");
            monster.TakeDamage(new MonsterHitContext(1f, PlayerAttackType.Normal));
            Check(monster.CurrentFlinchResistance == initial, "Normal damage reduced resistance");
            for (int i = 1; i <= initial; i++)
            {
                Parry(player, monster, data.Attacks[0]);
                Check(monster.CurrentFlinchResistance == initial - i, "Parry did not reduce resistance by exactly one");
                Check(monster.IsFlinching == (i == initial), "Flinch threshold mismatch");
                Check(otherBrain.Monster.CurrentFlinchResistance == initial && data.Flinch.resistance == initial, "Resistance mutated another monster or shared data");
            }
            float start = Time.time;
            await Until(() => Time.time >= start + data.Flinch.duration * .5f);
            Check(monster.IsFlinching && monster.CurrentFlinchResistance == 0, "Flinch ended early");
            await Until(() => !monster.IsFlinching);
            Check(Time.time >= start + data.Flinch.duration && monster.CurrentFlinchResistance == initial, "Duration or resistance reset failed");
            for (int i = 0; i < initial; i++) Parry(player, monster, data.Attacks[0]);
            Check(monster.IsFlinching, "Second flinch cycle failed");
            other.GetComponent<Monster>().Initialize(new MonsterBaseStats { maxHp = 1f });
            Parry(player, other.GetComponent<Monster>(), data.Attacks[0]);
            Check(otherBrain.State is MonsterDeadState && !other.GetComponent<Monster>().IsFlinching, "Lethal parry entered flinch instead of death");
            return "Passed: actual player parry success reduces 3->2->1->0; normal damage unchanged; monster-specific resistance; shared data preserved; configured duration held; resistance resets after recovery; second cycle works; lethal parry stays dead.";
        }
        finally
        {
            if (source != null) UnityEngine.Object.Destroy(source);
            if (target != null) UnityEngine.Object.Destroy(target);
            if (other != null) UnityEngine.Object.Destroy(other);
            Application.runInBackground = background;
        }
    }
}
