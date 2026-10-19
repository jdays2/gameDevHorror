using UnityEngine;
using EmergencyShift.Features.PlayerInput.Logic;
using Zenject;

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
        private IPlayerInput input;
        private GameplayInputBlock inputBlock;

        [Inject]
        private void Construct(IPlayerInput playerInput, GameplayInputBlock block)
        {
            input = playerInput;
            inputBlock = block;
        }
        private void Awake() => body = GetComponent<Rigidbody2D>();
        private void Update()
        {
            if (inputBlock.IsBlocked)
            {
                movement = Vector2.zero;
                return;
            }

            // Нормализация не даёт двигаться быстрее по диагонали.
            movement = input.Movement.normalized;
        }

        private void FixedUpdate()
        {
            body.linearVelocity = inputBlock.IsBlocked ? Vector2.zero : movement * moveSpeed;
        }
        private void OnDisable()
        {
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
            }
        }
    }
}
