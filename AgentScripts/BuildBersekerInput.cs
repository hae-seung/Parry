using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Parry.PlayerClass;
public static class BuildBersekerInput
{
    public static void Configure()
    {
        const string path = "Assets/Prefabs/Player/Player.prefab";
        var player = PrefabUtility.LoadPrefabContents(path);
        try
        {
            if (!player.GetComponent<PlayerBerseker>()) player.AddComponent<PlayerBerseker>();
            var input = player.GetComponent<PlayerInput>();
            var action = input.actions.FindAction("Player/Skill", true);
            var events = input.actionEvents.ToList();
            var evt = events.Find(e => e.actionId == action.id.ToString());
            if (evt == null) { evt = new PlayerInput.ActionEvent(action); events.Add(evt); }
            bool connected = Enumerable.Range(0, evt.GetPersistentEventCount()).Any(i => evt.GetPersistentMethodName(i) == "OnSkill");
            if (!connected) UnityEventTools.AddPersistentListener<InputAction.CallbackContext>(evt, player.GetComponent<MyController>().OnSkill);
            input.actionEvents = events.ToArray();
            PrefabUtility.SaveAsPrefabAsset(player, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(player); }
    }
    private static void Set(object target, string field, object value) => target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    private static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    public static object Verify()
    {
        Check(Application.isPlaying, "Play mode required");
        var player = UnityEngine.Object.FindAnyObjectByType<MyController>();
        var input = player.GetComponent<PlayerInput>();
        var gauge = player.GetComponent<PlayerParryGauge>();
        var mode = player.GetComponent<PlayerBerseker>();
        Set(gauge, "decayPerSecond", 0f);
        var keyboard = input.devices.OfType<Keyboard>().First();
        var oldBehavior = InputSystem.settings.backgroundBehavior;
        try
        {
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            input.ActivateInput();
            input.SwitchCurrentActionMap("Player");
            KeyState(keyboard, false);
            Set(gauge, "currentGauge", 69f);
            KeyState(keyboard, true);
            Check(!mode.IsBerserking && gauge.CurrentGauge == 69f, "69 should not enter or consume");
            KeyState(keyboard, false);
            Set(gauge, "currentGauge", 70f);
            KeyState(keyboard, true);
            Check(mode.IsBerserking && gauge.CurrentGauge == 0f, "70 should enter and consume 70");
            KeyState(keyboard, true);
            Check(mode.IsBerserking, "Holding R toggled again");
            KeyState(keyboard, false);
            Check(mode.IsBerserking, "Releasing R exited mode");
            KeyState(keyboard, true);
            Check(!mode.IsBerserking && gauge.CurrentGauge == 0f, "Second press failed to exit");
            KeyState(keyboard, false);
            Set(gauge, "currentGauge", 100f);
            KeyState(keyboard, true);
            Check(mode.IsBerserking && gauge.CurrentGauge == 30f, "100 should leave 30");
            return "R input passed: 69 rejected, 70 consumed, hold/release stable, second press exits at zero, 100 leaves 30.";
        }
        finally { KeyState(keyboard, false); InputSystem.settings.backgroundBehavior = oldBehavior; }
    }
    private static void KeyState(Keyboard keyboard, bool pressed)
    {
        InputSystem.QueueStateEvent(keyboard, pressed ? new KeyboardState(Key.R) : new KeyboardState());
        InputSystem.Update();
    }
}
