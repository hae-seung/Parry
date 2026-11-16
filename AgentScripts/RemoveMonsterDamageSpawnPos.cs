using UnityEditor;
using UnityEngine;

public static class RemoveMonsterDamageSpawnPos
{
    public static object Run()
    {
        foreach (string path in new[] { "Assets/Prefabs/Monster/Goblin.prefab", "Assets/Prefabs/Monster/TrainingDummy.prefab" })
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var marker = root.transform.Find("DamageSpawnPos");
                if (marker != null) Object.DestroyImmediate(marker.gameObject);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        return "Removed obsolete DamageSpawnPos markers and serialized references from Goblin and TrainingDummy prefabs.";
    }
}
