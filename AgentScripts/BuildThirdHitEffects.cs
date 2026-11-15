using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
public static class BuildThirdHitEffects
{
    public static void Run()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode first.");
        string sparkPath = "Assets/Prefabs/Effects/Hit_Spark.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(sparkPath) == null)
            Build("Hit_Spark", "02_Impacts");
        var player = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Player/Player.prefab");
        try
        {
            var serialized = new SerializedObject(player.GetComponent<Parry.PlayerClass.PlayerAttack>());
            serialized.FindProperty("thirdHitEffectPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Effects/Hit_Directional.prefab");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(player, "Assets/Prefabs/Player/Player.prefab");
        }
        finally { PrefabUtility.UnloadPrefabContents(player); }
        AssetDatabase.SaveAssets();
        Debug.Log("Third hit effect assigned; Hit_Spark prefab ready.");
    }

    private static GameObject Build(string name, string category)
    {
        string folder = "Assets/AssetStoreImport/AW_VFX_Mega_Pack_Free/" + category + "/Hit_Spark_Small/Small/frames";
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
            renderer.sortingLayerName = "Player";
            renderer.sortingOrder = 20;
            go.AddComponent<Animator>().runtimeAnimatorController = controller;
            go.AddComponent<Parry.AnimationClass.EffectLifetime>();
            return PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/Effects/" + name + ".prefab");
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }
}
