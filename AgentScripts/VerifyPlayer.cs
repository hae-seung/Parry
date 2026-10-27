using System;
using System.Threading.Tasks;
using Parry.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class VerifyPlayer
{
    public static async Task<object> Run()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Enter Play mode first.");
        var player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
        var body = player.GetComponent<Rigidbody2D>();
        var keyboard = InputSystem.AddDevice<Keyboard>();
        try
        {
            await Task.Delay(1000);
            float startX = body.position.x;
            float floorY = body.position.y;
            Check(player.IsGrounded, "Player did not land.");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
            await Task.Delay(500);
            Check(body.position.x > startX + 1f, "D did not move right.");
            Check(player.FacingDirection == 1, "Right facing failed.");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.A));
            await Task.Delay(300);
            Check(body.linearVelocity.x < -5f && player.FacingDirection == -1, "A did not move/facing left.");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space));
            await Task.Delay(200);
            Check(body.position.y > floorY + 0.6f, "Space did not jump.");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            await Task.Delay(80);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space, Key.D));
            await Task.Delay(150);
            Check(body.linearVelocity.y < 6f, "Unexpected second jump in air.");
            Check(body.linearVelocity.x > 5f, "Air control failed.");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            await Task.Delay(1000);
            Check(player.IsGrounded && Mathf.Abs(body.position.y - floorY) < 0.1f, "Landing failed.");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
            await Task.Delay(2400);
            Check(body.position.x < 11.3f, "Player passed through right wall.");
            Check(player.IsGrounded, "Wall contact caused loss of ground.");
            return new { passed = true, checks = "landing, A/D movement, facing, jump, no double jump, air control, wall collision", position = body.position.ToString(), frame = Time.frameCount };
        }
        finally
        {
            InputSystem.RemoveDevice(keyboard);
        }
    }

    private static void Check(bool success, string message)
    {
        if (!success) throw new InvalidOperationException(message);
    }
}
