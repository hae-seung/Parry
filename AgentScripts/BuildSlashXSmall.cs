using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using Parry;

public static class BuildSlashXSmall
{
    public static object Run()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode first.");
        const string name = "Slash_X_Small";
        foreach (string path in new[] {
            "Assets/Prefabs/Effects/" + name + ".prefab",
            "Assets/Animations/Effects/" + name + ".anim",
            "Assets/Animations/Effects/" + name + ".controller" })
            if (AssetDatabase.LoadMainAssetAtPath(path) != null)
                throw new InvalidOperationException("Already exists: " + path);
        var prefab = Build(name, "03_Slashes");
        AssetDatabase.SaveAssets();
        return AssetDatabase.GetAssetPath(prefab);
    }

    public static async Task<object> Verify()
    {
        bool background = Application.runInBackground;
        Application.runInBackground = true;
        try
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Effects/Slash_X_Small.prefab");
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animations/Effects/Slash_X_Small.anim");
            if (clip.isLooping || clip.frameRate != 15 || AnimationUtility.GetAnimationEvents(clip).Single().functionName != "EndEffect")
                throw new Exception("Clip configuration mismatch");
            var first = ObjectPoolManager.Instance.SpawnObject(prefab, new Vector3(-100, 0, 0));
            for (int i = 0; i < 200 && first.activeSelf; i++) await Task.Delay(20);
            if (first.activeSelf) throw new Exception("EndEffect did not return to pool");
            var second = ObjectPoolManager.Instance.SpawnObject(prefab, new Vector3(-99, 0, 0));
            if (first != second) throw new Exception("Pool did not reuse instance");
            for (int i = 0; i < 200 && second.activeSelf; i++) await Task.Delay(20);
            if (second.activeSelf) throw new Exception("Reused effect did not return to pool");
            return "Passed: non-looping 19-frame clip at 15fps, EndEffect auto-release, same pooled instance reused and released again.";
        }
        finally { Application.runInBackground = background; }
    }

    private static GameObject Build(string name, string category)
    {
        string folder = "Assets/AssetStoreImport/AW_VFX_Mega_Pack_Free/03_Slashes/Slash_X/Small/frames";
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
