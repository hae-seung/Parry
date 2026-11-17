using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Parry.PlayerClass;

public static class MigratePlayerData
{
    private const BindingFlags Fields=BindingFlags.Instance|BindingFlags.NonPublic;
    private static readonly string[] Sections={"character","stats","movement","attack","parry","parryGauge","berserker","hitReaction"};
    private static readonly Type[] Components={typeof(PlayerCharacter),typeof(PlayerStatComponent),typeof(PlayerMovement),typeof(PlayerAttack),typeof(PlayerParry),typeof(PlayerParryGauge),typeof(PlayerBerseker),typeof(PlayerHitReaction)};

    private static PlayerData Capture(GameObject player)
    {
        var data=ScriptableObject.CreateInstance<PlayerData>();
        for(int i=0;i<Sections.Length;i++)
        {
            var section=typeof(PlayerData).GetField(Sections[i],Fields);
            object value=Activator.CreateInstance(section.FieldType);
            var component=player.GetComponent(Components[i]);
            foreach(var field in section.FieldType.GetFields(BindingFlags.Instance|BindingFlags.Public))
            {
                string sourceName=field.Name=="initialGauge"?"currentGauge":field.Name;
                var source=Components[i].GetField(sourceName,Fields);
                if(source==null)throw new Exception("Missing source "+Components[i].Name+"."+sourceName);
                field.SetValue(value,source.GetValue(component));
            }
            section.SetValue(data,value);
        }
        return data;
    }

    private static string Values(PlayerData data) => string.Join("\n",Sections.Select(s=>JsonUtility.ToJson(typeof(PlayerData).GetField(s,Fields).GetValue(data))));

    private static void Bind(PlayerCharacter character,PlayerData data)
    {
        var serialized=new SerializedObject(character);
        serialized.FindProperty("data").objectReferenceValue=data;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        if(PrefabUtility.IsPartOfPrefabInstance(character))PrefabUtility.RecordPrefabInstancePropertyModifications(character);
    }

    public static object Run()
    {
        if(Application.isPlaying)throw new Exception("Edit mode required");
        if(EditorSceneManager.GetActiveScene().isDirty)throw new Exception("Save current scene before migrating");
        if(!AssetDatabase.IsValidFolder("Assets/Data/Player"))AssetDatabase.CreateFolder("Assets/Data","Player");
        const string path="Assets/Data/Player/PlayerData.asset";
        if(AssetDatabase.LoadAssetAtPath<PlayerData>(path)!=null)throw new Exception("PlayerData already exists");
        var prefab=PrefabUtility.LoadPrefabContents("Assets/Prefabs/Player/Player.prefab");
        PlayerData baseline;
        string values;
        try
        {
            baseline=Capture(prefab);
            baseline.name="PlayerData";
            values=Values(baseline);
            AssetDatabase.CreateAsset(baseline,path);
            Bind(prefab.GetComponent<PlayerCharacter>(),baseline);
            PrefabUtility.SaveAsPrefabAsset(prefab,"Assets/Prefabs/Player/Player.prefab");
        }
        finally{PrefabUtility.UnloadPrefabContents(prefab);}
        string original=EditorSceneManager.GetActiveScene().path;
        var results=new System.Collections.Generic.List<string>();
        foreach(var scenePath in new[]{"Assets/Scenes/Stage01.unity","Assets/Scenes/SampleScene.unity"})
        {
            var scene=EditorSceneManager.OpenScene(scenePath,OpenSceneMode.Single);
            foreach(var character in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<PlayerCharacter>(true)))
            {
                var captured=Capture(character.gameObject);
                PlayerData selected=baseline;
                if(Values(captured)!=values)
                {
                    string overridePath="Assets/Data/Player/"+scene.name+"PlayerData.asset";
                    captured.name=scene.name+"PlayerData";
                    AssetDatabase.CreateAsset(captured,overridePath);
                    selected=captured;
                }
                else UnityEngine.Object.DestroyImmediate(captured);
                Bind(character,selected);
                results.Add(scene.name+" -> "+AssetDatabase.GetAssetPath(selected));
            }
            EditorSceneManager.SaveScene(scene);
        }
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(original,OpenSceneMode.Single);
        return new{assets=results,baseValues=values};
    }
}
