using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
public static class BuildParryEffects
{
    public static void Run()
    {
        System.IO.Directory.CreateDirectory("Assets/Animations/Effects");
        System.IO.Directory.CreateDirectory("Assets/Prefabs/Effects");
        AssetDatabase.Refresh();
        var shield = Build("Shield_Bubble", "06_Heal_Shield_Buffs");
        var hit = Build("Hit_Parry", "02_Impacts");
        var player = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Player/Player.prefab");
        try
        {
            var parry = player.GetComponent<Parry.PlayerClass.PlayerParry>();
            var serialized = new SerializedObject(parry);
            serialized.FindProperty("shieldBubblePrefab").objectReferenceValue = shield;
            serialized.FindProperty("parryHitPrefab").objectReferenceValue = hit;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(player, "Assets/Prefabs/Player/Player.prefab");
        }
        finally { PrefabUtility.UnloadPrefabContents(player); }
        AssetDatabase.SaveAssets();
        Debug.Log("Parry effects created and assigned.");
    }

    private static GameObject Build(string name, string category)
    {
        string folder = "Assets/AssetStoreImport/AW_VFX_Mega_Pack_Free/" + category + "/" + name + "/Small/frames";
        var paths = System.IO.Directory.GetFiles(folder, "*.png").OrderBy(p => p).ToArray();
        var sprites = paths.Select(path =>
        {
            path = path.Replace('\\', '/');
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }).ToArray();
        var clip = new AnimationClip { frameRate = 15, name = name };
        var keys = sprites.Select((sprite, index) => new ObjectReferenceKeyframe { time = index / 15f, value = sprite }).ToList();
        keys.Add(new ObjectReferenceKeyframe { time = sprites.Length / 15f, value = sprites.Last() });
        AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), keys.ToArray());
        AnimationUtility.SetAnimationEvents(clip, new[] { new AnimationEvent { time = sprites.Length / 15f, functionName = "EndEffect" } });
        AssetDatabase.CreateAsset(clip, "Assets/Animations/Effects/" + name + ".anim");
        var controller = AnimatorController.CreateAnimatorControllerAtPath("Assets/Animations/Effects/" + name + ".controller");
        var state = controller.layers[0].stateMachine.AddState(name);
        state.motion = clip;
        controller.layers[0].stateMachine.defaultState = state;
        var go = new GameObject(name);
        try
        {
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprites[0];
            renderer.sortingOrder = 20;
            go.AddComponent<Animator>().runtimeAnimatorController = controller;
            go.AddComponent<Parry.AnimationClass.EffectLifetime>();
            return PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/Effects/" + name + ".prefab");
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }
}
