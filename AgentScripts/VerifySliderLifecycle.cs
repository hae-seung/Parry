using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class VerifySliderLifecycle
{
    public static object Run()
    {
        if (EditorApplication.isPlaying) throw new Exception("Run in Edit mode");
        var errors = new List<string>();
        Application.LogCallback capture = (message, stack, type) =>
        {
            if (type == LogType.Exception || type == LogType.Error)
                errors.Add(message + "\n" + stack);
        };
        Application.logMessageReceived += capture;
        int initialCount = Selectable.allSelectableCount;
        try
        {
            ValidateRegistry();
            for (int i = 0; i < 10; i++)
            {
                var contents = PrefabUtility.LoadPrefabContents("Assets/Prefabs/UI/MonsterStatCanvas.prefab");
                PrefabUtility.UnloadPrefabContents(contents);
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/MonsterStatCanvas.prefab"));
                try
                {
                    instance.SetActive(false);
                    instance.SetActive(true);
                    ValidateRegistry();
                }
                finally { UnityEngine.Object.DestroyImmediate(instance); }
                ValidateRegistry();
                if (Selectable.allSelectableCount != initialCount)
                    throw new Exception("Selectable count changed after cleanup");
            }
            if (errors.Count > 0) throw new Exception(string.Join("\n", errors));
            return new { cycles = 10, initialCount, finalCount = Selectable.allSelectableCount, errors = errors.Count };
        }
        finally { Application.logMessageReceived -= capture; }
    }

    private static void ValidateRegistry()
    {
        var type = typeof(Selectable);
        var statics = BindingFlags.NonPublic | BindingFlags.Static;
        var instances = BindingFlags.NonPublic | BindingFlags.Instance;
        var array = (Selectable[])type.GetField("s_Selectables", statics).GetValue(null);
        int count = Selectable.allSelectableCount;
        if (count < 0 || count > array.Length) throw new Exception("Invalid Selectable count: " + count);
        for (int i = 0; i < count; i++)
        {
            if (array[i] == null || (int)type.GetField("m_CurrentIndex", instances).GetValue(array[i]) != i)
                throw new Exception("Invalid Selectable index: " + i);
        }
        foreach (var selectable in Resources.FindObjectsOfTypeAll<Selectable>())
        {
            if (!selectable.gameObject.scene.IsValid() ||
                !(bool)type.GetField("m_EnableCalled", instances).GetValue(selectable))
                continue;
            int index = (int)type.GetField("m_CurrentIndex", instances).GetValue(selectable);
            if (index < 0 || index >= count || array[index] != selectable)
                throw new Exception("Unregistered active Selectable: " + selectable.gameObject.scene.name + "/" + selectable.name);
        }
    }
}
