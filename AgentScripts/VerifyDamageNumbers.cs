using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using DamageNumbersPro;
using Parry.PlayerClass;
using Parry.MonsterClass;
using Parry.CombatClass;

public static class VerifyDamageNumbers
{
    private static void Assert(bool value, string message)
    {
        if (!value) throw new Exception(message);
    }
    public static async Task<string> Run()
    {
        Assert(Application.isPlaying, "Play 모드 필요");
        foreach (var brain in UnityEngine.Object.FindObjectsByType<MonsterBrain>()) brain.enabled = false;
        var player = UnityEngine.Object.FindAnyObjectByType<PlayerCharacter>();
        var monster = UnityEngine.Object.FindAnyObjectByType<Monster>();
        var stats = new SerializedObject(player.Stats);
        float oldChance = stats.FindProperty("criticalChance").floatValue;
        float oldVariance = stats.FindProperty("damageVariancePercent").floatValue;
        Action<float, float> configure = (chance, variance) =>
        {
            stats.Update();
            stats.FindProperty("criticalChance").floatValue = chance;
            stats.FindProperty("damageVariancePercent").floatValue = variance;
            stats.ApplyModifiedPropertiesWithoutUndo();
        };
        try
        {
            await Task.Delay(300);
            configure(0f, 0f);
            var hit = player.Stats.CreateMonsterHit(100f);
            Assert(hit.Damage == 100f && !hit.IsCritical, "일반 피해 계산 실패");
            float hp = monster.Hp;
            monster.TakeDamage(hit);
            await Task.Delay(50);
            var normal = DamageNumber.activeInstances.Single(n => n.number == 100f);
            Assert(normal.GetTextMesh().textInfo.characterCount >= 3, "TMP 숫자 메시 생성 실패");
            Assert(monster.Hp == hp - 100f && normal.numberSettings.color.g > 0.7f && normal.enablePooling, "일반 피해·노란 숫자·풀링 실패");
            Vector3 origin = normal.transform.position;
            var pooledNumber = normal;
            configure(100f, 0f);
            var criticalHit = player.Stats.CreateMonsterHit(100f);
            Assert(criticalHit.Damage == 150f && criticalHit.IsCritical, "치명타 1.5배 계산 실패");
            monster.TakeDamage(criticalHit);
            await Task.Delay(50);
            var critical = DamageNumber.activeInstances.Single(n => n.number == 150f);
            Assert(critical.numberSettings.color.r > 0.9f && critical.numberSettings.color.g < 0.4f, "치명타 빨간 숫자 실패");
            await Task.Delay(150);
            Assert(normal.transform.position != origin, "숫자 확산 이동 실패");
            configure(0f, 10f);
            bool varied = false;
            for (int i = 0; i < 300; i++)
            {
                var sample = player.Stats.CreateMonsterHit(100f);
                Assert(sample.Damage >= 90f && sample.Damage <= 110f && !sample.IsCritical, "난수 ±10% 범위 오류");
                varied |= sample.Damage != 100f;
            }
            Assert(varied, "난수 변동 없음");
            configure(0f, 0f);
            var attackComponent = player.GetComponent<PlayerAttack>();
            const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
            var normalAttack = AssetDatabase.LoadAssetAtPath<PlayerAttackData>("Assets/Data/Player/Attacks/Player_Attack1.asset");
            typeof(PlayerAttack).GetField("currentAttack", flags).SetValue(attackComponent, normalAttack);
            typeof(PlayerAttack).GetField("hitEventPending", flags).SetValue(attackComponent, true);
            var hitCenter = (Vector2)typeof(PlayerAttack).GetMethod("GetHitboxCenter", flags).Invoke(attackComponent, null);
            var collider = monster.GetComponent<Collider2D>();
            Vector2 colliderOffset = (Vector2)collider.bounds.center - (Vector2)monster.transform.position;
            monster.GetComponent<Rigidbody2D>().position = hitCenter - colliderOffset;
            Physics2D.SyncTransforms();
            hp = monster.Hp;
            attackComponent.ApplyAttackHit();
            Assert(monster.Hp == hp - Mathf.Round(normalAttack.Damage), "일반 공격 이벤트 컨텍스트 전달 실패");
            attackComponent.ApplyAttackHit();
            Assert(monster.Hp == hp - Mathf.Round(normalAttack.Damage), "공격 이벤트 중복 피해 발생");
            configure(100f, 0f);
            var parry = player.GetComponent<PlayerParry>();
            parry.CancelParry();
            typeof(PlayerParry).GetField("nextParryTime", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(parry, 0f);
            Assert(parry.TryStartParry(), "반격 패링 시작 실패");
            hp = monster.Hp;
            var incoming = AssetDatabase.LoadAssetAtPath<MonsterAttackData>("Assets/Data/Monster/Attacks/Goblin/Goblin_Attack1.asset");
            player.TakeDamage(new AttackHitContext(incoming, monster, 1));
            await Task.Delay(50);
            float counter = Mathf.Round(player.Stats.ParryCounterDamage * 1.5f);
            Assert(monster.Hp == hp - counter && DamageNumber.activeInstances.Any(n => n.number == counter), "패링 반격 치명타·출력 실패");
            await Task.Delay(1000);
            Assert(!normal.gameObject.activeSelf, "수명 종료 후 풀 반환 실패");
            var prefab = AssetDatabase.LoadAssetAtPath<DamageNumber>("Assets/Prefabs/UI/DamageNumbers/NormalDamage.prefab");
            int reused = 0;
            for (int i = 0; i < 24; i++)
                if (prefab.Spawn(origin, i + 1) == pooledNumber) reused++;
            Assert(reused == 1, "풀 객체 재사용 실패");
            return "PASS: normal/critical damage and colors, +/-10% variance, parry counter crit, motion, pool return and reuse.";
        }
        finally { configure(oldChance, oldVariance); }
    }
}


