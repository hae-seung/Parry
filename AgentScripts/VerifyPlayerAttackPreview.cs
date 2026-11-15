using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Parry.PlayerClass;

public static class VerifyPlayerAttackPreview
{
    private static readonly BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    private static T Field<T>(object instance, string name) => (T)instance.GetType().GetField(name, Private).GetValue(instance);
    private static object Call(object instance, string name, params object[] args) => instance.GetType().GetMethod(name, Private).Invoke(instance, args);
    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    public static async Task<string> Run()
    {
        Assert(!Application.isPlaying, "편집 모드에서 실행하세요.");
        Selection.activeGameObject = UnityEngine.Object.FindAnyObjectByType<PlayerAttack>().gameObject;
        MonsterAttackPreviewWindow.Open();
        await Task.Delay(200);
        var window = Resources.FindObjectsOfTypeAll<MonsterAttackPreviewWindow>().First();
        var player = UnityEngine.Object.FindAnyObjectByType<PlayerAttack>();
        Vector3 originalPosition = player.transform.position;
        Sprite originalSprite = player.GetComponent<SpriteRenderer>().sprite;
        var dropdown = window.rootVisualElement.Q<DropdownField>();
        Assert(dropdown.choices.Count == 3, "플레이어 공격 목록 실패");
        Vector3 position = Field<GameObject>(window, "preview").transform.position + Vector3.up;
        Call(window, "SetPreviewPosition", position);
        var initialAttack = Field<PlayerAttackData>(window, "selectedAttack");
        Vector3 expectedCenter = position + (Vector3)initialAttack.HitboxOffset;
        Assert((Vector3)Call(window, "GetHitboxCenter") == expectedCenter, "플레이어 판정이 원점 기준과 다름");
        foreach (int index in Enumerable.Range(0, 3))
        {
            Call(window, "SelectAttack", index);
            var attack = Field<PlayerAttackData>(window, "selectedAttack");
            Call(window, "GoToHitEvent");
            float time = Field<float>(window, "previewTime");
            float eventTime = AnimationUtility.GetAnimationEvents(attack.AnimationClip).First(evt => evt.functionName == "ApplyAttackHit").time;
            Assert(Mathf.Abs(time - eventTime) <= 1f / attack.AnimationClip.frameRate, "플레이어 타격 프레임 이동 실패");
            var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
            var sprite = AnimationUtility.GetObjectReferenceCurve(attack.AnimationClip, binding).Last(key => key.time <= time + 0.0001f).value;
            Assert(Field<SpriteRenderer>(window, "previewRenderer").sprite == sprite, "플레이어 공격 프레임 표시 실패");
            Assert(Field<GameObject>(window, "preview").transform.position == position, "공격 변경 시 위치 초기화");
        }

        Call(window, "SetTime", 0f);
        Call(window, "TogglePlayback");
        await Task.Delay(160);
        Assert(Field<float>(window, "previewTime") > 0f, "플레이어 공격 재생 실패");
        Call(window, "TogglePlayback");
        float paused = Field<float>(window, "previewTime");
        await Task.Delay(80);
        Assert(Field<float>(window, "previewTime") == paused, "플레이어 일시정지 실패");
        var originalAttack = Field<PlayerAttackData>(window, "selectedAttack");
        var copy = UnityEngine.Object.Instantiate(originalAttack);
        try
        {
            window.GetType().GetField("selectedAttack", Private).SetValue(window, copy);
            Call(window, "WriteHitbox", new Vector2(1.23f, 0.45f), new Vector2(2.5f, 1.5f));
            Assert(copy.HitboxOffset == new Vector2(1.23f, 0.45f) && copy.HitboxSize == new Vector2(2.5f, 1.5f), "플레이어 판정 편집 실패");
        }
        finally
        {
            window.GetType().GetField("selectedAttack", Private).SetValue(window, originalAttack);
            UnityEngine.Object.DestroyImmediate(copy);
        }

        Call(window, "StopPreview");
        Assert(!AnimationMode.InAnimationMode(), "플레이어 미리보기 종료 실패");
        Assert(player.transform.position == originalPosition && player.GetComponent<SpriteRenderer>().sprite == originalSprite, "플레이어 원본 변경");
        Call(window, "SelectAttack", 0);
        Call(window, "GoToHitEvent");
        window.Focus();
        return "PASS: three player attacks, real hit-event frames and sprites, transform-origin hitbox, position retention, playback/pause, data editing, cleanup. Player Attack1 hit frame ready.";
    }
}
