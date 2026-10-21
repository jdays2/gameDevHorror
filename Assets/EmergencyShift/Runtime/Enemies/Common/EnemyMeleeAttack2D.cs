using System;
using EmergencyShift.Features.Enemies;
using EmergencyShift.Features.Enemies.Logic;
using EmergencyShift.Features.Enemies.View;
using UnityEngine;
using Zenject;

namespace EmergencyShift
{
    /// <summary>Compatibility/event adapter; a pure personal model owns the attack timeline.</summary>
    [DisallowMultipleComponent]
    public sealed class EnemyMeleeAttack2D : MonoBehaviour
    {
        private EnemyMeleeControllerFactory factory;
        private EnemyMeleeController controller;

        public event Action Lunged;
        public event Action<bool> Resolved;

        [Serializable]
        public sealed class Snapshot
        {
            public bool struck;
            public bool lunged;
            public float cooldown;
        }

        [Inject]
        public void Construct(EnemyMeleeControllerFactory controllers)
        {
            factory = controllers ?? throw new ArgumentNullException(nameof(controllers));
        }

        private void OnDestroy() => Unbind();

        public void Configure(EnemyDefinition definition, EnemyPerception2D perception, EnemyMotor2D movement)
        {
            if (factory == null || definition == null || perception == null || movement == null)
            {
                throw new InvalidOperationException("Melee requires level composition, definition, perception and motor.");
            }

            EnemyMeleeController replacement = factory.Create(definition.ToMeleeSettings(),
                new EnemyMeleeEnvironment2D(perception, movement));
            if (controller != null)
            {
                var result = replacement.Restore(controller.Capture());
                if (!result.Success)
                {
                    throw new InvalidOperationException(result.Reason);
                }
            }

            Unbind();
            controller = replacement;
            controller.Lunged += ForwardLunge;
            controller.Resolved += ForwardResolution;
        }

        public bool CanStart(Vector2 at) => controller != null && controller.CanStart(at);
        public void Begin() => RequireController().Begin();
        public void Finish() => RequireController().Finish();
        public void Interrupt(float delay) => RequireController().Interrupt(delay);
        public Vector2 Step(float progress, Vector2 at, Vector2 heading, bool fixedHeading = false) =>
            RequireController().Step(progress, at, heading, fixedHeading);

        public Snapshot CaptureState()
        {
            EnemyMeleeState state = controller != null ? controller.Capture() : default;
            return new Snapshot { struck = state.Struck, lunged = state.Lunged, cooldown = state.Cooldown };
        }

        public void RestoreState(Snapshot saved)
        {
            if (saved == null)
            {
                throw new ArgumentNullException(nameof(saved));
            }

            var result = RequireController().Restore(new EnemyMeleeState(saved.struck, saved.lunged, saved.cooldown));
            if (!result.Success)
            {
                throw new InvalidOperationException(result.Reason);
            }
        }

        private void ForwardLunge() => Lunged?.Invoke();
        private void ForwardResolution(bool landed) => Resolved?.Invoke(landed);

        private void Unbind()
        {
            if (controller != null)
            {
                controller.Lunged -= ForwardLunge;
                controller.Resolved -= ForwardResolution;
            }
        }

        private EnemyMeleeController RequireController() => controller ??
            throw new InvalidOperationException("Melee must be configured by its actor after level composition.");
    }
}
