using UnityEngine;

namespace PnceHarekat
{
    /// <summary>
    /// Wraps Unity's input API so that the gameplay systems mirror the structure
    /// of the HTML prototype. Keeping this logic isolated makes it easier to
    /// replace with a different input backend in the future (for example the new
    /// Input System) without touching the gameplay code.
    /// </summary>
    public static class PlayerInputRouter
    {
        public static Vector2 ReadMovement()
        {
            var movement = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            if (movement.sqrMagnitude > 1f)
            {
                movement.Normalize();
            }

            return movement;
        }

        public static bool ConsumeDashPress()
        {
            return Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift);
        }

        public static bool ConsumeShieldPress()
        {
            return Input.GetKeyDown(KeyCode.Space);
        }
    }
}
