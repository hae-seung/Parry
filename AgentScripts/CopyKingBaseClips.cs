using System;
using UnityEditor;
using UnityEngine;

public static class CopyKingBaseClips
{
    public static object Run()
    {
        const string folder = "Assets/Animations/Boss/King";
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets/Animations/Boss", "King");
        string[] sources = { "Idle", "Run", "Death" };
        string[] names = { "King_Idle", "King_Walk", "King_Dead" };
        for (int i = 0; i < sources.Length; i++)
        {
            string target = folder + "/" + names[i] + ".anim";
            if (AssetDatabase.LoadMainAssetAtPath(target) != null)
                throw new InvalidOperationException("Already exists: " + target);
            if (!AssetDatabase.CopyAsset("Assets/AssetStoreImport/Medieval King Pack 2/Animations/" + sources[i] + ".anim", target))
                throw new InvalidOperationException("Copy failed: " + target);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(target);
            clip.name = names[i];
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssetIfDirty(clip);
        }
        return names;
    }
}
