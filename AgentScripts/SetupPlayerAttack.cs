using System;
using System.IO;
using System.Linq;
using Parry.Player;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using MonsterTarget = Parry.Monster.Monster;

public static class SetupPlayerAttack
{
    public static object Run()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Exit Play mode first.");
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/SampleScene.unity") throw new InvalidOperationException("Open SampleScene.");
        Directory.CreateDirectory("Temp/AttackSetupBackup");
        foreach (var path in new[] { scene.path, "Assets/Prefabs/Player/Player.prefab", "Assets/InputSystem_Actions.inputactions", "ProjectSettings/TagManager.asset" })
        {
            string backup = "Temp/AttackSetupBackup/" + Path.GetFileName(path);
            if (!File.Exists(backup)) File.Copy(path, backup);
        }
        var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = tags.FindProperty("layers");
        int layer = LayerMask.NameToLayer("Monster");
        if (layer < 0)
        {
            for (int i = 8; i < layers.arraySize; i++)
                if (string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue)) { layer = i; break; }
            if (layer < 0) throw new InvalidOperationException("No free user layer.");
            layers.GetArrayElementAtIndex(layer).stringValue = "Monster";
            tags.ApplyModifiedProperties();
        }

        var inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
        var action = inputAsset.FindAction("Player/Attack", true);
        for (int i = action.bindings.Count - 1; i >= 0; i--)
            if (action.bindings[i].path != "<Mouse>/leftButton") action.ChangeBinding(i).Erase();
        File.WriteAllText(AssetDatabase.GetAssetPath(inputAsset), inputAsset.ToJson());
        AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(inputAsset));

        const string prefabPath = "Assets/Prefabs/Player/Player.prefab";
        var prefab = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            var attack = prefab.GetComponent<PlayerAttack>();
            if (attack == null) attack = prefab.AddComponent<PlayerAttack>();
            var visualTransform = prefab.transform.Find("AttackVisual");
            var visualObject = visualTransform != null ? visualTransform.gameObject : new GameObject("AttackVisual");
            visualObject.transform.SetParent(prefab.transform, false);
            var visual = visualObject.GetComponent<SpriteRenderer>();
            if (visual == null) visual = visualObject.AddComponent<SpriteRenderer>();
            visual.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Prototype/WhiteSquare.png");
            visual.color = new Color(1f, 0.7f, 0.15f, 0.45f);
            visual.sortingOrder = 2;
            visual.enabled = false;
            var attackData = new SerializedObject(attack);
            attackData.FindProperty("targetLayers").intValue = 1 << layer;
            attackData.FindProperty("attackVisual").objectReferenceValue = visual;
            attackData.ApplyModifiedPropertiesWithoutUndo();
            var input = prefab.GetComponent<PlayerInput>();
            var attackAction = input.actions.FindAction("Player/Attack", true);
            var events = input.actionEvents.ToArray().ToList();
            var attackEvent = events.Find(e => e.actionId == attackAction.id.ToString());
            if (attackEvent == null) { attackEvent = new PlayerInput.ActionEvent(attackAction); events.Add(attackEvent); }
            bool connected = false;
            for (int i = 0; i < attackEvent.GetPersistentEventCount(); i++)
                if (attackEvent.GetPersistentTarget(i) == attack && attackEvent.GetPersistentMethodName(i) == "OnAttack") connected = true;
            if (!connected) UnityEventTools.AddPersistentListener<InputAction.CallbackContext>(attackEvent, attack.OnAttack);
            input.actionEvents = events.ToArray();
            PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }

        var monsterObject = GameObject.Find("Monster");
        if (monsterObject == null) throw new InvalidOperationException("Expected the existing Monster object.");
        Undo.RecordObject(monsterObject, "Set monster attack target layer");
        foreach (var child in monsterObject.GetComponentsInChildren<Transform>(true))
        {
            Undo.RecordObject(child.gameObject, "Set monster layer");
            child.gameObject.layer = layer;
        }
        if (monsterObject.GetComponent<MonsterTarget>() == null) Undo.AddComponent<MonsterTarget>(monsterObject);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        return new { scene = scene.path, monster = monsterObject.name, layer, attack = "Left mouse, ground single hit", damage = 20 };
    }
}
