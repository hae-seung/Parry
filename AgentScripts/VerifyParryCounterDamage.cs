using System;
using System.Reflection;
using UnityEngine;
using Parry.PlayerClass;
using Parry.MonsterClass;
using Parry.CombatClass;

public static class VerifyParryCounterDamage
{
    private static void Assert(bool value, string message)
    {
        if (!value) throw new Exception(message);
    }

    public static string Run()
    {
        Assert(Application.isPlaying, "Play 모드 필요");
        var player = UnityEngine.Object.FindAnyObjectByType<PlayerCharacter>();
        var parry = player.GetComponent<PlayerParry>();
        var brain = UnityEngine.Object.FindAnyObjectByType<MonsterBrain>();
        var monster = brain.GetComponent<Monster>();
        var body = monster.GetComponent<Rigidbody2D>();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        typeof(Monster).GetField("hp", flags).SetValue(monster, 1000f);
        brain.Attack.Stop();
        brain.Attack.Initialize(brain.Data.Attacks);
        Assert(brain.Attack.TryStartAttack(0f), "몬스터 공격 시작 실패");
        var motion = brain.Attack.CurrentAttack;
        var state = brain.State;
        var velocity = body.linearVelocity;
        var other = new GameObject("OtherCounterTarget").AddComponent<Monster>();
        other.Initialize(new MonsterBaseStats { maxHp = 100f });
        try
        {
            parry.CancelParry();
            typeof(PlayerParry).GetField("nextParryTime", flags).SetValue(parry, 0f);
            Assert(parry.TryStartParry(), "패링 시작 실패");
            float hp = player.Stats.Hp;
            float monsterHp = monster.Hp;
            float otherHp = other.Hp;
            float damage = player.Stats.ParryCounterDamage;
            Assert(damage == 10f, "반격 피해 초기값 오류");
            player.TakeDamage(new AttackHitContext(motion, monster, 1));
            Assert(monster.Hp == monsterHp - damage, "반격 피해량 불일치");
            Assert(other.Hp == otherHp && player.Stats.Hp == hp, "다른 대상 또는 플레이어 체력 변경");
            Assert(brain.Attack.IsMotionPlaying && brain.Attack.CurrentAttack == motion && ReferenceEquals(brain.State, state), "반격이 공격 또는 AI 상태를 중단함");
            Assert(body.linearVelocity == velocity, "반격이 이동 속도를 변경함");
            Assert(parry.IsParrying, "반격 후 패링 판정 창 종료됨");
            player.TakeDamage(new AttackHitContext(motion, monster, 1));
            Assert(monster.Hp == monsterHp - damage * 2f, "유효 시간 내 두 번째 패링 반격 실패");
            typeof(Monster).GetField("hp", flags).SetValue(monster, damage);
            player.TakeDamage(new AttackHitContext(motion, monster, 1));
            Assert(monster.IsDead && !monster.gameObject.activeSelf, "반격 치명타 사망 처리 실패");
            return "PASS: configured counter damage, attacker-only damage, player protected, attack/state/velocity preserved while alive, active parry window retained, lethal counter death.";
        }
        finally { UnityEngine.Object.Destroy(other.gameObject); }
    }
}
