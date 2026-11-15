using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Parry.PlayerClass;

public static class VerifyAnimationPreviewPlayback
{
    public static async Task<string> Run()
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var window = EditorWindow.GetWindow<AnimationPreviewWindow>();
        window.CreateGUI();
        var source = UnityEngine.Object.FindAnyObjectByType<PlayerCharacter>().gameObject;
        var fields = window.rootVisualElement.Query<ObjectField>().ToList();
        fields[0].value = source;
        fields[1].value = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animations/Player/Player_Roll.anim");
        var type = typeof(AnimationPreviewWindow);
        var preview = (GameObject)type.GetField("preview", flags).GetValue(window);
        if (preview == null) throw new Exception("미리보기 없음");
        var renderer = preview.GetComponent<SpriteRenderer>();
        var before = renderer.sprite;
        var button = window.rootVisualElement.Q<Button>();
        typeof(Clickable).GetMethod("Invoke", flags).Invoke(button.clickable, new object[] { null });
        await Task.Delay(140);
        float time = (float)type.GetField("time", flags).GetValue(window);
        if (time <= 0f || before == renderer.sprite)
            throw new Exception($"재생 실패 time={time}, sprite={renderer.sprite}");
        return $"PASS: playback time={time}, sprite {before.name} -> {renderer.sprite.name}, active={preview.activeInHierarchy}, enabled={renderer.enabled}, scene={preview.scene.name}";
    }
}
