using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using Parry.PlayerClass;
using Parry.MonsterClass;
public static class VerifyBerserkerCooldown
{
    private static void Set(object obj,string field,object value) => obj.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(obj,value);
    private static void Check(bool ok,string msg) { if(!ok) throw new Exception(msg); }
    private static async Task Until(Func<bool> done)
    {
        for(int i=0;i<300 && !done();i++) await Task.Delay(20);
        Check(done(),"Timed out");
    }
    public static async Task<object> Run()
    {
        bool background=Application.runInBackground;
        Application.runInBackground=true;
        try
        {
            foreach(var brain in UnityEngine.Object.FindObjectsByType<MonsterBrain>()) brain.enabled=false;
            var c=UnityEngine.Object.FindAnyObjectByType<PlayerCharacter>();
            var mode=c.GetComponent<PlayerBerseker>();
            var gauge=c.GetComponent<PlayerParryGauge>();
            Set(mode,"cooldown",.4f); Set(mode,"duration",1); Set(gauge,"decayPerSecond",0f);
            await Until(()=>c.IsGrounded);
            Set(gauge,"currentGauge",100f);
            c.Skill(); Check(mode.IsRoaring,"Entry failed");
            float afterEntry=gauge.CurrentGauge;
            c.Skill(); Check(mode.IsRoaring && mode.IsBerserking && gauge.CurrentGauge==afterEntry,"R cancelled roar");
            await Until(()=>!mode.IsRoaring);
            c.Skill(); Check(!mode.IsBerserking,"Manual end failed");
            float endTime=Time.time;
            Set(gauge,"currentGauge",100f); c.Skill();
            Check(!mode.IsBerserking && gauge.CurrentGauge==100f,"Cooldown allowed entry or consumed gauge");
            mode.EndBerserker(false);
            await Until(()=>Time.time>=endTime+.4f);
            c.Skill(); Check(mode.IsBerserking,"Cooldown expiration did not allow entry");
            await Until(()=>!mode.IsBerserking);
            Set(gauge,"currentGauge",100f); c.Skill();
            Check(!mode.IsBerserking && gauge.CurrentGauge==100f,"Automatic end did not start cooldown");
            return "Passed: roar R lock, manual end cooldown, no gauge loss on rejection, reentry after cooldown, automatic end cooldown.";
        }
        finally { Time.timeScale=1; Application.runInBackground=background; }
    }
}
