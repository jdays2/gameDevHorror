using System;
using UnityEngine;

namespace EmergencyShift.Features.Flashlight.Logic
{
    public sealed class FlashlightAimController
    {
        private readonly FlashlightAimModel model;
        private readonly IFlashlightAimEnvironment environment;

        public FlashlightAimController(FlashlightAimModel model, IFlashlightAimEnvironment environment)
        {
            this.model = model ?? throw new ArgumentNullException(nameof(model));
            this.environment = environment ?? throw new ArgumentNullException(nameof(environment));
        }

        public void Smooth(Vector2 worldPoint, float deltaTime)
        {
            if (environment.IsAvailable && model.Step(environment.Angle, worldPoint - environment.OwnerPosition, deltaTime, out float angle))
            {
                environment.SetAngle(angle);
            }
        }

        public void Snap(Vector2 worldPoint)
        {
            if (environment.IsAvailable && model.Snap(environment.Angle, worldPoint - environment.OwnerPosition, out float angle))
            {
                environment.SetAngle(angle);
            }
        }
    }
}
