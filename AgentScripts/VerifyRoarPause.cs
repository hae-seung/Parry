using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using Parry.PlayerClass;
using Parry.MonsterClass;
using Parry.AnimationClass;
public static class VerifyRoarPause
{
    private static void Set(object target, string name, object value) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    private static void Property(object target, string name, object value) => target.GetType().GetProperty(name).SetValue(target, value);
    private static void Check(bool ok, string msg) { if(!ok) throw new Exception(msg); }
    public static async Task<object> Run()
    {
        Check(Application.isPlaying, "Play required");
        bool background = Application.runInBackground;
        Application.runInBackground = true;
        try
        {
            var character = UnityEngine.Object.FindAnyObjectByType<PlayerCharacter>();
            var mode = character.GetComponent<PlayerBerseker>();
            var gauge = character.GetComponent<PlayerParryGauge>();
            var movement = character.GetComponent<PlayerMovement>();
            var attack = character.GetComponent<PlayerAttack>();
            var parry = character.GetComponent<PlayerParry>();
            var reaction = character.GetComponent<PlayerHitReaction>();
            Set(gauge, "decayPerSecond", 0f);
            Set(gauge, "currentGauge", 100f);
            Action reject = () => { character.Skill(); Check(!mode.IsBerserking && gauge.CurrentGauge == 100, "Invalid action allowed entry"); };
            Property(movement, "IsGrounded", false); reject();
            Property(movement, "IsGrounded", true);
            Set(movement, "jumpRequested", true); reject(); Set(movement, "jumpRequested", false);
            Property(character, "IsRolling", true); reject(); Property(character, "IsRolling", false);
            Set(attack, "bIsAttacking", true); reject(); Set(attack, "bIsAttacking", false);
            Set(parry, "isParrying", true); Set(parry, "parryEndTime", Time.time + 100); reject(); Set(parry, "isParrying", false);
            Property(reaction, "IsReacting", true); reject(); Property(reaction, "IsReacting", false);
            Property(reaction, "IsReacting", true); Property(reaction, "IsFainted", true); reject(); Property(reaction, "IsFainted", false); Property(reaction, "IsReacting", false);
            character.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
            Property(movement, "IsGrounded", true);
            character.Skill();
            Check(mode.IsRoaring && Time.timeScale == 0 && gauge.CurrentGauge == 100f - (float)mode.GetType().GetField("gaugeCost", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(mode), $"Roar did not pause: roaring={mode.IsRoaring}, scale={Time.timeScale}, gauge={gauge.CurrentGauge}, grounded={character.IsGrounded}, attacking={character.IsAttacking}, parrying={character.IsParrying}, hit={character.IsHitReacting}, clip={character.GetComponent<AnimationClipPlayer>().CanPlay}");
            Check(!attack.TryStartAttack(), "Attack accepted during roar");
            Check(!parry.TryStartParry(), "Parry accepted during roar");
            character.Attack(); character.Parry(); character.Skill();
            Check(mode.IsRoaring && mode.IsBerserking && !character.IsAttacking && !character.IsParrying, "Forwarded input bypassed internal checks");
            int remaining = mode.RemainingSeconds;
            float pausedTime = Time.time;
            var monster = UnityEngine.Object.FindAnyObjectByType<MonsterBrain>();
            var monsterPos = monster.transform.position;
            var state = monster.State;
            await Task.Delay(150);
            Check(mode.IsRoaring && Time.time == pausedTime, "Game clock advanced during roar");
            Check(mode.RemainingSeconds == remaining, "Berserker countdown ran during roar");
            Check(monster.transform.position == monsterPos && monster.State == state, "Monster advanced during pause");
            for(int i=0;i<250 && mode.IsRoaring;i++) await Task.Delay(20);
            Check(!mode.IsRoaring && Time.timeScale == 1 && mode.IsBerserking, "Unscaled roar failed or time not restored");
            float resumedTime = Time.time;
            for(int i=0;i<250 && mode.RemainingSeconds == remaining;i++) await Task.Delay(20);
            Check(mode.RemainingSeconds == remaining-1 && Time.time-resumedTime>=.9f, "Countdown did not start after roar");
            mode.EndBerserker(false);
            Set(gauge, "currentGauge", 100f); Property(movement, "IsGrounded", true);
            character.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
            Time.timeScale = .5f;
            character.Skill();
            Check(mode.IsRoaring && Time.timeScale == 0, "Second roar failed");
            mode.enabled = false;
            Check(!mode.IsRoaring && !mode.IsBerserking && Time.timeScale == .5f, "Disable did not restore previous time scale");
            return "Passed: airborne/jump/roll/attack/parry/hit/faint rejection without cost, game/monster freeze, unscaled roar completion, delayed countdown, disable restores previous time scale.";
        }
        finally { Time.timeScale = 1; Application.runInBackground = background; }
    }
}



