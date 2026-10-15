using UnityEngine;
using UnityEngine.InputSystem;

namespace EmergencyShift
{
    /// <summary>Читает WASD и перемещает капсулу через физику Unity.</summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerMotor2D : MonoBehaviour
    {
        [SerializeField, Min(0.1f)]
        private float moveSpeed = 3.5f;
        private Rigidbody2D body;
        private Vector2 movement;
        private void Awake() => body = GetComponent<Rigidbody2D>();
        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                movement = Vector2.zero;
                return;
            }

            float x = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
            float y = (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);
            // Нормализация не даёт двигаться быстрее по диагонали.
            movement = new Vector2(x, y).normalized;
        }

        private void FixedUpdate() => body.linearVelocity = movement * moveSpeed;
        private void OnDisable()
        {
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
            }
        }
    }
}
