using System;
using System.Threading.Tasks;
using Parry.PlayerClass;
using Parry.MonsterClass;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class VerifyGroundCombo
{
    public static async Task<object> Run()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Enter Play mode first.");
        var player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
        var attack = player.GetComponent<PlayerAttack>();
        var input = player.GetComponent<PlayerInput>();
        var oldBackgroundBehavior = InputSystem.settings.backgroundBehavior;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        input.ActivateInput();
        input.SwitchCurrentActionMap("Player");
        var monster = UnityEngine.Object.FindAnyObjectByType<Monster>();
        var body = player.GetComponent<Rigidbody2D>();
        var monsterBody = monster.GetComponent<Rigidbody2D>();
        Mouse mouse = Mouse.current;
        foreach (var device in input.devices)
            if (device is Mouse paired) mouse = paired;
        if (mouse == null) throw new InvalidOperationException("No mouse.");
        monster.Initialize(new MonsterBaseStats { maxHp = 500f });
        monsterBody.bodyType = RigidbodyType2D.Kinematic;
        monsterBody.linearVelocity = Vector2.zero;
        foreach (var collider in monster.GetComponentsInChildren<Collider2D>()) collider.isTrigger = true;
        body.position = new Vector2(0f, -2.2f);
        body.linearVelocity = Vector2.zero;
        monsterBody.position = new Vector2(1.7f, -2.2f);
        var duplicate = new GameObject("ComboVerificationHurtbox");
        duplicate.layer = monster.gameObject.layer;
        duplicate.transform.SetParent(monster.transform, false);
        duplicate.AddComponent<CircleCollider2D>().isTrigger = true;
        Physics2D.SyncTransforms();
        try
        {
            Release(mouse);
            await Task.Delay(200);
            Check(player.IsGrounded, "Not grounded.");

            // A held click must stop after the first strike.
            Press(mouse);
            await Until(() => attack.IsAttacking);
            await Until(() => !attack.IsAttacking);
            await Task.Delay(200);
            Check(monster.Hp == 480f, "Hold repeated attacks or duplicate damage.");
            Release(mouse);
            await Task.Delay(50);

            await Tap(mouse);
            Check(attack.ComboStep == 1, "First click did not start first strike.");
            await Tap(mouse);
            Check(attack.HasQueuedAttack, "Second click was not buffered.");
            Check(!attack.TryStartAttack(), "Multiple clicks accumulated beyond one queued strike.");
            await Until(() => attack.ComboStep == 2);
            await Tap(mouse);
            await Until(() => attack.ComboStep == 3);
            Check(!attack.TryStartAttack(), "Third strike queued an extra attack.");
            await Until(() => attack.Phase == PlayerAttack.AttackPhase.Recovery);
            double thirdRecoveryStart = Time.timeAsDouble;
            await Until(() => !attack.IsAttacking);
            Check(Time.timeAsDouble - thirdRecoveryStart >= 0.34, "Third strike recovery was too short.");
            Check(monster.Hp == 405f, "Combo did not deal 20 + 20 + 35 once per strike.");

            await Tap(mouse);
            Check(attack.ComboStep == 1, "Combo did not reset to first strike.");
            await Tap(mouse);
            attack.CancelAttack();
            await Task.Delay(800);
            Check(!attack.IsAttacking && !attack.HasQueuedAttack && monster.Hp == 405f, "Cancel left a queued attack or late damage.");

            // Only the next strike is buffered; two clicks must produce exactly two strikes.
            await Tap(mouse);
            await Tap(mouse);
            await Until(() => attack.ComboStep == 2);
            await Until(() => !attack.IsAttacking);
            Check(monster.Hp == 365f, "Two clicks did not stop after second strike.");

            body.position = new Vector2(0f, 1f);
            body.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
            await Task.Delay(50);
            Check(!attack.TryStartAttack(), "Ground combo started in air.");
            return new { passed = true, damage = "20 / 20 / 35", checks = "real mouse input, hold stops at one, 3-strike sequence, one-slot buffer, per-strike deduplication, third recovery, cancellation clears queue, reset, 2-click stop, ground-only" };
        }
        finally
        {
            Release(mouse);
            InputSystem.settings.backgroundBehavior = oldBackgroundBehavior;
            UnityEngine.Object.Destroy(duplicate);
        }
    }

    private static void Press(Mouse mouse) => InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left));
    private static void Release(Mouse mouse) => InputSystem.QueueStateEvent(mouse, new MouseState());
    private static async Task Tap(Mouse mouse) { Press(mouse); await Task.Delay(30); Release(mouse); await Task.Delay(30); }
    private static async Task Until(Func<bool> predicate)
    {
        double deadline = Time.realtimeSinceStartupAsDouble + 3;
        while (!predicate())
        {
            if (Time.realtimeSinceStartupAsDouble > deadline) throw new InvalidOperationException("Timed out waiting for combo state.");
            await Task.Delay(5);
        }
    }
    private static void Check(bool success, string message) { if (!success) throw new InvalidOperationException(message); }
}
