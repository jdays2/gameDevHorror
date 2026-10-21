using System;
using EmergencyShift.Features.Projectiles.Config;
using EmergencyShift.Features.Projectiles.Logic;
using UnityEngine;

namespace EmergencyShift.Features.Projectiles
{
    public sealed class ProjectileFlightControllerFactory
    {
        public ProjectileFlightController Create(ProjectileFlightSettings settings, Vector2 origin, Vector2 heading,
            IProjectileFlightEnvironment environment)
        {
            var model = new ProjectileFlightModel(settings);
            var result = new LaunchProjectileCommand(model, origin, heading).Execute();
            if (!result.Success)
            {
                throw new ArgumentException(result.Reason);
            }

            return new ProjectileFlightController(model, environment);
        }
    }
}
