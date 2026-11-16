using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class UpdateGoblinFlinch
{
    public static object Run()
    {
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animations/Monster/Goblin/Goblin_Flinch.anim");
        var sprites = AssetDatabase.LoadAllAssetsAtPath("Assets/AssetStoreImport/Monsters Creatures Fantasy/Sprites/Goblin/Goblin_Flinch.png").OfType<Sprite>().OrderBy(s => s.rect.x).ToArray();
        var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        AnimationUtility.SetObjectReferenceCurve(clip, binding, sprites.Select((s, i) => new ObjectReferenceKeyframe { time = i / clip.frameRate, value = s }).ToArray());
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        settings.stopTime = sprites.Length / clip.frameRate;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
        AssetDatabase.SaveAssets();
        var keys = AnimationUtility.GetObjectReferenceCurve(clip, binding);
        if (keys.Length != 2 || keys.Any(k => !sprites.Contains(k.value)) || !clip.isLooping) throw new Exception("Flinch clip update verification failed");
        return new { sprites = keys.Select(k => k.value.name).ToArray(), clip.frameRate, clip.length, clip.isLooping };
    }
}
