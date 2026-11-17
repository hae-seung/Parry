using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Parry.MonsterClass;
using Parry.UIClass;

public static class ConnectMonsterStatUI
{
    private static void Set(UnityEngine.Object target, string field, UnityEngine.Object value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(field).objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    public static object Run()
    {
        if (EditorApplication.isPlaying) throw new Exception("Stop Play mode first");
        const string path = "Assets/Prefabs/UI/MonsterStatCanvas.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var stat = root.GetComponent<MonsterStatUI>();
            var healthObject = root.transform.Find("HealthSet");
            var health = healthObject.GetComponent<MonsterHealthUI>() ?? healthObject.gameObject.AddComponent<MonsterHealthUI>();
            var flinchObject = root.transform.Find("FlinchSlider");
            var flinch = flinchObject.GetComponent<MonsterFlinchUI>() ?? flinchObject.gameObject.AddComponent<MonsterFlinchUI>();
            var executionObject = root.transform.Find("ExecutionSet");
            var execution = executionObject.GetComponent<MonsterExecutionUI>() ?? executionObject.gameObject.AddComponent<MonsterExecutionUI>();
            Set(health, "healthSlider", healthObject.Find("HealthSlider").GetComponent<UnityEngine.UI.Slider>());
            Set(health, "whiteHealthSlider", healthObject.Find("WhiteHealthSlider").GetComponent<UnityEngine.UI.Slider>());
            Set(flinch, "slider", flinchObject.GetComponent<UnityEngine.UI.Slider>());
            Set(execution, "fillImage", executionObject.Find("ExecutionImageSlider").GetComponent<UnityEngine.UI.Image>());
            Set(stat, "healthUI", health);
            Set(stat, "flinchUI", flinch);
            Set(stat, "executionUI", execution);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        return Verify();
    }

    public static object Verify()
    {
        var root = new GameObject("MonsterStatVerification");
        try
        {
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var monster = root.AddComponent<Monster>();
            typeof(Monster).GetField("hitFlashDuration", flags).SetValue(monster, 0f);
            var canvas = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/MonsterStatCanvas.prefab"));
            canvas.transform.SetParent(root.transform, false);
            var health = canvas.transform.Find("HealthSet/HealthSlider").GetComponent<UnityEngine.UI.Slider>();
            var white = canvas.transform.Find("HealthSet/WhiteHealthSlider").GetComponent<UnityEngine.UI.Slider>();
            var flinch = canvas.transform.Find("FlinchSlider").GetComponent<UnityEngine.UI.Slider>();
            var execution = canvas.transform.Find("ExecutionSet");
            var fill = execution.Find("ExecutionImageSlider").GetComponent<UnityEngine.UI.Image>();
            monster.Initialize(new MonsterBaseStats { maxHp = 100 }, new MonsterFlinchData { resistance = 3 });
            typeof(Monster).GetMethod("Start", flags).Invoke(monster, null);
            if (health.value != 1f || white.value != 1f || flinch.value != 1f) throw new Exception("Initial values failed");
            typeof(Monster).GetMethod("ApplyDamage", flags).Invoke(monster, new object[] { 25f });
            if (health.value != .75f || white.value != 1f) throw new Exception("Health damage failed");
            if (execution.gameObject.activeSelf || fill.fillAmount != 0f) throw new Exception("Empty execution UI must be hidden");
            typeof(Monster).GetField("totalDamage", flags).SetValue(monster, 25f);
            monster.InitUI();
            if (!execution.gameObject.activeSelf || Mathf.Abs(fill.fillAmount - 25f / 75f) > .0001f)
                throw new Exception("Execution ratio failed");
            typeof(Monster).GetField("totalDamage", flags).SetValue(monster, 50f);
            monster.InitUI();
            if (Mathf.Abs(fill.fillAmount - 50f / 75f) > .0001f) throw new Exception("Execution increase failed");
            typeof(Monster).GetMethod("ApplyDamage", flags).Invoke(monster, new object[] { 25f });
            if (fill.fillAmount != 1f) throw new Exception("Execution remaining HP update failed");
            canvas.GetComponentInChildren<MonsterExecutionUI>(true).SetValue(50f, 0f);
            if (fill.fillAmount != 1f) throw new Exception("Execution zero HP failed");
            typeof(Monster).GetMethod("ClearStoredDamage", flags).Invoke(monster, null);
            if (execution.gameObject.activeSelf || fill.fillAmount != 0f) throw new Exception("Execution clear failed");
            var reduce = typeof(Monster).GetMethod("ReduceFlinchResistance", flags);
            for (int i = 2; i >= 0; i--)
            {
                reduce.Invoke(monster, null);
                if (Mathf.Abs(flinch.value - i / 3f) > .0001f) throw new Exception("Resistance display failed");
            }
            monster.ResetFlinchResistance();
            if (flinch.value != 1f) throw new Exception("Resistance reset failed");
            monster.Initialize(new MonsterBaseStats { maxHp = 100 }, new MonsterFlinchData { resistance = 3 });
            if (flinch.value != 1f || health.value != 1f || white.value != 1f) throw new Exception("Health reset failed");
            var withoutUI = new GameObject("MonsterWithoutUI");
            withoutUI.transform.SetParent(root.transform);
            var plainMonster = withoutUI.AddComponent<Monster>();
            plainMonster.Initialize(new MonsterBaseStats { maxHp = 100 });
            typeof(Monster).GetMethod("Start", flags).Invoke(plainMonster, null);
            return new { health = "immediate + delayed white", flinch = "3 -> 2 -> 1 -> 0 -> reset 3", initialization = "Monster.Start -> child UI.Init(this)", withoutUI = "supported", execution = "hidden at zero, stored damage / remaining HP, updates on damage and clear" };
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }
}
