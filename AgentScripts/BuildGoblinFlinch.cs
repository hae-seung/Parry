using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Parry.MonsterClass;

public static class BuildGoblinFlinch
{
    public static object Run()
    {
        if (EditorApplication.isPlaying) throw new Exception("Stop Play mode first");
        const string path = "Assets/Animations/Monster/Goblin/Goblin_Flinch.anim";
        if (AssetDatabase.LoadMainAssetAtPath(path)) throw new Exception("Flinch clip already exists");
        var sprite = AssetDatabase.LoadAllAssetsAtPath("Assets/AssetStoreImport/Monsters Creatures Fantasy/Sprites/Goblin/Attack2.png").OfType<Sprite>().Single(s => s.name == "Attack2_2");
        var clip = new AnimationClip { name = "Goblin_Flinch", frameRate = 10 };
        AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), new[] { new ObjectReferenceKeyframe { time = 0f, value = sprite } });
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.stopTime = 0.1f;
        settings.loopTime = false;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        AssetDatabase.CreateAsset(clip, path);
        const string prefabPath = "Assets/Prefabs/Monster/Goblin.prefab";
        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            var serialized = new SerializedObject(root.GetComponent<MonsterBrain>().Data);
            serialized.FindProperty("flinch").FindPropertyRelative("clip").objectReferenceValue = clip;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
        return new { sprite = sprite.name, clip = path, duration = "MonsterData.Flinch.duration defaults to 1 second" };
    }
}
