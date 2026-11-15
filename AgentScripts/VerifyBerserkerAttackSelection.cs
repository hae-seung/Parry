using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Parry.PlayerClass;
using Parry.AnimationClass;

public static class VerifyBerserkerAttackSelection
{
    public static object Run()
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var scene = EditorSceneManager.NewPreviewScene();
        GameObject clone = null;
        PlayerBerseker mode = null;
        var modeState = typeof(PlayerBerseker).GetField("<IsBerserking>k__BackingField", flags);
        try
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/Player.prefab");
            clone = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            var attack = clone.GetComponent<PlayerAttack>();
            var character = clone.GetComponent<PlayerCharacter>();
            var player = clone.GetComponent<AnimationClipPlayer>();
            mode = clone.GetComponent<PlayerBerseker>();
            typeof(AnimationClipPlayer).GetMethod("Awake", flags).Invoke(player, null);
            typeof(PlayerCharacter).GetMethod("Awake", flags).Invoke(character, null);
            typeof(PlayerAttack).GetMethod("Awake", flags).Invoke(attack, null);
            var data = new SerializedObject(attack);
            var normal = data.FindProperty("attacks");
            var berserker = data.FindProperty("berserkerAttacks");
            if (normal.arraySize != 3 || berserker.arraySize != 3) throw new Exception("Expected three attacks in each list");
            var start = typeof(PlayerAttack).GetMethod("StartAttack", flags);
            var current = typeof(PlayerAttack).GetField("currentAttack", flags);
            var indexField = typeof(PlayerAttack).GetField("attackIndex", flags);
            for (int i = 0; i < 3; i++)
            {
                var original = (PlayerAttackData)normal.GetArrayElementAtIndex(i).objectReferenceValue;
                var special = (BerserkerAttackData)berserker.GetArrayElementAtIndex(i).objectReferenceValue;
                if (special.SlashStackCount != (i == 2 ? 2 : 1)) throw new Exception("Stack count mismatch");
                if (special.Damage != original.Damage || Vector2.Distance(special.HitboxSize, original.HitboxSize * 1.2f) > .0001f) throw new Exception("Attack data mismatch");
                modeState.SetValue(mode, true);
                if (!(bool)start.Invoke(attack, new object[] { i }) || (PlayerAttackData)current.GetValue(attack) != special || !player.IsPlayingClip(special.AnimationClip)) throw new Exception("Berserker selection failed");
                modeState.SetValue(mode, false);
                if ((PlayerAttackData)current.GetValue(attack) != special || !player.IsPlayingClip(special.AnimationClip)) throw new Exception("Mode change interrupted current attack");
                // Successful hit advances the existing shared index; finishing preserves it.
                indexField.SetValue(attack, i + 1);
                attack.EndAttackMotion();
                if ((int)indexField.GetValue(attack) != i + 1) throw new Exception("Combo index reset");
                int next = (i + 1) % 3;
                var expected = (PlayerAttackData)normal.GetArrayElementAtIndex(next).objectReferenceValue;
                if (!(bool)start.Invoke(attack, new object[] { i + 1 }) || (PlayerAttackData)current.GetValue(attack) != expected || !player.IsPlayingClip(expected.AnimationClip)) throw new Exception("Normal continuation failed");
                attack.CancelAttack();
            }
            return "Passed: prefab references, inherited data, stack counts 1/1/2, 20% larger hitboxes, all three berserker clips, current attack retained after mode exit, shared index and normal continuation.";
        }
        finally
        {
            if (mode != null) modeState.SetValue(mode, false);
            if (clone != null) UnityEngine.Object.DestroyImmediate(clone);
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }
}
