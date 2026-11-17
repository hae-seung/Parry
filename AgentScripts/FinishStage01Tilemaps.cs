using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using TMPro;

public static class FinishStage01Tilemaps
{
    private const string Folder = "Assets/Data/Stage01";
    private static Sprite square;
    private static Tilemap ground, backdrop, details;
    private static Tile surface, stoneA, stoneB, sky, silhouette, pillar, iron, flame, core;

    public static object Run()
    {
        if (Application.isPlaying || EditorSceneManager.GetActiveScene().path != "Assets/Scenes/Stage01.unity")
            throw new Exception("Open Stage01 in Edit mode");
        var stage = GameObject.Find("Stage01_OuterRuins").transform;
        foreach (Transform child in stage.Cast<Transform>().ToArray())
            if (child.name != "Encounters" && child.name != "BossSpawn" && child.name != "PlayerSpawn" && child.name != "CameraBounds")
                UnityEngine.Object.DestroyImmediate(child.gameObject);
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Data", "Stage01");
        square = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Prototype/WhiteSquare.png");
        var topSprite = AssetDatabase.LoadAllAssetsAtPath("Assets/AssetStoreImport/Hero Knight - Pixel Art/Sprites/EnvironmentTiles.png").OfType<Sprite>().Single(s=>s.name=="EnvironmentTiles_1");
        surface = TileAsset("StoneSurface",topSprite,Color.white,true);
        stoneA = TileAsset("StoneFillA",square,new Color(.17f,.18f,.19f),true);
        stoneB = TileAsset("StoneFillB",square,new Color(.19f,.20f,.21f),true);
        sky = TileAsset("Sky",square,new Color(.055f,.075f,.105f),false);
        silhouette = TileAsset("DistantStone",square,new Color(.085f,.12f,.16f),false);
        pillar = TileAsset("GateStone",square,new Color(.20f,.24f,.27f),false);
        iron = TileAsset("TorchIron",square,new Color(.24f,.20f,.15f),false);
        flame = TileAsset("Ember",square,new Color(1,.48f,.12f),false);
        core = TileAsset("EmberCore",square,new Color(1,.84f,.4f),false);
        foreach(var tile in new[]{pillar,iron,flame,core})
        {
            tile.transform=Matrix4x4.Scale(new Vector3(.25f,.25f,1));
            EditorUtility.SetDirty(tile);
        }
        ground = Map("Ground",stage,1,-1,true);
        backdrop = Map("Background",stage,1,-40,false);
        details = Map("Decoration",stage,.25f,-10,false);
        Fill(backdrop,-20,-10,145,25,sky);
        for(int i=0;i<20;i++)
        {
            int x=-9+i*7, height=4+i*7%5;
            Fill(backdrop,x,0,x+4,height,silhouette);
            for(int j=0;j<2;j++) Fill(backdrop,x+j*3,height,x+j*3+1,height+1,silhouette);
        }
        Ground(-8,30,0);
        Ground(30,34,1);
        Ground(34,44,2);
        Ground(44,48,1);
        Ground(48,118,0);
        Fill(ground,-9,-8,-8,10,stoneA);
        Fill(ground,118,-8,119,10,stoneA);
        // Optional two-step route. Every collision tile is on this same Tilemap.
        for(int x=55;x<59;x++) ground.SetTile(new Vector3Int(x,1,0),surface);
        for(int x=61;x<67;x++) ground.SetTile(new Vector3Int(x,3,0),surface);
        Gate(27);
        Gate(88);
        for(int i=0;i<9;i++)
        {
            int x=2+i*13;
            int y=x>=34&&x<44?2:x>=30&&x<48?1:0;
            Fill(details,x*4,y*4,x*4+1,y*4+6,iron);
            Fill(details,x*4-1,y*4+6,x*4+2,y*4+7,iron);
            Fill(details,x*4,y*4+7,x*4+1,y*4+9,flame);
            details.SetTile(new Vector3Int(x*4,y*4+7,0),core);
        }
        Label(stage,"Title","ASHEN PASS",new Vector2(5,3.1f),4,new Color(.72f,.76f,.72f));
        Label(stage,"Subtitle","01  /  OUTER RUINS",new Vector2(5,2.5f),1.8f,new Color(.45f,.55f,.59f));
        Label(stage,"ArenaTitle","THE OLD COURT",new Vector2(102,4.5f),3,new Color(.65f,.68f,.66f));
        ground.CompressBounds();
        ground.GetComponent<TilemapCollider2D>().ProcessTilemapChanges();
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        return new { scene="Stage01",tilemaps=3,terrainSpriteRenderers=stage.GetComponentsInChildren<SpriteRenderer>().Count(s=>s.GetComponentInParent<Parry.MonsterClass.Monster>()==null),collider="TilemapCollider2D + CompositeCollider2D",tiles=ground.GetUsedTilesCount() };
    }

    private static Tile TileAsset(string name,Sprite sprite,Color color,bool solid)
    {
        string path=Folder+"/"+name+".asset";
        var tile=AssetDatabase.LoadAssetAtPath<Tile>(path);
        if(tile==null){tile=ScriptableObject.CreateInstance<Tile>();AssetDatabase.CreateAsset(tile,path);}
        tile.sprite=sprite;
        tile.color=color;
        tile.colliderType=solid?Tile.ColliderType.Grid:Tile.ColliderType.None;
        EditorUtility.SetDirty(tile);
        return tile;
    }

    private static Tilemap Map(string name,Transform parent,float cell,int order,bool solid)
    {
        var root=new GameObject(name+"Grid",typeof(Grid));
        root.transform.SetParent(parent,false);
        root.GetComponent<Grid>().cellSize=new Vector3(cell,cell,1);
        var go=new GameObject(name,typeof(Tilemap),typeof(TilemapRenderer));
        go.transform.SetParent(root.transform,false);
        var map=go.GetComponent<Tilemap>();
        map.tileAnchor=new Vector3(.5f,.5f,0);
        go.GetComponent<TilemapRenderer>().sortingOrder=order;
        if(solid)
        {
            go.AddComponent<Rigidbody2D>().bodyType=RigidbodyType2D.Static;
            go.AddComponent<CompositeCollider2D>().geometryType=CompositeCollider2D.GeometryType.Polygons;
            go.AddComponent<TilemapCollider2D>().compositeOperation=Collider2D.CompositeOperation.Merge;
        }
        return map;
    }

    private static void Fill(Tilemap map,int x0,int y0,int x1,int y1,TileBase tile)
    {
        for(int x=x0;x<x1;x++)for(int y=y0;y<y1;y++)map.SetTile(new Vector3Int(x,y,0),tile);
    }

    private static void Ground(int from,int to,int top)
    {
        for(int x=from;x<to;x++)for(int y=-8;y<top;y++)
            ground.SetTile(new Vector3Int(x,y,0),y==top-1?surface:((x/2+y)%3==0?stoneB:stoneA));
    }

    private static void Gate(int x)
    {
        Fill(details,(x-2)*4,0,(x-2)*4+3,21,pillar);
        Fill(details,(x+2)*4,0,(x+2)*4+3,21,pillar);
        Fill(details,(x-2)*4-1,21,(x+2)*4+4,24,pillar);
    }

    private static void Label(Transform parent,string name,string text,Vector2 pos,float size,Color color)
    {
        var go=new GameObject(name);
        go.transform.SetParent(parent,false);
        go.transform.position=pos;
        var label=go.AddComponent<TextMeshPro>();
        label.text=text;label.fontSize=size;label.alignment=TextAlignmentOptions.Center;label.color=color;
        label.rectTransform.sizeDelta=new Vector2(18,2);
        label.GetComponent<MeshRenderer>().sortingOrder=-5;
    }
}
