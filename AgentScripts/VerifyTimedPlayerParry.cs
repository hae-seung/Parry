using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.InputSystem;
using Parry.AnimationClass;
using Parry.MonsterClass;
using Parry.PlayerClass;

public static class VerifyTimedPlayerParry
{
    private static readonly BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static async Task WaitGameTime(float duration)
    {
        float end = Time.time + duration;
        while (Time.time < end)
            await Task.Delay(10);
    }

    private static async Task WaitMotion(AnimationClipPlayer player)
    {
        float deadline = Time.realtimeSinceStartup + 4f;
        while (player.IsPlaying && Time.realtimeSinceStartup < deadline)
            await Task.Delay(10);
        Assert(!player.IsPlaying, "클립 종료 시간 초과");
    }

    private static async Task WaitRecovery(PlayerCharacter character)
    {
        var reaction = character.GetComponent<PlayerHitReaction>();
        float deadline = Time.realtimeSinceStartup + 8f;
        while ((reaction.IsReacting || reaction.IsInvulnerable) && Time.realtimeSinceStartup < deadline)
            await Task.Delay(10);
        Assert(!reaction.IsReacting && !reaction.IsInvulnerable, "피격 회복 대기 시간 초과");
    }

    public static async Task<string> Run()
    {
        Assert(Application.isPlaying, "Play 모드에서 실행하세요.");
        var brain = UnityEngine.Object.FindAnyObjectByType<MonsterBrain>();
        var character = brain.GetComponent<MonsterSensor>().Target;
        var parry = character.GetComponent<PlayerParry>();
        var stats = character.Stats;
        var playerAttack = character.GetComponent<PlayerAttack>();
        var clips = character.GetComponent<AnimationClipPlayer>();
        var parryData = new SerializedObject(parry);
        var parryClip = (AnimationClip)parryData.FindProperty("parryClip").objectReferenceValue;
        Assert(parryClip != null, "패링 클립 연결 누락");
        Assert(character.GetComponent<PlayerInput>().actions.FindAction("Player/Parry").bindings.Any(binding => binding.path == "<Keyboard>/space"), "스페이스바 패링 바인딩 누락");
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Animations/Player/PlayerAnimController.controller");
        Assert(!controller.parameters.Any(parameter => parameter.name == "IsParrying"), "패링 Animator 파라미터가 남아 있음");
        Assert(!controller.layers[0].stateMachine.states.Any(state => state.state.name == "Player_Parry"), "패링 Animator 상태가 남아 있음");
        Assert(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/Player.prefab").GetComponent<PlayerParry>() != null, "프리팹에 시간 기반 패링 누락");

        brain.enabled = false;
        character.GetComponent<PlayerMovement>().enabled = false;
        var body = brain.GetComponent<Rigidbody2D>();
        var playerBody = character.GetComponent<Rigidbody2D>();
        body.gravityScale = playerBody.gravityScale = 0f;
        body.linearVelocity = playerBody.linearVelocity = Vector2.zero;
        body.position = new Vector2(0f, 1000f);
        playerBody.position = new Vector2(1f, 1000f);
        Physics2D.SyncTransforms();
        playerBody.position += new Vector2(0.9f, 1000f) - (Vector2)character.GetComponent<Collider2D>().bounds.center;
        Physics2D.SyncTransforms();
        brain.Movement.Face(playerBody.position.x);
        var attack = brain.Attack;
        var testAttack = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<MonsterAttackData>("Assets/Data/Monster/Attacks/Goblin/Goblin_Attack2.asset"));
        try
        {
            var attackData = new SerializedObject(testAttack);
            attackData.FindProperty("cooldown").floatValue = 0f;
            attackData.FindProperty("attackType").enumValueIndex = (int)MonsterAttackType.Normal;
            attackData.FindProperty("knocksDown").boolValue = false;
            attackData.FindProperty("hitboxOffset").vector2Value = new Vector2(0.9f, 0f);
            attackData.FindProperty("hitboxSize").vector2Value = new Vector2(2f, 2f);
            attackData.ApplyModifiedPropertiesWithoutUndo();
            attack.Initialize(new List<MonsterAttackData> { testAttack });

            float hp = stats.Hp;
            Assert(attack.TryStartAttack(0f), "기준 공격 시작 실패");
            await WaitMotion(brain.GetComponent<AnimationClipPlayer>());
            Assert(Mathf.Approximately(stats.Hp, hp - testAttack.Damage), "패링 없는 타격의 피해 실패");
            attack.Stop();
            await WaitRecovery(character);

            parryData.FindProperty("parryDuration").floatValue = 0.9f;
            parryData.ApplyModifiedPropertiesWithoutUndo();
            character.Parry();
            Assert(character.IsParrying && clips.IsPlayingClip(parryClip), "패링 입력 진입 또는 클립 재생 실패");
            float endTime = (float)typeof(PlayerParry).GetField("parryEndTime", Private).GetValue(parry);
            Assert(!parry.TryStartParry(), "패링 중 중복 시작 허용");
            Assert((float)typeof(PlayerParry).GetField("parryEndTime", Private).GetValue(parry) == endTime, "중복 입력으로 유효 시간 연장");
            Assert(attack.TryStartAttack(0f), "패링 성공 검사 공격 시작 실패");
            await WaitGameTime(parryClip.length + 0.07f);
            Assert(!clips.IsPlaying && parry.IsParrying, "클립 종료가 패링 유효 시간을 종료함");
            hp = stats.Hp;
            await WaitMotion(brain.GetComponent<AnimationClipPlayer>());
            Assert(stats.Hp == hp, "실제 타격 이벤트를 패링하지 못함");
            attack.Stop();
            await WaitGameTime(0.95f);
            Assert(!parry.IsParrying && !character.IsParrying, "시간 만료 후 패링 유지");

            // 플레이어 피격 진입점에서 성공 시 0.2초, 시도 시 3초 쿨타임을 적용한다.
            Assert(parry.TryStartParry(), "플레이어 피격 진입점 검사 시작 실패");
            float startCooldownEnd = (float)typeof(PlayerParry).GetField("nextParryTime", Private).GetValue(parry);
            Assert(Mathf.Abs(startCooldownEnd - Time.time - 3f) < 0.01f, "패링 시도 쿨타임이 3초가 아님");
            hp = stats.Hp;
            character.TakeDamage(testAttack.Damage);
            Assert(stats.Hp == hp && parry.IsParrying, "플레이어 피격 진입점 패링 실패 또는 유효 시간 종료");
            float successCooldownEnd = (float)typeof(PlayerParry).GetField("nextParryTime", Private).GetValue(parry);
            Assert(Mathf.Abs(successCooldownEnd - Time.time - 0.2f) < 0.01f, "성공 쿨타임이 0.2초가 아님");
            parry.CancelParry();
            Assert(!parry.TryStartParry(), "성공 직후 쿨타임 중 재입력 허용");
            await WaitGameTime(0.21f);
            Assert(parry.TryStartParry(), "성공 후 0.2초 경과 시 재사용 실패");
            parry.CancelParry();
            Assert(!parry.TryStartParry(), "실패한 시도의 3초 쿨타임 무시");
            await WaitGameTime(3.02f);

            // Update가 종료 처리를 하기 전 타격이 들어와도 만료된 패링은 실패해야 한다.
            Assert(parry.TryStartParry(), "만료 검사 패링 시작 실패");
            typeof(PlayerParry).GetField("parryEndTime", Private).SetValue(parry, Time.time - 0.01f);
            Assert(attack.TryStartAttack(0f), "만료 검사 공격 시작 실패");
            hp = stats.Hp;
            attack.ApplyAttackHit();
            Assert(Mathf.Approximately(stats.Hp, hp - testAttack.Damage), "Update 전 만료된 패링이 타격을 막음");
            attack.Stop();
            await WaitRecovery(character);

            // 만료 후 새 공격이 시작되면 뒤늦은 패링 정리가 그 클립을 끊으면 안 된다.
            typeof(PlayerAttack).GetMethod("StartAttack", Private).Invoke(playerAttack, new object[] { 0 });
            typeof(PlayerParry).GetMethod("Update", Private).Invoke(parry, null);
            Assert(playerAttack.IsAttacking && clips.IsPlaying, "패링 정리가 다음 공격 클립을 끊음");
            typeof(PlayerParry).GetField("nextParryTime", Private).SetValue(parry, Time.time);
            Assert(parry.TryStartParry() && !playerAttack.IsAttacking, "일반 공격을 패링으로 취소 실패");

            parry.enabled = false;
            Assert(!parry.IsParrying && !clips.IsPlayingClip(parryClip), "비활성화 시 패링 정리 실패");
            parry.enabled = true;
            Assert(!parry.IsParrying, "재활성화 시 패링이 남음");
            Assert(!parry.TryStartParry(), "재활성화로 쿨타임 초기화");
            typeof(PlayerParry).GetField("nextParryTime", Private).SetValue(parry, Time.time);
            Assert(parry.TryStartParry(), "사망 검사 패링 시작 실패");
            stats.TakeDamage(99999f);
            Assert(!parry.IsParrying && character.IsDead, "사망 후 패링 유지");
            Assert(!parry.TryStartParry(), "사망 후 패링 시작 허용");
            return "PASS: player-owned hit/parry decision, real monster events, active window retained on success, 3s attempt cooldown, 0.2s success cooldown, double-input rejection, timed expiry, attack cancellation, clip ownership, disable/reenable, death.";
        }
        finally
        {
            attack.Stop();
            parry.CancelParry();
            UnityEngine.Object.DestroyImmediate(testAttack);
        }
    }
}
