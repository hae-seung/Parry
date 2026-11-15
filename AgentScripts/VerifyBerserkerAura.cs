using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Parry.PlayerClass;
public static class VerifyBerserkerAura
{
    public static object Run()
    {
        var mode = UnityEngine.Object.FindAnyObjectByType<PlayerBerseker>();
        var gauge = mode.GetComponent<PlayerParryGauge>();
        for(int i=0;i<7;i++) gauge.GainOnParrySuccess();
        mode.ToggleBerserker();
        var aura = UnityEngine.Object.FindObjectsByType<Animator>().Single(a => a.name == "Berserker_Aura(Clone)").gameObject;
        Check(aura.transform.parent == mode.transform, "Not following player");
        Check(aura.transform.localPosition == Vector3.zero, "Wrong foot offset");
        var sr = aura.GetComponent<SpriteRenderer>();
        Check(sr.color.r > sr.color.g && sr.color.r > sr.color.b, "Not red tinted");
        Check(aura.GetComponent<Animator>().runtimeAnimatorController.animationClips[0].isLooping, "Not looping");
        mode.ToggleBerserker();
        Check(!aura.activeSelf, "Manual end did not release");
        for(int i=0;i<7;i++) gauge.GainOnParrySuccess();
        mode.ToggleBerserker();
        Check(aura.activeSelf && aura.transform.parent == mode.transform, "Pool did not reuse");
        mode.EndBerserker(false);
        Check(!aura.activeSelf, "Discard end did not release");
        for(int i=0;i<7;i++) gauge.GainOnParrySuccess();
        mode.ToggleBerserker();
        return "Passed: player parent/foot position, red tint, looping clip, manual/discard return and reuse. Active for preview.";
    }
    private static void Check(bool ok, string msg) { if(!ok) throw new Exception(msg); }
}
