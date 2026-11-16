using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Parry.MonsterClass;
using Parry.PlayerClass;
using Parry.CombatClass;
using Parry.AnimationClass;
using DamageNumbersPro;
using Parry.UIClass;

public static class VerifyExecutionStates
{
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    static async Task Until(Func<bool> condition)
    {
        for (int i = 0; i < 200 && !condition(); i++) await Task.Delay(10);
        Check(condition(), "Timed out");
    }
    public static async Task<object> Run()
    {
        bool background = Application.runInBackground;
        Application.runInBackground = true;
        GameObject source = null, target = null, numberTemplate = null;
        var spawner = DamageNumberSpawner.Instance;
        var numberField = typeof(DamageNumberSpawner).GetField("normalDamagePrefab", Flags);
        var originalNumber = (DamageNumber)numberField.GetValue(spawner);
        try
        {
            numberTemplate = UnityEngine.Object.Instantiate(originalNumber.gameObject);
            numberTemplate.SetActive(false);
            var testNumber = numberTemplate.GetComponent<DamageNumber>();
            testNumber.lifetime = 10f;
            testNumber.enablePooling = false;
            numberField.SetValue(spawner, testNumber);
            source = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/Player.prefab"));
            source.transform.position = new Vector3(-120, 0, 0);
            var mode = source.GetComponent<PlayerBerseker>();
            var pose = AssetDatabase.LoadAllAssetsAtPath("Assets/AssetStoreImport/Monsters Creatures Fantasy/Sprites/Goblin/Take Hit.png").OfType<Sprite>().Single(s => s.name == "Take Hit_2");
            foreach (string name in new[] { "Goblin", "TrainingDummy" })
            {
                target = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Monster/" + name + ".prefab"));
                target.transform.position = new Vector3(name == "Goblin" ? -90 : -190, 2, 0);
                target.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
                var monster = target.GetComponent<Monster>();
                var brain = target.GetComponent<MonsterBrain>();
                var clip = target.GetComponent<AnimationClipPlayer>();
                monster.Initialize(new MonsterBaseStats { maxHp = 100f });
                typeof(Monster).GetField("burstHitInterval", Flags).SetValue(monster, .05f);
                if (brain != null)
                {
                    Check(brain.Attack.TryStartAttack(1f), "Could not start attack fixture");
                    brain.ChangeState(brain.AttackState);
                }
                Physics2D.SyncTransforms();
                var bounds = target.GetComponent<Collider2D>().bounds;
                var positionMethod = typeof(Monster).GetMethod("GetDamageSpawnPosition", Flags);
                for (int sample = 0; sample < 100; sample++)
                    Check(bounds.Contains((Vector3)positionMethod.Invoke(monster, null)), "Damage spawn outside body bounds");
                var hit = new MonsterHitContext(1f, PlayerAttackType.Normal, false, true, 1000f, 20);
                hit.Berseker = mode;
                monster.TakeDamage(hit);
                Check(monster.IsExecuting && monster.IsDead && target.activeInHierarchy, "Execution was disabled at zero HP");
                if (brain != null)
                {
                    Check(brain.State is MonsterExecutionState, "Missing execution state");
                    Check(brain.Attack.CurrentAttack == null && !brain.Attack.TryStartAttack(1f), "Attack not cancelled or restarted");
                }
                monster.TakeDamage(new MonsterHitContext(555f, PlayerAttackType.Normal));
                await Until(() => target.GetComponent<SpriteRenderer>().sprite == pose);
                Check(monster.IsExecuting && target.GetComponent<SpriteRenderer>().sprite == pose, $"Execution pose not held: {name}, executing={monster.IsExecuting}, sprite={target.GetComponent<SpriteRenderer>().sprite.name}, expected={pose.name}, state={brain?.State}, clipPlaying={clip.IsPlaying}");
                await Until(() => !monster.IsExecuting);
                Check(target != null && target.activeInHierarchy, "Destroyed before death animation");
                if (brain != null) Check(brain.State is MonsterDeadState, "Missing dead state");
                await Task.Delay(30);
                int numbers = UnityEngine.Object.FindObjectsByType<DamageNumber>().Count(d => d.number == 100f && Mathf.Abs(d.transform.position.x - target.transform.position.x) < 20f);
                Check(numbers == 10, "Expected ten burst numbers, found " + numbers + " on " + name + ": " + string.Join("; ", UnityEngine.Object.FindObjectsByType<DamageNumber>(FindObjectsInactive.Include).Where(d => Mathf.Abs(d.transform.position.x - target.transform.position.x) < 20f).Select(d => d.number + "/" + d.gameObject.activeInHierarchy + "/life=" + d.lifetime + "/pool=" + d.enablePooling + "/name=" + d.name)));
                Check(!target.GetComponent<Rigidbody2D>().simulated, "Death physics still active");
                await Until(() => target == null);
            }
            target = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Monster/Goblin.prefab"));
            target.transform.position = new Vector3(-90, 2, 0);
            var normal = target.GetComponent<Monster>();
            normal.Initialize(new MonsterBaseStats { maxHp = 10f });
            normal.TakeDamage(new MonsterHitContext(10f, PlayerAttackType.Normal));
            Check(target.GetComponent<MonsterBrain>().State is MonsterDeadState && !normal.IsExecuting, "Ordinary death did not enter dead state");
            await Until(() => target == null);
            return "Passed: Goblin and dummy hold Take Hit_2; active attacks cancel; external damage rejected; all ten burst numbers finish despite HP zero; dead state follows; death event destroys; normal lethal damage enters dead state directly.";
        }
        finally
        {
            numberField.SetValue(spawner, originalNumber);
            if (numberTemplate != null) UnityEngine.Object.Destroy(numberTemplate);
            if (target != null) UnityEngine.Object.Destroy(target);
            if (source != null) UnityEngine.Object.Destroy(source);
            Application.runInBackground = background;
        }
    }
}
