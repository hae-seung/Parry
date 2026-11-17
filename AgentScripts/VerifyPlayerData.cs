using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Parry.PlayerClass;
using Parry.UIClass;

public static class VerifyPlayerData
{
    private const BindingFlags Fields=BindingFlags.Instance|BindingFlags.NonPublic;

    public static object Run()
    {
        if(!Application.isPlaying)throw new Exception("Play first");
        var player=UnityEngine.Object.FindAnyObjectByType<PlayerCharacter>();
        var expected=player.Data;
        if(player.Stats.Hp!=expected.Stats.maxHp||player.Stats.Stamina!=expected.Stats.maxStamina||player.ParryGauge.CurrentGauge!=expected.ParryGauge.initialGauge)
            throw new Exception("Initial runtime stats differ from PlayerData");
        Check(player,"characterData",expected.Character);
        Check(player.Stats,"data",expected.Stats);
        Check(player.GetComponent<PlayerMovement>(),"data",expected.Movement);
        Check(player.GetComponent<PlayerAttack>(),"data",expected.Attack);
        Check(player.PlayerParryComponent,"data",expected.Parry);
        Check(player.ParryGauge,"data",expected.ParryGauge);
        Check(player.PlayerBerserker,"data",expected.Berserker);
        Check(player.GetComponent<PlayerHitReaction>(),"data",expected.HitReaction);
        var statsUI=UnityEngine.Object.FindAnyObjectByType<PlayerStatUI>();
        var health=(UnityEngine.UI.Slider)typeof(PlayerStatUI).GetField("healthSlider",Fields).GetValue(statsUI);
        var stamina=(UnityEngine.UI.Slider)typeof(PlayerStatUI).GetField("staminaSlider",Fields).GetValue(statsUI);
        if(health.value!=1f||stamina.value!=1f)throw new Exception("UI initial values failed");
        foreach(var monster in UnityEngine.Object.FindObjectsByType<Parry.MonsterClass.Monster>())monster.gameObject.SetActive(false);
        player.StartCoroutine(Exercise(player));
        return new{initialStats=true,allEightDataGroups=true,uiAfterAwake=true,behaviorCheck="running"};
    }

    private static void Check(Component component,string field,object expected)
    {
        object actual=component.GetType().GetField(field,Fields).GetValue(component);
        if(JsonUtility.ToJson(actual)!=JsonUtility.ToJson(expected))throw new Exception(component.GetType().Name+" data mismatch");
    }

    private static IEnumerator Exercise(PlayerCharacter player)
    {
        var data=player.Data;
        var movement=player.GetComponent<PlayerMovement>();
        var attack=player.GetComponent<PlayerAttack>();
        var parry=player.PlayerParryComponent;
        while(!player.IsGrounded)yield return null;
        float startX=player.transform.position.x;
        player.Move(1f);
        yield return new WaitForSeconds(.2f);
        player.Move(0f);
        if(player.transform.position.x<=startX)Debug.LogError("[PlayerDataVerification] movement failed");
        float stamina=player.Stats.Stamina;
        player.Roll();
        if(player.Stats.Stamina!=stamina-data.Character.rollStaminaCost)Debug.LogError("[PlayerDataVerification] roll cost failed");
        yield return new WaitForSeconds(1f);
        player.Stats.TakeDamage(10);
        var healthUI=UnityEngine.Object.FindAnyObjectByType<PlayerStatUI>();
        var health=(UnityEngine.UI.Slider)typeof(PlayerStatUI).GetField("healthSlider",Fields).GetValue(healthUI);
        if(Mathf.Abs(health.value-player.Stats.Hp/player.Stats.MaxHp)>.0001f)Debug.LogError("[PlayerDataVerification] health event failed");
        if(!parry.TryStartParry()||!parry.TryParryHit())Debug.LogError("[PlayerDataVerification] parry failed");
        yield return new WaitForSeconds(.5f);
        if(!attack.TryStartAttack())Debug.LogError("[PlayerDataVerification] attack failed");
        yield return new WaitForSeconds(1.2f);
        player.Skill();
        if(!player.PlayerBerserker.IsBerserking)Debug.LogError("[PlayerDataVerification] berserker entry failed");
        yield return new WaitForSecondsRealtime(1f);
        if(player.PlayerBerserker.IsRoaring)Debug.LogError("[PlayerDataVerification] roar failed to finish");
        player.PlayerBerserker.EndBerserker(false);
        Debug.Log("[PlayerDataVerification] completed movement, roll, damage/UI, parry, attack, berserker");
    }
}
