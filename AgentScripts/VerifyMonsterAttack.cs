using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Parry.MonsterClass;
using Parry.PlayerClass;

public static class VerifyMonsterAttack
{
    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    public static async Task<string> Run()
    {
        Assert(Application.isPlaying, "Play 모드에서 실행하세요.");
        var brain = UnityEngine.Object.FindAnyObjectByType<MonsterBrain>();
        var player = brain.GetComponent<MonsterSensor>().Target;
        player.GetComponent<PlayerMovement>().enabled = false;
        var playerBody = player.GetComponent<Rigidbody2D>();
        var body = brain.GetComponent<Rigidbody2D>();
        playerBody.gravityScale = 0;
        body.gravityScale = 0;
        playerBody.linearVelocity = Vector2.zero;
        body.linearVelocity = Vector2.zero;
        body.position = new Vector2(0, 1000);
        playerBody.position = new Vector2(3f, 1000);
        Physics2D.SyncTransforms();
        float height = brain.GetComponent<Collider2D>().bounds.min.y - player.GetComponent<Collider2D>().bounds.min.y;
        playerBody.position += Vector2.up * height;
        Physics2D.SyncTransforms();
        await Task.Delay(100);
        Assert(brain.State is MonsterChaseState, "사거리 밖 추적 실패");
        float deadline = Time.time + 3f;
        while (!(brain.State is MonsterAttackState) && Time.time < deadline)
            await Task.Delay(20);
        Assert(brain.State is MonsterAttackState, "추적에서 공격으로 진입 실패");
        Assert(brain.Attack.CurrentAttack.name == "Goblin_Attack2", "사거리 조건에 맞는 공격 선택 실패");
        Assert(Mathf.Abs(body.linearVelocity.x) < 0.01f, "공격 중 이동");
        int direction = brain.Movement.FacingDirection;
        playerBody.position = new Vector2(body.position.x - 0.4f, playerBody.position.y);
        Physics2D.SyncTransforms();
        await Task.Delay(200);
        Assert(brain.Movement.FacingDirection == direction, "공격 중 방향 변경");
        await Task.Delay(800);
        Assert(brain.State is MonsterAttackState && !brain.Attack.IsMotionPlaying, "모션 이후 회복 대기 실패");
        await Task.Delay(650);
        Assert(brain.State is MonsterAttackState, "회복 후 다음 공격 진입 실패");
        Assert(brain.Attack.CurrentAttack.name == "Goblin_Attack1", "공격 2의 쿨타임 동안 공격 1 선택 실패");
        playerBody.position = new Vector2(20, playerBody.position.y);
        Physics2D.SyncTransforms();
        await Task.Delay(150);
        Assert(brain.State is MonsterAttackState, "진행 중 공격을 전투 종료로 중단함");
        await Task.Delay(1500);
        Assert(brain.State is MonsterWanderState, "회복 후 전투 종료 실패");
        Assert(!brain.Attack.IsMotionPlaying && brain.Attack.CurrentAttack == null, "공격 종료 정리 실패");
        await Task.Delay(1000);
        playerBody.position = new Vector2(body.position.x + 0.4f, playerBody.position.y);
        Physics2D.SyncTransforms();
        await Task.Delay(150);
        Assert(brain.State is MonsterAttackState, "재감지 후 공격 진입 실패");
        brain.enabled = false;
        await VerifyCooldowns(brain.Attack);
        brain.gameObject.SetActive(false);
        Assert(brain.State == null && !brain.Attack.IsMotionPlaying, "비활성화 시 공격 정리 실패");
        return "PASS: range selection, stationary attack, fixed facing, clip completion, end delay, all attacks on cooldown, independent cooldowns, cooldown expiry, zero cooldown reuse, deferred combat exit, playback cleanup.";
    }

    private static async Task VerifyCooldowns(MonsterAttack attack)
    {
        var first = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<MonsterAttackData>("Assets/Data/Monster/Attacks/Goblin/Goblin_Attack1.asset"));
        var second = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<MonsterAttackData>("Assets/Data/Monster/Attacks/Goblin/Goblin_Attack2.asset"));
        try
        {
            SetCooldown(first, 1f);
            SetCooldown(second, 1f);
            attack.Initialize(new List<MonsterAttackData> { first, second });
            Assert(attack.TryStartAttack(0f), "첫 공격 실행 실패");
            var selected = attack.CurrentAttack;
            attack.Stop();
            Assert(attack.TryStartAttack(0f), "다른 공격의 독립 쿨타임 실패");
            Assert(attack.CurrentAttack != selected, "쿨타임 중인 공격 재선택");
            attack.Stop();
            Assert(!attack.TryStartAttack(0f), "모든 공격이 쿨타임 중인데 공격 시작");
            float deadline = Time.time + 1.1f;
            while (Time.time < deadline)
                await Task.Delay(20);
            Assert(attack.TryStartAttack(0f), "쿨타임 만료 후 재사용 실패");
            attack.Stop();

            SetCooldown(first, 0f);
            attack.Initialize(new List<MonsterAttackData> { first });
            Assert(attack.TryStartAttack(0f), "쿨타임 0 공격 시작 실패");
            attack.Stop();
            Assert(attack.TryStartAttack(0f), "쿨타임 0 공격 즉시 재사용 실패");
        }
        finally
        {
            attack.Stop();
            UnityEngine.Object.DestroyImmediate(first);
            UnityEngine.Object.DestroyImmediate(second);
        }
    }

    private static void SetCooldown(MonsterAttackData attack, float duration)
    {
        var data = new SerializedObject(attack);
        data.FindProperty("cooldown").floatValue = duration;
        data.ApplyModifiedPropertiesWithoutUndo();
    }
}
