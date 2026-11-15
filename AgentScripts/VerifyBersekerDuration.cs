using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using Parry.PlayerClass;
public static class VerifyBersekerDuration
{
    private static void Set(object target, string name, object value) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    private static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    public static async Task<object> Run()
    {
        Check(Application.isPlaying, "Play mode required");
        bool background = Application.runInBackground;
        Application.runInBackground = true;
        try
        {
            var mode = UnityEngine.Object.FindAnyObjectByType<PlayerBerseker>();
            var gauge = mode.GetComponent<PlayerParryGauge>();
            Set(gauge, "decayPerSecond", 0f);
            Set(mode, "duration", 2);
            for (int i = 0; i < 7; i++) gauge.GainOnParrySuccess();
            mode.ToggleBerserker();
            Check(mode.IsBerserking && mode.RemainingSeconds == 2, "Initial time failed");
            float startTime = Time.time;
            for (int i = 0; i < 250 && mode.RemainingSeconds == 2; i++) await Task.Delay(20);
            Check(Time.time - startTime >= 1f, "Countdown ticked before a game second");
            Check(mode.IsBerserking && mode.RemainingSeconds == 1, "One-second decrement failed");
            for (int i = 0; i < 250 && mode.IsBerserking; i++) await Task.Delay(20);
            Check(!mode.IsBerserking && mode.RemainingSeconds == 0, "Auto end failed");
            for (int i = 0; i < 7; i++) gauge.GainOnParrySuccess();
            mode.ToggleBerserker();
            mode.ToggleBerserker();
            Check(!mode.IsBerserking && mode.RemainingSeconds == 0, "Manual end cleanup failed");
            for (int i = 0; i < 7; i++) gauge.GainOnParrySuccess();
            mode.ToggleBerserker();
            Check(mode.RemainingSeconds == 2, "Restart did not reset duration");
            mode.enabled = false;
            await Task.Delay(1100);
            Check(!mode.IsBerserking && mode.RemainingSeconds == 0, "Disable cleanup failed");
            return "Passed: duration initialization, 1-second decrement, zero auto end, manual cleanup, restart, disable cleanup.";
        }
        finally { Application.runInBackground = background; }
    }
}
