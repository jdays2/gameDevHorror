using System;
using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.Projectiles.Logic
{
    public sealed class ProjectileFlightController
    {
        private readonly IProjectileFlightEnvironment environment;

        public ProjectileFlightController(ProjectileFlightModel model, IProjectileFlightEnvironment environment)
        {
            Model = model ?? throw new ArgumentNullException(nameof(model));
            this.environment = environment ?? throw new ArgumentNullException(nameof(environment));
        }

        public ProjectileFlightModel Model { get; }

        public CommandResult Step(float deltaSeconds)
        {
            if (!float.IsFinite(deltaSeconds) || deltaSeconds < 0)
            {
                return CommandResult.Fail("Projectile step must be finite and nonnegative.");
            }

            if (!Model.IsActive)
            {
                return CommandResult.Ok();
            }

            if (!Model.BeginStep(deltaSeconds))
            {
                environment.Release();
                return CommandResult.Ok();
            }

            float step = Model.StepDistance(deltaSeconds);
            if (environment.Sweep(Model.Position, Model.Direction, Model.Settings.Radius, step))
            {
                // Complete before callbacks: a reentrant impact cannot deliver damage twice.
                Model.Stop();
                try
                {
                    environment.Impact(Model.Settings.Damage, Model.Direction);
                }
                finally
                {
                    environment.Release();
                }
            }
            else
            {
                Model.Advance(step);
                environment.Move(Model.Position);
            }

            return CommandResult.Ok();
        }
    }
}
