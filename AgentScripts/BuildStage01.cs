using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using Unity.Cinemachine;
using Parry.PlayerClass;
using Parry.MonsterClass;
using TMPro;

public static class BuildStage01
{
    private static Sprite square;
    private static Transform scenery;
    private static Tilemap terrain;
    private static TileBase topTile, wallTile;

    public static object Run()
    {
        const string destination = "Assets/Scenes/Stage01.unity";
        var source = EditorSceneManager.GetActiveScene();
        if (Application.isPlaying || source.path != "Assets/Scenes/SampleScene.unity" || source.isDirty)
            throw new Exception("Open saved SampleScene in Edit mode first.");
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(destination) != null)
            throw new Exception("Stage01 already exists; refusing to overwrite.");
        EditorSceneManager.SaveScene(source, destination, true);
        var scene = EditorSceneManager.OpenScene(destination);
        var player = UnityEngine.Object.FindAnyObjectByType<PlayerCharacter>();
        player.transform.SetParent(null, true);
        foreach (var monster in UnityEngine.Object.FindObjectsByType<Monster>(FindObjectsSortMode.None))
            UnityEngine.Object.DestroyImmediate(monster.gameObject);
        foreach (var root in scene.GetRootGameObjects())
            if (root.name == "Map" || root.name == "Entity") UnityEngine.Object.DestroyImmediate(root);

        square = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Prototype/WhiteSquare.png");
        var stage = new GameObject("Stage01_OuterRuins");
        scenery = Child("Scenery", stage.transform).transform;
        var grid = Child("Terrain", stage.transform).AddComponent<Grid>();
        var tileObject = Child("StoneGround", grid.transform);
        terrain = tileObject.AddComponent<Tilemap>();
        tileObject.AddComponent<TilemapRenderer>().sortingOrder = -1;
        topTile = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/AssetStoreImport/Hero Knight - Pixel Art/Environment/EnvironmentTiles_1.asset");
        wallTile = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/AssetStoreImport/Hero Knight - Pixel Art/Environment/EnvironmentTiles_3.asset");

        // Continuous lower route; terraces introduce jumping without lethal pits.
        Ground("Approach", -8, 30, 0, stage.transform);
        Ground("LowerStair", 30, 34, 1, stage.transform);
        Ground("UpperCourt", 34, 44, 2, stage.transform);
        Ground("DownStair", 44, 48, 1, stage.transform);
        Ground("RuinsToArena", 48, 118, 0, stage.transform);
        Platform("OptionalStep", 55, 59, 1.5f, stage.transform);
        Platform("OptionalUpperPath", 61, 67, 3f, stage.transform);
        Block("LeftBoundary", stage.transform, new Vector2(-8.5f, 1), new Vector2(1, 18), new Color(.12f,.14f,.16f), true);
        Block("ArenaEndWall", stage.transform, new Vector2(118.5f, 1), new Vector2(1,18), new Color(.12f,.14f,.16f), true);

        Block("NightSky", scenery, new Vector2(55,5), new Vector2(150,40), new Color(.055f,.075f,.105f), false, -50);
        var stoneSprite = AssetDatabase.LoadAllAssetsAtPath("Assets/AssetStoreImport/Hero Knight - Pixel Art/Sprites/EnvironmentTiles.png").OfType<Sprite>().Single(s => s.name == "EnvironmentTiles_3");
        for (int i = 0; i < 20; i++)
        {
            float x = -9 + i * 7;
            float height = 4 + (i * 7 % 5);
            var tower = Block("DistantRuins_" + i, scenery, new Vector2(x,height/2), new Vector2(3.5f,height), new Color(.14f,.20f,.26f), false, -30);
            var sr = tower.GetComponent<SpriteRenderer>();
            sr.sprite = stoneSprite;
            sr.drawMode = SpriteDrawMode.Tiled;
            tower.transform.localScale = Vector3.one;
            sr.size = new Vector2(3.5f,height);
            for (int j = 0; j < 3; j++)
                Block("Battlement", tower.transform, new Vector2(x-1.3f+j*1.3f,height+.3f), new Vector2(.6f,.6f), new Color(.10f,.15f,.20f), false,-29);
        }
        for (int i = 0; i < 9; i++)
        {
            float x = 2 + i * 13;
            float y = x >= 34 && x < 44 ? 2 : x >= 30 && x < 48 ? 1 : 0;
            Torch(x,y);
        }
        Arch(27,0,scenery);
        Arch(88,0,scenery);
        Label("Title", "ASHEN PASS", new Vector2(5,3.1f), 4f, new Color(.72f,.76f,.72f));
        Label("Subtitle", "01  /  OUTER RUINS", new Vector2(5,2.5f), 1.8f, new Color(.45f,.55f,.59f));
        Label("ArenaTitle", "THE OLD COURT", new Vector2(102,4.5f), 3f, new Color(.65f,.68f,.66f));
        for (int x = 93; x <= 114; x += 3)
            Block("ArenaInlay", scenery, new Vector2(x,.025f), new Vector2(.8f,.05f), new Color(.38f,.31f,.19f), false,0);

        var encounters = Child("Encounters", stage.transform).transform;
        SpawnGoblin("Goblin_Approach", 18, 0, encounters);
        SpawnGoblin("Goblin_UpperCourt", 39, 2, encounters);
        SpawnGoblin("Goblin_RuinsA", 72, 0, encounters);
        SpawnGoblin("Goblin_RuinsB", 79, 0, encounters);
        var boss = Child("BossSpawn", stage.transform);
        boss.transform.position = new Vector3(105,0,0);
        var start = Child("PlayerSpawn", stage.transform);
        start.transform.position = new Vector3(3,0,0);
        PlaceFeet(player.gameObject, 3, 0);

        var camera = UnityEngine.Object.FindAnyObjectByType<CinemachineCamera>();
        var target = camera.Target;
        target.TrackingTarget = player.transform;
        camera.Target = target;
        var lens = camera.Lens;
        lens.OrthographicSize = 4.5f;
        camera.Lens = lens;
        var composer = camera.GetComponent<CinemachinePositionComposer>() ?? camera.gameObject.AddComponent<CinemachinePositionComposer>();
        composer.CameraDistance = 10;
        composer.TargetOffset = new Vector3(1.8f,1.6f,0);
        composer.Damping = new Vector3(.3f,.5f,0);
        var boundsObject = Child("CameraBounds", stage.transform);
        boundsObject.layer = 2;
        var bounds = boundsObject.AddComponent<PolygonCollider2D>();
        bounds.isTrigger = true;
        bounds.points = new[] { new Vector2(-8,-4),new Vector2(-8,13),new Vector2(119,13),new Vector2(119,-4) };
        var confiner = camera.GetComponent<CinemachineConfiner2D>() ?? camera.gameObject.AddComponent<CinemachineConfiner2D>();
        confiner.BoundingShape2D = bounds;
        confiner.Damping = .2f;
        camera.transform.position = new Vector3(5,2,-10);
        Camera.main.transform.position = new Vector3(5,2,-10);
        Camera.main.backgroundColor = new Color(.055f,.075f,.105f);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = stage;
        if (SceneView.lastActiveSceneView != null)
            SceneView.lastActiveSceneView.LookAt(new Vector3(50,1,0), Quaternion.identity, 55, true);
        return new { scene = destination, length = 126, goblins = 4, bossSpawn = 105, route = "approach -> upper court -> ruins -> boss arena" };
    }

    private static GameObject Child(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent,false);
        return go;
    }

    private static GameObject Block(string name, Transform parent, Vector2 center, Vector2 size, Color color, bool collision, int order = -5)
    {
        var go = Child(name,parent);
        go.transform.position = center;
        go.transform.localScale = new Vector3(size.x,size.y,1);
        var sr=go.AddComponent<SpriteRenderer>();
        sr.sprite=square;
        sr.color=color;
        sr.sortingOrder=order;
        if (collision) go.AddComponent<BoxCollider2D>();
        return go;
    }

    private static void Ground(string name, int from, int to, int surface, Transform parent)
    {
        for(int x=from;x<to;x++)
            for(int y=-8;y<surface;y++)
                terrain.SetTile(new Vector3Int(x,y,0), y==surface-1 ? topTile : wallTile);
        var go=Child(name,parent);
        go.transform.position=new Vector3((from+to)*.5f,(surface-8)*.5f,0);
        go.AddComponent<BoxCollider2D>().size=new Vector2(to-from,surface+8);
    }

    private static void Platform(string name,float from,float to,float y,Transform parent)
    {
        var go=Child(name,parent);
        go.transform.position=new Vector3((from+to)*.5f,y-.25f,0);
        go.AddComponent<BoxCollider2D>().size=new Vector2(to-from,.5f);
        var sr=go.AddComponent<SpriteRenderer>();
        sr.sprite=((Tile)topTile).sprite;
        sr.drawMode=SpriteDrawMode.Tiled;
        sr.size=new Vector2(to-from,.5f);
        sr.sortingOrder=-1;
    }

    private static void Torch(float x,float y)
    {
        var post=Block("TorchPost",scenery,new Vector2(x,y+.75f),new Vector2(.13f,1.5f),new Color(.24f,.20f,.15f),false,-2);
        Block("Brazier",post.transform,new Vector2(x,y+1.45f),new Vector2(.5f,.18f),new Color(.20f,.18f,.16f),false,-2);
        Block("Ember",post.transform,new Vector2(x,y+1.65f),new Vector2(.22f,.3f),new Color(1,.51f,.14f),false,-2);
        Block("FlameCore",post.transform,new Vector2(x,y+1.62f),new Vector2(.09f,.18f),new Color(1,.86f,.47f),false,-1);
    }

    private static void Arch(float x,float y,Transform parent)
    {
        Color color=new Color(.20f,.24f,.27f);
        Block("GatePillar",parent,new Vector2(x-2,y+2.7f),new Vector2(.8f,5.4f),color,false,-15);
        Block("GatePillar",parent,new Vector2(x+2,y+2.7f),new Vector2(.8f,5.4f),color,false,-15);
        Block("GateLintel",parent,new Vector2(x,y+5.4f),new Vector2(5,.7f),color,false,-15);
    }

    private static void Label(string name,string text,Vector2 position,float size,Color color)
    {
        var go=Child(name,scenery);
        go.transform.position=position;
        var label=go.AddComponent<TextMeshPro>();
        label.text=text;
        label.fontSize=size;
        label.alignment=TextAlignmentOptions.Center;
        label.color=color;
        label.rectTransform.sizeDelta=new Vector2(18,2);
        label.GetComponent<MeshRenderer>().sortingOrder=-10;
    }

    private static void SpawnGoblin(string name,float x,float y,Transform parent)
    {
        var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Monster/Goblin.prefab"));
        go.name=name;
        go.transform.SetParent(parent,true);
        PlaceFeet(go,x,y);
    }

    private static void PlaceFeet(GameObject go,float x,float surface)
    {
        go.transform.position=new Vector3(x,0,0);
        Physics2D.SyncTransforms();
        float bottom=go.GetComponent<Collider2D>().bounds.min.y;
        go.transform.position=new Vector3(x,surface-bottom+.03f,0);
    }
}
