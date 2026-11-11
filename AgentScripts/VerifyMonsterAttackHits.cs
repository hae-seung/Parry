using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Parry.MonsterClass;
using Parry.PlayerClass;

public static class VerifyMonsterAttackHits
{
    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    public static async Task<string> Run()
    {
        Assert(Application.isPlaying, "Play 모드에서 실행하세요.");
        var brain = UnityEngine.Object.FindAnyObjectByType<MonsterBrain>();
        var attack = brain.Attack;
        var player = brain.GetComponent<MonsterSensor>().Target;
        var stats = player.Stats;
        brain.enabled = false;
        player.GetComponent<PlayerMovement>().enabled = false;
        var body = brain.GetComponent<Rigidbody2D>();
        var playerBody = player.GetComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        playerBody.gravityScale = 0f;
        body.linearVelocity = Vector2.zero;
        playerBody.linearVelocity = Vector2.zero;
        body.position = new Vector2(0f, 1000f);
        playerBody.position = new Vector2(1f, 1000f);
        Physics2D.SyncTransforms();
        var monsterCollider = brain.GetComponent<Collider2D>();
        var playerCollider = player.GetComponent<Collider2D>();
        Vector2 targetCenter = (Vector2)monsterCollider.bounds.center + Vector2.right * 0.9f;
        playerBody.position += targetCenter - (Vector2)playerCollider.bounds.center;
        Physics2D.SyncTransforms();
        brain.Movement.Face(playerBody.position.x);
        var extraCollider = new GameObject("TemporaryExtraPlayerCollider");
        extraCollider.layer = LayerMask.NameToLayer("Player");
        extraCollider.transform.SetParent(player.transform);
        extraCollider.transform.position = playerCollider.bounds.center;
        extraCollider.AddComponent<BoxCollider2D>().isTrigger = true;
        Physics2D.SyncTransforms();

        var first = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<MonsterAttackData>("Assets/Data/Monster/Attacks/Goblin/Goblin_Attack1.asset"));
        var second = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<MonsterAttackData>("Assets/Data/Monster/Attacks/Goblin/Goblin_Attack2.asset"));
        try
        {
            foreach (var data in new List<MonsterAttackData> { first, second })
            {
                attack.Initialize(new List<MonsterAttackData> { data });
                float hp = stats.Hp;
                float eventTime = AnimationUtility.GetAnimationEvents(data.AnimationClip)[0].time;
                Assert(attack.TryStartAttack(0f), "공격 시작 실패");
                await Task.Delay(Mathf.RoundToInt(Mathf.Min(0.15f, eventTime * 0.5f) * 1000f));
                Assert(stats.Hp == hp, "이벤트 전에 피해 발생");
                // 실제 애니메이션 평가가 이벤트를 전달할 때까지 기다린다.
                float hitDeadline = Time.time + 2f;
                while (stats.Hp == hp && Time.time < hitDeadline)
                    await Task.Delay(20);
                Assert(Mathf.Approximately(stats.Hp, hp - data.Damage), $"실제 애니메이션 이벤트 피해 실패: {data.name}, before {hp}, after {stats.Hp}, expected {data.Damage}");
                attack.ApplyAttackHit();
                attack.ApplyAttackHit();
                Assert(Mathf.Approximately(stats.Hp, hp - data.Damage), "중복 이벤트 또는 콜라이더 중복 피해");
                attack.Stop();
            }

            attack.Initialize(new List<MonsterAttackData> { first });
            float beforeMiss = stats.Hp;
            playerBody.position += Vector2.right * 10f;
            Physics2D.SyncTransforms();
            Assert(attack.TryStartAttack(0f), "빗나감 검사 공격 시작 실패");
            await WaitForMotionEnd(attack);
            Assert(stats.Hp == beforeMiss, "범위 밖 플레이어 피격");
            attack.Stop();

            playerBody.position += Vector2.left * 10f;
            Physics2D.SyncTransforms();
            Assert(attack.TryStartAttack(0f), "공격 취소 검사 시작 실패");
            attack.Stop();
            attack.ApplyAttackHit();
            await Task.Delay(1000);
            Assert(stats.Hp == beforeMiss, "취소한 공격에서 피해 발생");

            // 반대쪽을 향해 공격할 때 판정 중심도 반전되는지 확인한다.
            playerBody.position += Vector2.left * 1.8f;
            Physics2D.SyncTransforms();
            brain.Movement.Face(playerBody.position.x);
            Assert(attack.TryStartAttack(0f), "왼쪽 공격 시작 실패");
            await WaitForMotionEnd(attack);
            Assert(Mathf.Approximately(stats.Hp, beforeMiss - first.Damage), "왼쪽 공격 판정 반전 실패");
            attack.Stop();

            stats.TakeDamage(stats.Hp - 5f);
            Assert(attack.TryStartAttack(0f), "사망 검사 공격 시작 실패");
            await WaitForMotionEnd(attack);
            await Task.Delay(50);
            Assert(stats.Hp == 0f && player.IsDead, "타격에 의한 플레이어 사망 실패");
            Assert(player.GetComponent<Animator>().GetBool("IsDead"), "사망 애니메이션 전달 실패");
            return "PASS: both real clip events, no early damage, one hit per attack with multiple colliders, miss outside hitbox, cancellation, left-facing hit, HP zero and death animation.";
        }
        finally
        {
            attack.Stop();
            UnityEngine.Object.DestroyImmediate(extraCollider);
            UnityEngine.Object.DestroyImmediate(first);
            UnityEngine.Object.DestroyImmediate(second);
        }
    }

    private static async Task WaitForMotionEnd(MonsterAttack attack)
    {
        float deadline = Time.realtimeSinceStartup + 5f;
        while (attack.IsMotionPlaying && Time.realtimeSinceStartup < deadline)
            await Task.Delay(20);
        Assert(!attack.IsMotionPlaying, "클립 종료 대기 시간 초과");
    }
}
