using UnityEngine;
using UnityEngine.InputSystem;

namespace Parry.PlayerClass
{
    [RequireComponent(typeof(PlayerInput))]
    public sealed class MyController : PlayerController
    {
        public void OnMove(InputAction.CallbackContext context)
        {
            if (context.performed || context.canceled)
                Character.Move(context.ReadValue<Vector2>().x);
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            if (context.performed)
                Character.Jump();
        }

        public void OnRoll(InputAction.CallbackContext context)
        {
            if (context.performed)
                Character.Roll();
        }

        public void OnParry(InputAction.CallbackContext context)
        {
            if (context.performed)
                Character.Parry();
        }

        public void OnAttack(InputAction.CallbackContext context)
        {
            if (context.performed)
                Character.Attack();
        }
    }
}
