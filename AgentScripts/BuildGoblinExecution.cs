using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Parry.MonsterClass;
using Parry.AnimationClass;

public static class BuildGoblinExecution
{
    public static object Run()
    {
        if (EditorApplication.isPlaying) throw new Exception("Stop Play mode first");
        const string executionPath = "Assets/Animations/Monster/Goblin/Goblin_Execution.anim";
        const string deathPath = "Assets/Animations/Monster/Goblin/Goblin_Death.anim";
        if (AssetDatabase.LoadMainAssetAtPath(executionPath) || AssetDatabase.LoadMainAssetAtPath(deathPath)) throw new Exception("New animation already exists");
        var sprite = AssetDatabase.LoadAllAssetsAtPath("Assets/AssetStoreImport/Monsters Creatures Fantasy/Sprites/Goblin/Take Hit.png").OfType<Sprite>().Single(s => s.name == "Take Hit_2");
        var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        var execution = new AnimationClip { name = "Goblin_Execution", frameRate = 10 };
        AnimationUtility.SetObjectReferenceCurve(execution, binding, new[] { new ObjectReferenceKeyframe { time = 0f, value = sprite } });
        var settings = AnimationUtility.GetAnimationClipSettings(execution);
        settings.loopTime = false;
        settings.stopTime = 0.1f;
        AnimationUtility.SetAnimationClipSettings(execution, settings);
        AssetDatabase.CreateAsset(execution, executionPath);

        var sprites = AssetDatabase.LoadAllAssetsAtPath("Assets/AssetStoreImport/Monsters Creatures Fantasy/Sprites/Goblin/Death.png").OfType<Sprite>().OrderBy(s => s.rect.x).ToArray();
        var death = new AnimationClip { name = "Goblin_Death", frameRate = 10 };
        var keys = sprites.Select((s, i) => new ObjectReferenceKeyframe { time = i / 10f, value = s }).ToList();
        keys.Add(new ObjectReferenceKeyframe { time = sprites.Length / 10f, value = sprites.Last() });
        AnimationUtility.SetObjectReferenceCurve(death, binding, keys.ToArray());
        AnimationUtility.SetAnimationEvents(death, new[] { new AnimationEvent { time = sprites.Length / 10f, functionName = "EndDeath" } });
        AssetDatabase.CreateAsset(death, deathPath);

        foreach (string path in new[] { "Assets/Prefabs/Monster/Goblin.prefab", "Assets/Prefabs/Monster/TrainingDummy.prefab" })
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (!root.GetComponent<AnimationClipPlayer>()) root.AddComponent<AnimationClipPlayer>();
                var serialized = new SerializedObject(root.GetComponent<Monster>());
                serialized.FindProperty("executionClip").objectReferenceValue = execution;
                serialized.FindProperty("deathClip").objectReferenceValue = death;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        AssetDatabase.SaveAssets();
        return new { executionSprite = sprite.name, deathFrames = sprites.Length, executionLength = execution.length, deathLength = death.length };
    }
}
