using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using Parry.PlayerClass;
using Parry.MonsterClass;
using Parry.CombatClass;
public static class VerifyPlayerCleanup
{
    private static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    public static async Task<object> Run()
    {
        bool background = Application.runInBackground;
        Application.runInBackground = true;
        try
        {
            foreach(var brain in UnityEngine.Object.FindObjectsByType<MonsterBrain>()) brain.enabled = false;
            var c=UnityEngine.Object.FindAnyObjectByType<PlayerCharacter>();
            var attack=c.GetComponent<PlayerAttack>();
            var parry=c.GetComponent<PlayerParry>();
            var body=c.GetComponent<Rigidbody2D>();
            for(int i=0;i<250 && !c.IsGrounded;i++) await Task.Delay(20);
            c.Attack(); Check(c.IsAttacking,"Attack did not start");
            c.Parry(); Check(c.IsParrying && !c.IsAttacking,"Parry did not cancel attack");
            parry.CancelParry();
            c.Attack(); Check(c.IsAttacking,"Attack did not restart");
            typeof(PlayerCharacter).GetField("lastRollTime",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(c,float.NegativeInfinity);
            c.Roll(); Check(c.IsRolling && !c.IsAttacking,"Roll did not cancel attack");
            for(int i=0;i<250 && c.IsRolling;i++) await Task.Delay(20);
            Check(!c.IsRolling,"Roll end event failed");
            var attacker=UnityEngine.Object.FindAnyObjectByType<TrainingDummy>().GetComponent<Monster>();
            var data=AssetDatabase.LoadAssetAtPath<MonsterAttackData>("Assets/Data/Monster/Attacks/Goblin/Goblin_Attack1.asset");
            c.Attack(); Check(c.IsAttacking,"Attack before hit failed");
            float hp=c.Stats.Hp;
            c.TakeDamage(new AttackHitContext(data,attacker,1));
            Check(c.Stats.Hp<hp && !c.IsAttacking && c.IsHitReacting,"Actual hit did not cancel action");
            return "Passed: attack/parry cancellation, attack/roll cancellation, real EndRoll event, actual monster damage and hit interruption.";
        }
        finally { Application.runInBackground=background; }
    }
}
