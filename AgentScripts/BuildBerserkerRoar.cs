using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using Parry.PlayerClass;
using Parry.MonsterClass;
using Parry.AnimationClass;
public static class BuildBerserkerRoar
{
    public static object Run()
    {
        const string path = "Assets/Animations/Player/Player_BerserkerRoar.anim";
        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(path)) throw new Exception("Clip already exists.");
        var sprites = AssetDatabase.LoadAllAssetsAtPath("Assets/AssetStoreImport/Hero Knight - Pixel Art/Sprites/HeroKnight.png").OfType<Sprite>().ToArray();
        var sequence = new[] { 78, 47, 46 }.Select(i => sprites.Single(s => s.name == "HeroKnight_" + i)).ToArray();
        var clip = new AnimationClip { name = "Player_BerserkerRoar", frameRate = 25 };
        AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), new[] {
            new ObjectReferenceKeyframe { time = 0, value = sequence[0] },
            new ObjectReferenceKeyframe { time = .12f, value = sequence[1] },
            new ObjectReferenceKeyframe { time = .24f, value = sequence[2] },
            new ObjectReferenceKeyframe { time = .6f, value = sequence[2] }
        });
        AnimationUtility.SetAnimationEvents(clip, new[] { new AnimationEvent { time = clip.length, functionName = "EndRoar" } });
        AssetDatabase.CreateAsset(clip, path);
        var player = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Player/Player.prefab");
        try
        {
            var so = new SerializedObject(player.GetComponent<PlayerBerseker>());
            so.FindProperty("roarClip").objectReferenceValue = clip;
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(player, "Assets/Prefabs/Player/Player.prefab");
        }
        finally { PrefabUtility.UnloadPrefabContents(player); }
        AssetDatabase.SaveAssets();
        return new { path, frames = sequence.Select(s => s.name).ToArray(), length = clip.length };
    }
    private static void Check(bool ok, string msg) { if (!ok) throw new Exception(msg); }
    public static async Task<object> Verify()
    {
        Check(Application.isPlaying, "Play mode required");
        bool background = Application.runInBackground;
        Application.runInBackground = true;
        try
        {
            foreach(var brain in UnityEngine.Object.FindObjectsByType<MonsterBrain>()) brain.enabled = false;
            var character = UnityEngine.Object.FindAnyObjectByType<PlayerCharacter>();
            var mode = character.GetComponent<PlayerBerseker>();
            var gauge = character.GetComponent<PlayerParryGauge>();
            var body = character.GetComponent<Rigidbody2D>();
            var clipPlayer = character.GetComponent<AnimationClipPlayer>();
            for(int i=0;i<7;i++) gauge.GainOnParrySuccess();
            character.Move(1f);
            character.Skill();
            Check(mode.IsBerserking && mode.IsRoaring, "Roar did not begin");
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animations/Player/Player_BerserkerRoar.anim");
            Check(clipPlayer.IsPlayingClip(clip), "Roar clip not playing");
            character.Attack(); character.Parry(); character.Roll(); character.Jump(); character.Skill();
            Check(!character.IsAttacking && !character.IsParrying && !character.IsRolling && mode.IsBerserking, "Input bypassed roar lock");
            for(int i=0;i<100 && Time.deltaTime==0;i++) await Task.Delay(20);
            await Task.Delay(100);
            Check(Mathf.Abs(body.linearVelocity.x)<.001f, "Held movement was not blocked");
            for(int i=0;i<250 && mode.IsRoaring;i++) await Task.Delay(20);
            Check(!mode.IsRoaring && mode.IsBerserking, "End event did not unlock");
            Check(!clipPlayer.IsPlayingClip(clip), "Roar clip not stopped");
            character.Move(0f);
            character.Skill();
            Check(!mode.IsBerserking, "Skill did not unlock");
            for(int i=0;i<7;i++) gauge.GainOnParrySuccess();
            character.Skill();
            Check(mode.IsRoaring, "Reentry did not roar");
            mode.enabled = false;
            Check(!mode.IsRoaring && !mode.IsBerserking, "Disable left input locked");
            return "Passed: 78/47/46 clip playback, attack/parry/roll/jump/R lock, held movement lock, automatic event unlock, reentry and disable cleanup.";
        }
        finally { Application.runInBackground = background; }
    }
}

