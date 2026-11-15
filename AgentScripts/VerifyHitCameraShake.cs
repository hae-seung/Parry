using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using Parry.UIClass;
using Unity.Cinemachine;
using Parry.PlayerClass;
using Parry.MonsterClass;
using Parry.CombatClass;

public static class VerifyHitCameraShake
{
    public static async Task<string> Run()
    {
        if (!Application.isPlaying) throw new Exception("Play 모드 필요");
        foreach (var brain in UnityEngine.Object.FindObjectsByType<MonsterBrain>()) brain.enabled = false;
        var ui = UnityEngine.Object.FindAnyObjectByType<UIManager>();
        if (UIManager.Instance != ui) throw new Exception("싱글톤 등록 실패");
        var player = UnityEngine.Object.FindAnyObjectByType<PlayerCharacter>();
        if (player.GetComponent<CinemachineImpulseSource>() != null)
            throw new Exception("플레이어에 Impulse Source 잔존");
        var feedback = UnityEngine.Object.FindAnyObjectByType<Parry.CameraClass.CameraFeedback>();
        if (feedback == null || feedback.GetComponent<CinemachineCamera>() != null)
            throw new Exception("공용 카메라 효과 분리 실패");
        var monster = UnityEngine.Object.FindAnyObjectByType<Monster>();
        var camera = Camera.main;
        await Task.Delay(500);
        var baseline = camera.transform.position;
        var attack = AssetDatabase.LoadAssetAtPath<MonsterAttackData>("Assets/Data/Monster/Attacks/Goblin/Goblin_Attack1.asset");
        float hp = player.Stats.Hp;
        player.TakeDamage(new AttackHitContext(attack, monster, 1));
        float end = Time.time + 0.3f;
        if (player.Stats.Hp != hp - attack.Damage) throw new Exception("피격 실패");
        player.TakeDamage(new AttackHitContext(attack, monster, 1));
        if (player.Stats.Hp != hp - attack.Damage) throw new Exception("무적 실패");
        bool moved = false;
        while (Time.time < end)
        {
            moved |= camera.transform.position != baseline;
            await Task.Delay(5);
        }
        await Task.Delay(80);
        if (!moved || Vector3.Distance(camera.transform.position, baseline) > 0.0001f) throw new Exception($"카메라 흔들림 또는 원위치 복구 실패 moved={moved} start={baseline} end={camera.transform.position}");
        if (GameObject.Find("PlayerScreenDamage") != null) throw new Exception("ScreenDamage 잔존");
        return "PASS: singleton registration, damage-triggered camera motion, invulnerability filtering, position restored, ScreenDamage removed.";
    }
}



