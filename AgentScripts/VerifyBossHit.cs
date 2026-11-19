using System;
using System.Reflection;
using Parry.BossClass;
using Parry.PlayerClass;
using Parry.CombatClass;
using UnityEngine;

public static class VerifyBossHit
{
    public static object Run()
    {
        if (!Application.isPlaying) throw new Exception("Play mode required.");
        var boss = UnityEngine.Object.FindAnyObjectByType<Boss>();
        var player = UnityEngine.Object.FindAnyObjectByType<PlayerCharacter>();
        if (boss == null || player == null) throw new Exception("Scene boss/player missing.");
        var attack = player.GetComponent<PlayerAttack>();
        var data = player.Data.Attack.attacks[0];
        Vector3 original = boss.transform.position;
        try
        {
            Vector2 offset = data.HitboxOffset;
            Vector3 center = player.transform.position + new Vector3(offset.x * player.FacingDirection, offset.y);
            boss.transform.position += center - boss.GetComponent<Collider2D>().bounds.center;
            Physics2D.SyncTransforms();
            Set(attack, "currentAttack", data);
            Set(attack, "attackDirection", player.FacingDirection);
            Set(attack, "attackIndex", 0);
            Set(attack, "hitEventPending", true);
            float before = boss.Hp;
            attack.ApplyAttackHit();
            float after = boss.Hp;
            if (after >= before) throw new Exception("Player attack did not damage boss.");
            attack.ApplyAttackHit();
            if (boss.Hp != after) throw new Exception("Duplicate hit event applied damage.");
            var lethal = new MonsterHitContext(10000f, PlayerAttackType.Normal);
            boss.TakeDamage(ref lethal);
            if (boss.Hp != 0f || boss.CanTakeDamage) throw new Exception("HP zero handling failed.");
            return new { before, after, damage = before - after, zeroHp = boss.Hp, duplicateBlocked = true };
        }
        finally
        {
            boss.transform.position = original;
        }
    }

    private static void Set(object target, string field, object value)
        => target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
}
