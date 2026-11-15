using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using Parry;
using Parry.PlayerClass;
public static class BuildBerserkerAura
{
    public static object Run()
    {
        const string name = "Berserker_Aura";
        const string prefabPath = "Assets/Prefabs/Effects/Berserker_Aura.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath)) throw new Exception("Aura already exists; inspect before rebuilding.");
        var paths = System.IO.Directory.GetFiles("Assets/AssetStoreImport/AW_VFX_Mega_Pack_Free/06_Heal_Shield_Buffs/Heal_Aura/Small/frames", "*.png").OrderBy(p => p).ToArray();
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
            settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }).ToArray();
        var clip = new AnimationClip { name = name, frameRate = 15 };
        var keys = sprites.Select((s, i) => new ObjectReferenceKeyframe { time = i / 15f, value = s }).ToList();
        keys.Add(new ObjectReferenceKeyframe { time = sprites.Length / 15f, value = sprites.Last() });
        AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), keys.ToArray());
        var clipSettings = AnimationUtility.GetAnimationClipSettings(clip);
        clipSettings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, clipSettings);
        AssetDatabase.CreateAsset(clip, "Assets/Animations/Effects/" + name + ".anim");
        var controller = AnimatorController.CreateAnimatorControllerAtPath("Assets/Animations/Effects/" + name + ".controller");
        var state = controller.layers[0].stateMachine.AddState(name);
        state.motion = clip;
        controller.layers[0].stateMachine.defaultState = state;
        var go = new GameObject(name);
        try
        {
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprites[0];
            sr.color = new Color(1f, 0.08f, 0.06f, 0.85f);
            sr.sortingLayerName = "Player";
            sr.sortingOrder = -1;
            var shield = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Effects/Shield_Bubble.prefab");
            sr.sharedMaterial = shield.GetComponent<SpriteRenderer>().sharedMaterial;
            go.AddComponent<Animator>().runtimeAnimatorController = controller;
            go.AddComponent<PooledObject>();
            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
        var player = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Player/Player.prefab");
        try
        {
            var so = new SerializedObject(player.GetComponent<PlayerBerseker>());
            so.FindProperty("auraPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(player, "Assets/Prefabs/Player/Player.prefab");
        }
        finally { PrefabUtility.UnloadPrefabContents(player); }
        AssetDatabase.SaveAssets();
        return new { prefabPath, frames = sprites.Length, fps = 15, loop = clip.isLooping };
    }
}
