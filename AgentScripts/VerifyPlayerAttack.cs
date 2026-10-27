using System;
using System.Threading.Tasks;
using Parry.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using MonsterTarget = Parry.Monster.Monster;

public static class VerifyPlayerAttack
{
    public static async Task<object> Run()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Enter Play mode first.");
        var player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
        var attack = player.GetComponent<PlayerAttack>();
        var input = player.GetComponent<PlayerInput>();
        var monster = UnityEngine.Object.FindAnyObjectByType<MonsterTarget>();
        var body = player.GetComponent<Rigidbody2D>();
        var monsterBody = monster.GetComponent<Rigidbody2D>();
        var mouse = Mouse.current;
        if (mouse == null) throw new InvalidOperationException("No mouse device.");
        foreach (var device in input.devices) if (device is Mouse pairedMouse) mouse = pairedMouse;
        monsterBody.bodyType = RigidbodyType2D.Kinematic;
        monsterBody.linearVelocity = Vector2.zero;
        foreach (var collider in monster.GetComponentsInChildren<Collider2D>()) collider.isTrigger = true;
        var extra = new GameObject("VerificationExtraHurtbox");
        extra.layer = monster.gameObject.layer;
        extra.transform.SetParent(monster.transform, false);
        extra.AddComponent<CircleCollider2D>().isTrigger = true;
        body.position = new Vector2(0f, -2.2f);
        monsterBody.position = new Vector2(1.7f, -2.2f);
        Physics2D.SyncTransforms();
        await Task.Delay(300);
        Check(player.IsGrounded, "Player is not grounded.");
        float startHp = monster.Hp;

        Press(mouse);
        await Task.Delay(40);
        Check(attack.IsAttacking, "Left click did not invoke attack.");
        Check(monster.Hp == startHp, "Windup dealt damage.");
        await Until(() => monster.Hp < startHp, "Attack never hit.");
        Check(Mathf.Approximately(monster.Hp, startHp - 20f), "Duplicate colliders dealt duplicate damage.");
        Check(monster.GetComponent<SpriteRenderer>().color == Color.red, "Hit flash missing.");
        Check(!attack.TryStartAttack(), "Attack restarted during active/recovery.");
        await Task.Delay(700);
        Check(monster.Hp == startHp - 20f, "Holding click repeated damage/attack.");
        Check(!attack.IsAttacking, "Attack did not finish.");
        Check(monster.GetComponent<SpriteRenderer>().color != Color.red, "Hit flash did not restore.");
        Release(mouse);
        await Task.Delay(50);

        monsterBody.position = new Vector2(5f, -2.2f);
        Physics2D.SyncTransforms();
        await Click(mouse);
        Check(monster.Hp == startHp - 20f, "Out-of-range attack hit.");
        monsterBody.position = new Vector2(-1.7f, -2.2f);
        Physics2D.SyncTransforms();
        await Click(mouse);
        Check(monster.Hp == startHp - 20f, "Attack hit behind player.");

        monsterBody.position = new Vector2(1.7f, -2.2f);
        Physics2D.SyncTransforms();
        Check(attack.TryStartAttack(), "Could not start cancellation check.");
        attack.CancelAttack();
        await Task.Delay(550);
        Check(monster.Hp == startHp - 20f, "Cancelled windup still dealt damage.");

        body.position = new Vector2(0f, 1f);
        body.linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();
        await Task.Delay(50);
        Check(!attack.TryStartAttack(), "Ground attack accepted in air.");
        body.position = new Vector2(0f, -2.2f);
        body.linearVelocity = Vector2.zero;
        await Task.Delay(150);

        for (int i = 0; i < 4; i++) await Click(mouse);
        Check(monster.IsDead && monster.Hp == 0f && !monster.gameObject.activeSelf, "Lethal hits did not deactivate monster.");
        return new { passed = true, checks = "left-click event, windup, single hit across multiple colliders/frames, hold prevention, recovery lock, flash restore, range, rear miss, cancellation, ground-only, five-hit death", hp = monster.Hp, frames = Time.frameCount };
    }

    private static void Press(Mouse mouse) => InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left));
    private static void Release(Mouse mouse) => InputSystem.QueueStateEvent(mouse, new MouseState());
    private static async Task Click(Mouse mouse)
    {
        Press(mouse);
        await Task.Delay(60);
        Release(mouse);
        await Task.Delay(550);
    }
    private static async Task Until(Func<bool> condition, string failure)
    {
        double deadline = Time.realtimeSinceStartupAsDouble + 2;
        while (!condition())
        {
            if (Time.realtimeSinceStartupAsDouble > deadline) throw new InvalidOperationException(failure);
            await Task.Delay(10);
        }
    }
    private static void Check(bool success, string message)
    {
        if (!success) throw new InvalidOperationException(message);
    }
}
