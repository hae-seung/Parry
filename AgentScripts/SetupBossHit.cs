using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Parry.BossClass;

public static class SetupBossHit
{
    public static void Run()
    {
        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Exit Play mode before setup.");

        const string path = "Assets/Prefabs/Monster/Boss_King.prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            root.layer = LayerMask.NameToLayer("Monster");
            var collider = root.GetComponent<BoxCollider2D>();
            if (collider == null)
            {
                collider = root.AddComponent<BoxCollider2D>();
                Bounds bounds = root.GetComponent<SpriteRenderer>().sprite.bounds;
                collider.offset = bounds.center;
                collider.size = bounds.size;
            }
            collider.isTrigger = true;
            if (root.GetComponent<Boss>() == null)
                root.AddComponent<Boss>();
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        foreach (Boss boss in UnityEngine.Object.FindObjectsByType<Boss>())
        {
            Debug.Log($"Boss ready: {boss.name}, layer={boss.gameObject.layer}, collider={boss.GetComponent<Collider2D>().bounds}");
        }
    }
}
