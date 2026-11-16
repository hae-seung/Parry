using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Parry;
using Parry.PlayerClass;

public static class VerifyPoolShutdown
{
    const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    public static object Verify()
    {
        Check(!ObjectPoolManager.IsQuitting, "Quit flag was not reset on Play start");
        var manager = ObjectPoolManager.Instance;
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Effects/Berserker_Aura.prefab");
        var owner = new GameObject("Pool shutdown test owner");
        var aura = manager.SpawnObject(prefab, new Vector3(-90, 0, 0), owner.transform);
        aura.GetComponent<PooledObject>().Release();
        Check(!aura.activeSelf && aura.transform.parent == manager.transform, "Normal release failed");
        var reused = manager.SpawnObject(prefab, owner.transform.position, owner.transform);
        Check(reused == aura && aura.activeSelf, "Normal reuse failed");

        typeof(ObjectPoolManager).GetMethod("MarkQuitting", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
        UnityEngine.Object.DestroyImmediate(manager.gameObject);
        reused.GetComponent<PooledObject>().Release();
        Check(reused.activeSelf && reused.transform.parent == owner.transform, "Quit release changed the hierarchy");
        Check(UnityEngine.Object.FindObjectsByType<ObjectPoolManager>().Length == 0, "Release recreated the manager");
        UnityEngine.Object.DestroyImmediate(owner);
        typeof(ObjectPoolManager).GetMethod("ResetQuitState", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
        Check(!ObjectPoolManager.IsQuitting, "Quit reset failed");
        return "Passed: normal return/reuse; quitting skips hierarchy changes; return works after manager destruction without recreating singleton; reset clears static flag.";
    }

    public static object PrepareQuit()
    {
        Check(!ObjectPoolManager.IsQuitting, "Quit flag was not reset");
        var manager = ObjectPoolManager.Instance;
        var source = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/Player.prefab"));
        source.transform.position = new Vector3(-100, 0, 0);
        var mode = source.GetComponent<PlayerBerseker>();
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Effects/Berserker_Aura.prefab");
        var aura = manager.SpawnObject(prefab, source.transform.position, source.transform);
        typeof(PlayerBerseker).GetField("auraInstance", Flags).SetValue(mode, aura);
        typeof(PlayerBerseker).GetField("<IsBerserking>k__BackingField", Flags).SetValue(mode, true);
        var rootEffect = manager.SpawnObject((GameObject)typeof(PlayerBerseker).GetField("burserkerHitVfx", Flags).GetValue(mode), new Vector3(-90, 0, 0));
        rootEffect.GetComponent<Animator>().speed = 0f;
        Check(aura.activeInHierarchy && rootEffect.activeInHierarchy, "Quit fixtures not active");
        return "Ready to stop Play: player has active child aura and root effect is active.";
    }
}
