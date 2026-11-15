using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Parry.MonsterClass;
using Parry.PlayerClass;
using Parry.AnimationClass;
public static class BuildTrainingDummy
{
    public static object Run()
    {
        if (EditorApplication.isPlaying) throw new Exception("Stop Play mode first.");
        const string path = "Assets/Prefabs/Monster/TrainingDummy.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) throw new Exception("Dummy already exists.");
        var root = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Monster/Goblin.prefab");
        try
        {
            root.name = "TrainingDummy";
            UnityEngine.Object.DestroyImmediate(root.GetComponent<MonsterBrain>());
            UnityEngine.Object.DestroyImmediate(root.GetComponent<MonsterAttack>());
            UnityEngine.Object.DestroyImmediate(root.GetComponent<MonsterSensor>());
            UnityEngine.Object.DestroyImmediate(root.GetComponent<MonsterMovement>());
            UnityEngine.Object.DestroyImmediate(root.GetComponent<AnimationClipPlayer>());
            root.AddComponent<TrainingDummy>();
            var body = root.GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0;
            body.constraints = RigidbodyConstraints2D.FreezeAll;
            root.GetComponent<Collider2D>().isTrigger = true;
            root.transform.position = Vector3.zero;
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        var player = UnityEngine.Object.FindAnyObjectByType<PlayerCharacter>();
        if (player == null) throw new Exception("Player missing from scene; prefab created.");
        var dummy = (GameObject)PrefabUtility.InstantiatePrefab(prefab, player.gameObject.scene);
        dummy.transform.position = player.transform.position + Vector3.right * 3f;
        Physics2D.SyncTransforms();
        var playerCollider = player.GetComponent<Collider2D>();
        var dummyCollider = dummy.GetComponent<Collider2D>();
        dummy.transform.position += Vector3.up * (playerCollider.bounds.min.y - dummyCollider.bounds.min.y);
        dummy.GetComponent<SpriteRenderer>().flipX = true;
        EditorSceneManager.MarkSceneDirty(dummy.scene);
        EditorSceneManager.SaveScene(dummy.scene);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = dummy;
        return new { prefab = path, position = dummy.transform.position.ToString(), hp = 1000000, ai = dummy.GetComponent<MonsterBrain>() != null };
    }
}
