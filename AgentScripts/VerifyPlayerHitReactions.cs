using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Parry.CombatClass;
using Parry.AnimationClass;
using Parry.MonsterClass;
using Parry.PlayerClass;

public static class VerifyPlayerHitReactions
{
    private static readonly BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    private static void Assert(bool value, string message)
    {
        if (!value) throw new Exception(message);
    }
    private static async Task Wait(float duration)
    {
        float end = Time.time + duration;
        while (Time.time < end) await Task.Delay(10);
    }
    private static async Task WaitReaction(PlayerHitReaction reaction)
    {
        float deadline = Time.realtimeSinceStartup + 8f;
        while (reaction.IsReacting && Time.realtimeSinceStartup < deadline) await Task.Delay(10);
        Assert(!reaction.IsReacting, "피격 반응 종료 시간 초과");
    }
    private static MonsterAttackData Attack(string path, MonsterAttackType type)
    {
        var copy = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<MonsterAttackData>(path));
        var data = new SerializedObject(copy);
        data.FindProperty("attackType").enumValueIndex = (int)type;
        data.FindProperty("damage").floatValue = 1f;
        data.ApplyModifiedPropertiesWithoutUndo();
        return copy;
    }

    public static async Task<string> Run()
    {
        Assert(Application.isPlaying, "Play 모드에서 실행하세요.");
        var brain = UnityEngine.Object.FindAnyObjectByType<MonsterBrain>();
        brain.enabled = false;
        var source = brain.GetComponent<Monster>();
        var character = brain.GetComponent<MonsterSensor>().Target;
        var body = character.GetComponent<Rigidbody2D>();
        var reaction = character.GetComponent<PlayerHitReaction>();
        var clips = character.GetComponent<AnimationClipPlayer>();
        var attack = character.GetComponent<PlayerAttack>();
        var parry = character.GetComponent<PlayerParry>();
        body.position = new Vector2(0f, 1000f);
        body.linearVelocity = Vector2.zero;
        character.Move(0f);
        brain.GetComponent<Rigidbody2D>().gravityScale = 0f;
        brain.GetComponent<Rigidbody2D>().position = new Vector2(-4f, 1000f);
        Physics2D.SyncTransforms();
        var floor = new GameObject("TemporaryHitReactionFloor");
        floor.transform.position = new Vector3(0f, character.GetComponent<Collider2D>().bounds.min.y - 0.5f, 0f);
        floor.AddComponent<BoxCollider2D>().size = new Vector2(100f, 1f);
        var wall = new GameObject("TemporaryHitReactionWall");
        wall.SetActive(false);
        var normal = Attack("Assets/Data/Monster/Attacks/Goblin/Goblin_Attack1.asset", MonsterAttackType.Normal);
        var heavy = Attack("Assets/Data/Monster/Attacks/Goblin/Goblin_Attack2.asset", MonsterAttackType.Heavy);
        var down = Attack("Assets/Data/Monster/Attacks/Goblin/Goblin_Attack2.asset", MonsterAttackType.Heavy);
        var noPush = Attack("Assets/Data/Monster/Attacks/Goblin/Goblin_Attack1.asset", MonsterAttackType.Normal);
        var noKnockback = Attack("Assets/Data/Monster/Attacks/Goblin/Goblin_Attack1.asset", MonsterAttackType.Normal);
        var noPushData = new SerializedObject(noPush);
        noPushData.FindProperty("knockbackDistance").floatValue = 0f;
        noPushData.ApplyModifiedPropertiesWithoutUndo();
        var noKnockbackData = new SerializedObject(noKnockback);
        noKnockbackData.FindProperty("canKnockback").boolValue = false;
        noKnockbackData.ApplyModifiedPropertiesWithoutUndo();
        try
        {
            var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
            var hurtClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animations/Player/Player_Hurt.anim");
            var downClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animations/Player/Player_Knockdown.anim");
            var upClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animations/Player/Player_GetUp.anim");
            var hurtKeys = AnimationUtility.GetObjectReferenceCurve(hurtClip, binding);
            Assert(hurtKeys.Length == 2 && hurtKeys[0].value.name == "HeroKnight_46" && hurtKeys[1].value.name == "HeroKnight_47", "넉백 클립에 흰색 또는 잘못된 프레임 포함");
            Assert(AnimationUtility.GetObjectReferenceCurve(downClip, binding).Last().value.name == "HeroKnight_DeathNoBlood_5", "기절이 무릎 꿇기에서 끝나지 않음");
            Assert(AnimationUtility.GetObjectReferenceCurve(upClip, binding).Last().value.name == "HeroKnight_DeathNoBlood_3", "기상 마지막 프레임이 서는 자세가 아님");

            Physics2D.SyncTransforms();
            await Wait(0.15f);
            Assert(character.IsGrounded, "피격 검증용 바닥 접촉 실패");
            Assert(attack.TryStartAttack(), "피격 취소 검사용 일반 공격 시작 실패");
            float hp = character.Stats.Hp;
            float startX = body.position.x;
            character.TakeDamage(new AttackHitContext(normal, source, 1));
            Assert(character.Stats.Hp == hp - 1f && character.IsHitReacting && !attack.IsAttacking, "일반 피격 또는 공격 취소 실패");
            Assert(!(bool)typeof(PlayerAttack).GetField("hitEventPending", Private).GetValue(attack), "취소한 공격의 판정이 남음");
            character.Attack(); character.Parry(); character.Roll(); character.Jump();
            Assert(!attack.IsAttacking && !parry.IsParrying && !character.IsRolling, "넉백 중 행동 허용");
            await WaitReaction(reaction);
            float normalDistance = body.position.x - startX;
            Assert(normalDistance > 0.05f && normalDistance < 0.45f, "일반 공격 작은 밀림 실패: " + normalDistance);
            float protectedHp = character.Stats.Hp;
            float protectedUntil = (float)typeof(PlayerHitReaction).GetField("hitInvulnerabilityEndTime", Private).GetValue(reaction);
            character.TakeDamage(new AttackHitContext(down, source, 1));
            Assert(character.Stats.Hp == protectedHp && !reaction.IsReacting, "피격 무적 중 피해 또는 추가 경직 발생");
            Assert((float)typeof(PlayerHitReaction).GetField("hitInvulnerabilityEndTime", Private).GetValue(reaction) == protectedUntil, "추가 타격으로 무적 연장");
            await Wait(0.18f);

            startX = body.position.x;
            Assert(attack.TryStartAttack(), "강공격 검사용 일반 공격 시작 실패");
            character.TakeDamage(new AttackHitContext(heavy, source, 1));
            Assert(!attack.IsAttacking && reaction.IsReacting && !reaction.IsFainted, "강공격 공격 취소 실패");
            await WaitReaction(reaction);
            float heavyDistance = body.position.x - startX;
            Assert(heavyDistance > normalDistance * 1.8f, "강공격 밀림이 일반 공격보다 충분히 크지 않음");
            Assert(!reaction.IsFainted, "기절 종료 후 상태가 유지됨");
            await Wait(0.18f);

            character.TakeDamage(new AttackHitContext(down, source, 1));
            hp = character.Stats.Hp;
            bool sawDown = false;
            bool sawGetUp = false;
            float deadline = Time.realtimeSinceStartup + 8f;
            while (reaction.IsReacting && Time.realtimeSinceStartup < deadline)
            {
                sawDown |= reaction.IsFainted;
                sawGetUp |= clips.IsPlayingClip(upClip);
                Assert(reaction.IsInvulnerable, "기절 또는 기상 도중 무적 해제");
                character.TakeDamage(new AttackHitContext(normal, source, 1));
                character.Attack(); character.Parry(); character.Roll(); character.Jump();
                Assert(character.Stats.Hp == hp && !attack.IsAttacking && !parry.IsParrying && !character.IsRolling, "기절 또는 기상 중 타격/입력 허용");
                await Task.Delay(10);
            }
            Assert(!reaction.IsReacting && sawDown && sawGetUp, "무릎 꿇기/대기/자동 기상 흐름 실패");
            Assert(!clips.IsPlayingClip(upClip) && !reaction.IsInvulnerable, "완전 기상 후 정리 실패");

            wall.transform.position = new Vector3(body.position.x + 0.85f, body.position.y, 0f);
            wall.AddComponent<BoxCollider2D>().size = new Vector2(0.2f, 10f);
            wall.SetActive(true);
            Physics2D.SyncTransforms();
            character.TakeDamage(new AttackHitContext(heavy, source, 1));
            await WaitReaction(reaction);
            Assert(character.GetComponent<Collider2D>().bounds.max.x <= wall.GetComponent<Collider2D>().bounds.min.x + 0.05f, "밀림으로 벽 관통");
            await Wait(0.18f);
            wall.SetActive(false);

            // 밀림이 0이어도 넉백이 켜져 있으면 이동과 모든 행동 입력을 막는다.
            startX = body.position.x;
            hp = character.Stats.Hp;
            character.TakeDamage(new AttackHitContext(noPush, source, 1));
            Assert(character.IsHitReacting && character.Stats.Hp == hp - 1f, "밀림 없는 넉백 적용 실패");
            character.Move(1f);
            character.Jump(); character.Attack(); character.Parry(); character.Roll();
            await Wait(0.08f);
            Assert(Mathf.Abs(body.position.x - startX) < 0.03f, "넉백 중 이동 입력 적용 또는 0 거리 밀림 발생");
            Assert(!attack.IsAttacking && !parry.IsParrying && !character.IsRolling, "밀림 없는 넉백 중 행동 허용");
            character.Move(0f);
            await WaitReaction(reaction);
            await Wait(0.18f);

            // 넉백을 끄면 기존 공격을 유지하면서 데이터에 설정한 거리만큼 밀린다.
            Assert(attack.TryStartAttack(), "넉백 없는 타격 검사용 공격 시작 실패");
            startX = body.position.x;
            character.TakeDamage(new AttackHitContext(noKnockback, source, 1));
            Assert(!character.IsHitReacting && attack.IsAttacking && !clips.IsPlayingClip(hurtClip), "넉백을 끈 타격이 공격을 중단함");
            await Wait(0.24f);
            Assert(body.position.x - startX > 0.05f && attack.IsAttacking, "넉백 없는 밀림 또는 공격 유지 실패");
            attack.CancelAttack();
            await Wait(0.18f);

            noKnockbackData.Update();
            noKnockbackData.FindProperty("knockbackDistance").floatValue = 0f;
            noKnockbackData.ApplyModifiedPropertiesWithoutUndo();
            Assert(attack.TryStartAttack(), "넉백·밀림 없는 타격 검사용 공격 시작 실패");
            startX = body.position.x;
            character.TakeDamage(new AttackHitContext(noKnockback, source, 1));
            await Wait(0.24f);
            Assert(!character.IsHitReacting && attack.IsAttacking && Mathf.Abs(body.position.x - startX) < 0.03f, "넉백·밀림을 모두 끈 타격이 행동이나 위치를 변경함");
            attack.CancelAttack();
            await Wait(0.18f);

            character.TakeDamage(new AttackHitContext(down, source, 1));
            character.gameObject.SetActive(false);
            Assert(!reaction.IsReacting && !reaction.IsInvulnerable && !clips.IsPlaying, "비활성화 시 피격 정리 실패");
            character.gameObject.SetActive(true);
            Assert(!character.IsHitReacting, "재활성화 후 피격 잠금 유지");
            character.Stats.TakeDamage(99999f);
            Assert(character.IsDead && !reaction.IsReacting && !clips.IsPlaying, "치명타가 피격 모션으로 사망을 덮음");
            return $"PASS: nonwhite hurt frames, kneel/get-up clips, both attacks cancel attacks, normal push {normalDistance:0.000}, heavy push {heavyDistance:0.000}, input lock, no repeated damage/iframe extension, mandatory heavy faint, complete get-up immunity, wall collision, disable/reenable, death.";
        }
        finally
        {
            reaction.CancelReaction();
            UnityEngine.Object.DestroyImmediate(floor);
            UnityEngine.Object.DestroyImmediate(wall);
            UnityEngine.Object.DestroyImmediate(normal);
            UnityEngine.Object.DestroyImmediate(heavy);
            UnityEngine.Object.DestroyImmediate(down);
            UnityEngine.Object.DestroyImmediate(noPush);
            UnityEngine.Object.DestroyImmediate(noKnockback);
        }
    }
}

