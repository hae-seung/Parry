using System;
using System.Reflection;
using UnityEngine;
using Parry.MonsterClass;

public static class VerifyMonsterBaseStats
{
    public static string Run()
    {
        if (!Application.isPlaying) throw new Exception("Play 모드 필요");
        var brain = UnityEngine.Object.FindAnyObjectByType<MonsterBrain>();
        var monster = brain.GetComponent<Monster>();
        var stats = brain.Data.BaseStats;
        if (monster.Hp != stats.maxHp) throw new Exception("Awake 체력 초기화 불일치");
        float speed = (float)typeof(MonsterMovement).GetField("moveSpeed", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(brain.Movement);
        if (speed != stats.moveSpeed) throw new Exception("Awake 이동속도 초기화 불일치");
        var other = new GameObject("IndependentMonsterStats").AddComponent<Monster>();
        try
        {
            other.Initialize(stats);
            monster.TakeDamage(new Parry.CombatClass.MonsterHitContext(25f));
            if (monster.Hp != stats.maxHp - 25f || other.Hp != stats.maxHp || brain.Data.BaseStats.maxHp != stats.maxHp)
                throw new Exception("런타임 체력 격리 실패");
            return $"PASS: Awake HP={stats.maxHp}, movement speed={speed}, per-monster HP, shared data unchanged.";
        }
        finally { UnityEngine.Object.Destroy(other.gameObject); }
    }
}
