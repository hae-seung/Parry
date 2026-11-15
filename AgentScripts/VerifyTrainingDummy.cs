using System;
using System.Threading.Tasks;
using UnityEngine;
using Parry.MonsterClass;
using Parry.CombatClass;
public static class VerifyTrainingDummy
{
    public static async Task<object> Run()
    {
        if (!Application.isPlaying) throw new Exception("Play mode required.");
        var dummy = UnityEngine.Object.FindAnyObjectByType<TrainingDummy>();
        var monster = dummy.GetComponent<Monster>();
        var start = dummy.transform.position;
        float before = monster.Hp;
        monster.TakeDamage(new MonsterHitContext(100, PlayerAttackType.Normal));
        if (monster.Hp != before - 100) throw new Exception("Damage not applied.");
        await Task.Delay(500);
        if (dummy.transform.position != start) throw new Exception("Dummy moved.");
        if (!dummy.gameObject.activeSelf || dummy.GetComponent<MonsterBrain>() != null || dummy.GetComponent<MonsterAttack>() != null)
            throw new Exception("Unexpected dummy state.");
        return new { hpBefore = before, hpAfter = monster.Hp, stationary = true, noAIOrAttack = true };
    }
}
