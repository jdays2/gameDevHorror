using EmergencyShift.Core.Commands;
using UnityEngine;

namespace EmergencyShift.Features.Projectiles.Logic
{
    public readonly struct LaunchProjectileCommand : ICommand
    {
        private readonly ProjectileFlightModel model;
        private readonly Vector2 origin;
        private readonly Vector2 heading;

        public LaunchProjectileCommand(ProjectileFlightModel model, Vector2 origin, Vector2 heading)
        {
            this.model = model;
            this.origin = origin;
            this.heading = heading;
        }

        public CommandResult CanExecute() => model != null && !model.HasLaunched &&
            ProjectileFlightModel.Finite(origin) && ProjectileFlightModel.Finite(heading)
            ? CommandResult.Ok() : CommandResult.Fail("Projectile requires an unused model and finite launch geometry.");

        public CommandResult Execute()
        {
            CommandResult result = CanExecute();
            if (result.Success)
            {
                model.Launch(origin, heading);
            }

            return result;
        }
    }
}
