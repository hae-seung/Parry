using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using TMPro;

public static class ApplySubtitleKoreanFont
{
    public static object Run()
    {
        if(Application.isPlaying) throw new Exception("Edit mode required");
        const string folder="Assets/Fonts/Galmuri/";
        AssetDatabase.Refresh();
        var source=AssetDatabase.LoadAssetAtPath<Font>(folder+"Galmuri11.ttf");
        var subtitle=GameObject.Find("Subtitle").GetComponent<TMP_Text>();
        string text=subtitle.text;
        var main=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(folder+"Galmuri11 SDF.asset");
        if(main==null)
        {
            main=TMP_FontAsset.CreateFontAsset(source,44,4,GlyphRenderMode.SDFAA,512,512,AtlasPopulationMode.Dynamic);
            main.name="Galmuri11 SDF";
            string characters=new string(Enumerable.Range(32,95).Select(i=>(char)i).ToArray())+text;
            if(!main.TryAddCharacters(characters,out string missing)) throw new Exception("Missing glyphs: "+missing);
            main.atlasPopulationMode=AtlasPopulationMode.Static;
            Save(main,folder+"Galmuri11 SDF.asset");
        }
        var fallback=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(folder+"Galmuri11 Dynamic Fallback.asset");
        if(fallback==null)
        {
            fallback=TMP_FontAsset.CreateFontAsset(source,44,4,GlyphRenderMode.SDFAA,1024,1024,AtlasPopulationMode.Dynamic);
            fallback.name="Galmuri11 Dynamic Fallback";
            fallback.isMultiAtlasTexturesEnabled=true;
            typeof(TMP_FontAsset).GetField("m_ClearDynamicDataOnBuild",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(fallback,true);
            Save(fallback,folder+"Galmuri11 Dynamic Fallback.asset");
        }
        if(main.fallbackFontAssetTable==null)
            main.fallbackFontAssetTable=new System.Collections.Generic.List<TMP_FontAsset>();
        if(!main.fallbackFontAssetTable.Contains(fallback))
        {
            main.fallbackFontAssetTable.Add(fallback);
            EditorUtility.SetDirty(main);
        }
        Undo.RecordObject(subtitle,"Apply Korean subtitle font");
        subtitle.font=main;
        subtitle.fontSharedMaterial=main.material;
        subtitle.ForceMeshUpdate();
        foreach(char c in text)
            if(!char.IsWhiteSpace(c)&&!main.HasCharacter(c,true)) throw new Exception("Unsupported character: "+c);
        EditorUtility.SetDirty(subtitle);
        EditorSceneManager.MarkSceneDirty(subtitle.gameObject.scene);
        EditorSceneManager.SaveScene(subtitle.gameObject.scene);
        AssetDatabase.SaveAssets();
        return new {font=main.name,text=subtitle.text,characters=subtitle.textInfo.characterCount,scene=subtitle.gameObject.scene.path};
    }

    private static void Save(TMP_FontAsset font,string path)
    {
        AssetDatabase.CreateAsset(font,path);
        AssetDatabase.AddObjectToAsset(font.material,font);
        foreach(var texture in font.atlasTextures) AssetDatabase.AddObjectToAsset(texture,font);
    }
}
