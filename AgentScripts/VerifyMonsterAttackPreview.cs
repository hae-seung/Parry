using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Parry.MonsterClass;

public static class VerifyMonsterAttackPreview
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
        MonsterAttackPreviewWindow.Open();
        await Task.Delay(200);
        var window = Resources.FindObjectsOfTypeAll<MonsterAttackPreviewWindow>().First();
        var monster = UnityEngine.Object.FindAnyObjectByType<MonsterBrain>();
        Sprite originalSprite = monster.GetComponent<SpriteRenderer>().sprite;
        Vector3 originalMonsterPosition = monster.transform.position;
        var dropdown = window.rootVisualElement.Q<DropdownField>();
        Assert(dropdown.choices.Count == 2, "공격 목록 표시 실패");
        Assert(Field<GameObject>(window, "preview") != null, "미리보기 오브젝트 생성 실패");
        Vector3 originalPreviewPosition = Field<GameObject>(window, "preview").transform.position;
        Vector3 movedPreviewPosition = originalPreviewPosition + new Vector3(2f, 1f, 0f);
        Call(window, "SetPreviewPosition", movedPreviewPosition);
        Assert(Field<GameObject>(window, "preview").transform.position == movedPreviewPosition, "미리보기 몬스터 이동 실패");

        for (int i = 0; i < 2; i++)
        {
            Call(window, "SelectAttack", i);
            var attack = Field<MonsterAttackData>(window, "selectedAttack");
            Vector3 expectedCenter = movedPreviewPosition + (Vector3)attack.HitboxOffset;
            Assert((Vector3)Call(window, "GetHitboxCenter") == expectedCenter, "몬스터 미리보기 원점 기준 불일치");
            VerifyColliderIndependence(attack);
            Call(window, "GoToHitEvent");
            Assert(Field<GameObject>(window, "preview").transform.position == movedPreviewPosition, "공격 또는 프레임 변경 시 미리보기 위치 초기화");
            float time = Field<float>(window, "previewTime");
            float hitTime = AnimationUtility.GetAnimationEvents(attack.AnimationClip).First(evt => evt.functionName == "ApplyAttackHit").time;
            Assert(Mathf.Abs(time - hitTime) <= 1f / attack.AnimationClip.frameRate, "타격 프레임 이동 실패");
            var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
            var expected = AnimationUtility.GetObjectReferenceCurve(attack.AnimationClip, binding).Last(key => key.time <= time + 0.0001f).value;
            Assert(Field<SpriteRenderer>(window, "previewRenderer").sprite == expected, "선택한 공격 프레임 표시 실패");
        }

        Call(window, "SetTime", 0f);
        Call(window, "TogglePlayback");
        await Task.Delay(180);
        Assert(Field<float>(window, "previewTime") > 0f, "편집 모드 재생 실패");
        Assert(Field<GameObject>(window, "preview").transform.position == movedPreviewPosition, "재생 중 미리보기 위치 변경");
        Call(window, "TogglePlayback");
        float paused = Field<float>(window, "previewTime");
        await Task.Delay(100);
        Assert(Field<float>(window, "previewTime") == paused, "일시정지 실패");

        var originalAttack = Field<MonsterAttackData>(window, "selectedAttack");
        var testAttack = UnityEngine.Object.Instantiate(originalAttack);
        try
        {
            window.GetType().GetField("selectedAttack", Private).SetValue(window, testAttack);
            Call(window, "WriteHitbox", new Vector2(1.3f, 0.2f), new Vector2(2f, 1.5f));
            Assert(testAttack.HitboxOffset == new Vector2(1.3f, 0.2f) && testAttack.HitboxSize == new Vector2(2f, 1.5f), "판정 데이터 편집 실패");
            Assert(originalAttack.HitboxOffset != testAttack.HitboxOffset, "테스트에서 원본 에셋 변경");
        }
        finally
        {
            window.GetType().GetField("selectedAttack", Private).SetValue(window, originalAttack);
            UnityEngine.Object.DestroyImmediate(testAttack);
        }

        Call(window, "StopPreview");
        Assert(Field<GameObject>(window, "preview") == null && !AnimationMode.InAnimationMode(), "미리보기 종료 정리 실패");
        Assert(monster.GetComponent<SpriteRenderer>().sprite == originalSprite, "전투 몬스터 스프라이트 변경");
        Assert(monster.transform.position == originalMonsterPosition, "기준 몬스터 위치 변경");
        Call(window, "SelectAttack", 0);
        Call(window, "SetPreviewPosition", originalPreviewPosition);
        Call(window, "GoToHitEvent");
        window.Focus();
        return "PASS: preview movement, position retained across attacks/frames/playback, two attack choices, hit-event frames, playback/pause, hitbox editing, cleanup, original monster position and sprite preserved. Window left at Attack1 hit frame.";
    }

    private static void VerifyColliderIndependence(MonsterAttackData data)
    {
        var temporary = new GameObject("TemporaryHitboxOriginTest") { hideFlags = HideFlags.HideAndDontSave };
        try
        {
            temporary.SetActive(false);
            temporary.transform.position = new Vector3(13f, 7f, 0f);
            var collider = temporary.AddComponent<BoxCollider2D>();
            var attack = temporary.AddComponent<MonsterAttack>();
            var method = typeof(MonsterAttack).GetMethod("GetHitboxCenter", Private);
            Vector2 rightBefore = (Vector2)method.Invoke(attack, new object[] { data, 1 });
            Vector2 leftBefore = (Vector2)method.Invoke(attack, new object[] { data, -1 });
            collider.offset = new Vector2(2f, -3f);
            collider.size = new Vector2(7f, 9f);
            Assert((Vector2)method.Invoke(attack, new object[] { data, 1 }) == rightBefore, "콜라이더 수정으로 오른쪽 공격 위치 변경");
            Assert((Vector2)method.Invoke(attack, new object[] { data, -1 }) == leftBefore, "콜라이더 수정으로 왼쪽 공격 위치 변경");
            Assert(rightBefore == (Vector2)temporary.transform.position + data.HitboxOffset, "실제 공격 원점 기준 불일치");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(temporary);
        }
    }
}
