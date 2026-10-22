using UnityEngine;
using EmergencyShift.Features.PlayerMovement;
using EmergencyShift.Features.PlayerMovement.Logic;
using Zenject;

namespace EmergencyShift
{
    /// <summary>Legacy authored speed and native Rigidbody adapter for the personal movement controller.</summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerMotor2D : MonoBehaviour
    {
        [SerializeField, Min(0.1f)]
        private float moveSpeed = 3.5f;
        [SerializeField, Tooltip("Keep all authored values, even old defaults. Non-default legacy values are always retained.")]
        private bool useAuthoredRules;
        private Rigidbody2D body;
        private PlayerMovementController controller;

        [Inject]
        public void Construct(PlayerMovementControllerFactory controllers)
        {
            if (controllers == null)
            {
                throw new System.ArgumentNullException(nameof(controllers));
            }

            controller = controllers.Create(moveSpeed, useAuthoredRules);
        }
        private void Awake() => body = GetComponent<Rigidbody2D>();
        private void Update() => controller.SampleInput();

        private void FixedUpdate()
        {
            body.linearVelocity = controller.Velocity;
        }
        private void OnDisable()
        {
            controller?.Stop();
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
            }
        }
    }
}
