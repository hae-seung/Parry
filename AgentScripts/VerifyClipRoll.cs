using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using Parry.PlayerClass;
using Parry.MonsterClass;
using Parry.AnimationClass;

public static class VerifyClipRoll
{
    private static void Assert(bool value, string message)
    {
        if (!value) throw new Exception(message);
    }

    public static async Task<string> Run()
    {
        Assert(Application.isPlaying, "Play 모드 필요");
        foreach (var brain in UnityEngine.Object.FindObjectsByType<MonsterBrain>())
            brain.enabled = false;
        var player = UnityEngine.Object.FindAnyObjectByType<PlayerCharacter>();
        var body = player.GetComponent<Rigidbody2D>();
        var movement = player.GetComponent<PlayerMovement>();
        var clips = player.GetComponent<AnimationClipPlayer>();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var rollClip = (AnimationClip)typeof(PlayerCharacter).GetField("rollClip", flags).GetValue(player);
        Assert(rollClip != null && !rollClip.isLooping, "구르기 클립 연결 또는 반복 설정 오류");
        var floor = new GameObject("RollTestFloor");
        body.position = new Vector2(0f, 1000f);
        body.linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();
        floor.transform.position = new Vector3(0f, player.GetComponent<Collider2D>().bounds.min.y - 0.5f, 0f);
        floor.AddComponent<BoxCollider2D>().size = new Vector2(100f, 1f);
        player.Move(0f);
        try
        {
            await Task.Delay(500);
            Assert(player.IsGrounded, "검증용 바닥 착지 실패");
            float startX = body.position.x;
            player.Roll();
            Assert(player.IsRolling && clips.IsPlayingClip(rollClip), "클립 구르기 시작 실패");
            await Task.Delay(80);
            float rollSpeed = (float)typeof(PlayerMovement).GetField("rollSpeed", flags).GetValue(movement);
            Assert(Mathf.Abs(Mathf.Abs(body.linearVelocity.x) - rollSpeed) < 0.01f, "구르기 이동 속도 불일치");
            float deadline = Time.realtimeSinceStartup + 4f;
            while (player.IsRolling && Time.realtimeSinceStartup < deadline) await Task.Delay(10);
            Assert(!player.IsRolling && !clips.IsPlayingClip(rollClip), "클립 종료 후 구르기 유지");
            Assert(Mathf.Abs(body.position.x - startX) > 0.1f, "구르기 이동 없음");
            await Task.Delay(400);
            player.Roll();
            Assert(player.IsRolling, "쿨타임 후 재시작 실패");
            player.gameObject.SetActive(false);
            Assert(!player.IsRolling && !clips.IsPlayingClip(rollClip), "비활성화 시 구르기 유지");
            player.gameObject.SetActive(true);
            return "PASS: one-shot roll clip, configured movement speed, automatic end, cooldown retry, disable cleanup.";
        }
        finally { UnityEngine.Object.Destroy(floor); }
    }
}
