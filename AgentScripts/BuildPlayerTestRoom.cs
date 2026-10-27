using System.IO;
using Parry.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BuildPlayerTestRoom
{
    public static object Build()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/SampleScene.unity" || Application.isPlaying)
            throw new System.InvalidOperationException("Open SampleScene in Edit mode first.");
        if (GameObject.Find("Player") != null || GameObject.Find("TestRoom") != null)
            throw new System.InvalidOperationException("Player or TestRoom already exists; refusing to duplicate.");

        Directory.CreateDirectory("Assets/Art/Prototype");
        Directory.CreateDirectory("Assets/Physics");
        Directory.CreateDirectory("Assets/Prefabs/Player");
        var texture = new Texture2D(8, 8);
        var pixels = new Color[64];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
        texture.SetPixels(pixels);
        texture.Apply();
        const string spritePath = "Assets/Art/Prototype/WhiteSquare.png";
        File.WriteAllBytes(spritePath, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(spritePath);
        var importer = (TextureImporter)AssetImporter.GetAtPath(spritePath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = 8;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        var material = new PhysicsMaterial2D("PlayerFrictionless") { friction = 0f, bounciness = 0f };
        AssetDatabase.CreateAsset(material, "Assets/Physics/PlayerFrictionless.physicsMaterial2D");

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Create player test room");
        var room = new GameObject("TestRoom");
        Undo.RegisterCreatedObjectUndo(room, "Create TestRoom");
        Block("Floor", room.transform, new Vector2(0, -3.5f), new Vector2(24, 1), sprite);
        Block("LeftWall", room.transform, new Vector2(-12, 0), new Vector2(1, 8), sprite);
        Block("RightWall", room.transform, new Vector2(12, 0), new Vector2(1, 8), sprite);

        var player = new GameObject("Player");
        Undo.RegisterCreatedObjectUndo(player, "Create Player");
        player.transform.position = new Vector3(-4, -1, 0);
        var body = player.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = 3;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        var capsule = player.AddComponent<CapsuleCollider2D>();
        capsule.size = new Vector2(0.8f, 1.6f);
        capsule.sharedMaterial = material;
        Visual("Body", player.transform, Vector2.zero, new Vector2(0.8f, 1.6f), sprite, new Color(0.15f, 0.75f, 0.85f));
        var marker = Visual("FacingMarker", player.transform, new Vector2(0.3f, 0.3f), new Vector2(0.2f, 0.16f), sprite, Color.white);
        marker.GetComponent<SpriteRenderer>().sortingOrder = 1;
        var controller = player.AddComponent<PlayerController>();
        var serialized = new SerializedObject(controller);
        serialized.FindProperty("facingMarker").objectReferenceValue = marker.transform;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.SaveAsPrefabAssetAndConnect(player, "Assets/Prefabs/Player/Player.prefab", InteractionMode.AutomatedAction);
        var camera = Camera.main;
        Undo.RecordObject(camera, "Frame test room");
        Undo.RecordObject(camera.transform, "Frame test room");
        camera.orthographic = true;
        camera.orthographicSize = 7.5f;
        camera.transform.position = new Vector3(0, 1, -10);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.045f, 0.065f, 0.09f);
        Selection.activeGameObject = player;
        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        return new { scene = scene.path, player = player.name, prefab = "Assets/Prefabs/Player/Player.prefab", input = "A/D + Space" };
    }

    private static GameObject Visual(string name, Transform parent, Vector2 position, Vector2 size, Sprite sprite, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localScale = new Vector3(size.x, size.y, 1);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        return go;
    }

    private static void Block(string name, Transform parent, Vector2 position, Vector2 size, Sprite sprite)
    {
        var block = Visual(name, parent, position, size, sprite, new Color(0.25f, 0.3f, 0.36f));
        block.AddComponent<BoxCollider2D>();
    }
}
