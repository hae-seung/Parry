using System;
using UnityEditor;
using UnityEngine;
using Parry;
using Parry.AnimationClass;
using Parry.PlayerClass;
public static class VerifyThirdHitEffects
{
    public static object Run()
    {
        var player = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/Player.prefab");
        var reference = new SerializedObject(player.GetComponent<PlayerAttack>()).FindProperty("thirdHitEffectPrefab").objectReferenceValue;
        if (reference == null || reference.name != "Hit_Directional") throw new Exception("Missing third-hit reference");
        foreach (string name in new[] { "Hit_Directional", "Hit_Spark" })
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Effects/" + name + ".prefab");
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animations/Effects/" + name + ".anim");
            if (clip.isLooping || Array.Find(AnimationUtility.GetAnimationEvents(clip), e => e.functionName == "EndEffect") == null)
                throw new Exception(name + " lifetime event missing");
            var holder = new GameObject("TemporaryEffectPoolCheck");
            GameObject instance = null;
            try
            {
                var pool = new Pool(16, holder.transform);
                instance = pool.SpawnObject(prefab, Vector3.zero, holder.transform);
                instance.GetComponent<EffectLifetime>().EndEffect();
                if (instance.activeSelf) throw new Exception("Not released");
                var reused = pool.SpawnObject(prefab, Vector3.one, holder.transform);
                if (reused != instance || !reused.activeSelf) throw new Exception("Not reused");
            }
            finally { UnityEngine.Object.DestroyImmediate(holder); }
        }
        return "Prefab reference, end events, non-looping clips, pool release/reuse passed; temporary objects cleaned.";
    }
}
