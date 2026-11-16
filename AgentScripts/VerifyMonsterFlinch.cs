using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Parry.MonsterClass;
using Parry.AnimationClass;
using Parry.CombatClass;

public static class VerifyMonsterFlinch
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    static async Task Until(Func<bool> done)
    {
        for (int i = 0; i < 300 && !done(); i++) await Task.Delay(10);
        Check(done(), "Timed out waiting for state");
    }
    public static async Task<object> Run()
    {
        bool background = Application.runInBackground;
        float timeScale = Time.timeScale;
        Application.runInBackground = true;
        GameObject target = null;
        try
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Monster/Goblin.prefab");
            var pose = AssetDatabase.LoadAllAssetsAtPath("Assets/AssetStoreImport/Monsters Creatures Fantasy/Sprites/Goblin/Attack2.png").OfType<Sprite>().Single(s => s.name == "Attack2_2");
            target = UnityEngine.Object.Instantiate(prefab);
            target.transform.position = new Vector3(-100, 0, 0);
            target.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            var brain = target.GetComponent<MonsterBrain>();
            var monster = target.GetComponent<Monster>();
            var clipPlayer = target.GetComponent<AnimationClipPlayer>();
            Check(brain.Attack.TryStartAttack(1f), "Attack setup failed");
            brain.ChangeState(brain.AttackState);
            brain.Movement.MoveTo(-90);
            monster.Flinch();
            float start = Time.time;
            Check(brain.State is MonsterFlinchState && monster.IsFlinching, "Flinch did not enter");
            Check(brain.Attack.CurrentAttack == null && !brain.Attack.TryStartAttack(1f), "Flinch attack lock failed");
            await Until(() => Time.time >= start + .2f);
            Check(brain.State is MonsterFlinchState && target.GetComponent<SpriteRenderer>().sprite == pose, "Attack2_2 pose not held");
            Check(Mathf.Abs(target.GetComponent<Rigidbody2D>().linearVelocity.x) < .001f, "Flinch did not stop movement");
            Time.timeScale = 0f;
            await Task.Delay(100);
            Check(brain.State is MonsterFlinchState, "Flinch timer ignored game pause");
            Time.timeScale = timeScale;
            await Until(() => brain.State == brain.WanderState);
            Check(!monster.IsFlinching && !clipPlayer.IsPlaying && Time.time >= start + 1f, "Flinch did not recover to idle/wander");
            monster.Flinch();
            typeof(Monster).GetMethod("EnterExecutionState", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(monster, null);
            Check(brain.State is MonsterExecutionState && !monster.IsFlinching, "Execution did not interrupt flinch");
            monster.Flinch();
            Check(brain.State is MonsterExecutionState, "Flinch interrupted execution");
            UnityEngine.Object.Destroy(target);
            target = UnityEngine.Object.Instantiate(prefab);
            target.transform.position = new Vector3(-100, 0, 0);
            brain = target.GetComponent<MonsterBrain>();
            monster = target.GetComponent<Monster>();
            monster.Initialize(new MonsterBaseStats { maxHp = 10f });
            monster.Flinch();
            monster.TakeDamage(new MonsterHitContext(10f, PlayerAttackType.Normal));
            Check(brain.State is MonsterDeadState && !monster.IsFlinching, "Death did not interrupt flinch");
            monster.Flinch();
            Check(brain.State is MonsterDeadState, "Dead monster entered flinch");
            await Until(() => target == null);
            return "Passed: Attack2_2 hold, attack/movement stop, attack start blocked, game-time duration pauses correctly, automatic idle/wander recovery after 1 second, execution/death override flinch, dead/executing monsters cannot enter flinch.";
        }
        finally
        {
            Time.timeScale = timeScale;
            Application.runInBackground = background;
            if (target != null) UnityEngine.Object.Destroy(target);
        }
    }
}
