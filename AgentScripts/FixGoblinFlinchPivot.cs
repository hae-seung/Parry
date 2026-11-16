using System;
using System.Linq;
using System.IO;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

public static class FixGoblinFlinchPivot
{
    const string Folder = "Assets/AssetStoreImport/Monsters Creatures Fantasy/Sprites/Goblin/";
    static int FootY(string path, Sprite sprite)
    {
        var image = new Texture2D(2, 2);
        try
        {
            image.LoadImage(File.ReadAllBytes(path));
            var pixels = image.GetPixels32();
            Rect r = sprite.rect;
            for (int y = 0; y < r.height; y++)
                for (int x = 0; x < r.width; x++)
                    if (pixels[((int)r.y + y) * image.width + (int)r.x + x].a > 25) return y;
            throw new Exception("Sprite is transparent");
        }
        finally { UnityEngine.Object.DestroyImmediate(image); }
    }
    public static object Inspect()
    {
        string referencePath = Folder + "Attack2.png";
        var reference = AssetDatabase.LoadAllAssetsAtPath(referencePath).OfType<Sprite>().Single(s => s.name == "Attack2_2");
        float floor = (FootY(referencePath, reference) - reference.pivot.y) / reference.pixelsPerUnit;
        var sprites = AssetDatabase.LoadAllAssetsAtPath(Folder + "Goblin_Flinch.png").OfType<Sprite>().OrderBy(s => s.rect.x).ToArray();
        return new { referenceFootWorldY = floor, sprites = sprites.Select(s => new { s.name, pivot = s.pivot.ToString(), footPixelY = FootY(Folder + "Goblin_Flinch.png", s), footWorldY = (FootY(Folder + "Goblin_Flinch.png", s) - s.pivot.y) / s.pixelsPerUnit }).ToArray() };
    }
    public static object Run()
    {
        string path = Folder + "Goblin_Flinch.png";
        string referencePath = Folder + "Attack2.png";
        var reference = AssetDatabase.LoadAllAssetsAtPath(referencePath).OfType<Sprite>().Single(s => s.name == "Attack2_2");
        float floor = (FootY(referencePath, reference) - reference.pivot.y) / reference.pixelsPerUnit;
        var importer = AssetImporter.GetAtPath(path);
        var factories = new SpriteDataProviderFactories();
        factories.Init();
        var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        var capability = provider.GetDataProvider<ISpriteFrameEditCapability>();
        if (capability == null || !capability.GetEditCapability().HasCapability(EEditCapability.EditPivot))
            throw new Exception("Importer does not support pivot editing; no changes applied");
        var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
        var rects = provider.GetSpriteRects();
        foreach (var rect in rects)
        {
            var sprite = sprites.Single(s => s.name == rect.name);
            float pivotY = FootY(path, sprite) - floor * sprite.pixelsPerUnit;
            rect.alignment = SpriteAlignment.Custom;
            rect.pivot = new Vector2(sprite.pivot.x / sprite.rect.width, pivotY / sprite.rect.height);
        }
        provider.SetSpriteRects(rects);
        provider.Apply();
        importer.SaveAndReimport();
        foreach (var sprite in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>())
            if (Mathf.Abs((FootY(path, sprite) - sprite.pivot.y) / sprite.pixelsPerUnit - floor) > .0001f)
                throw new Exception("Foot alignment verification failed");
        return Inspect();
    }
}
