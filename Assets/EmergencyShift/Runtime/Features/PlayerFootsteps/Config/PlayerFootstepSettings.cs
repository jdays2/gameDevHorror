using System;

namespace EmergencyShift.Features.PlayerFootsteps.Config
{
    public sealed class PlayerFootstepSettings
    {
        public float DistancePerStep { get; }
        public float MinimumInterval { get; }

        public PlayerFootstepSettings(float distancePerStep, float minimumInterval)
        {
            if (!float.IsFinite(distancePerStep) || distancePerStep <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(distancePerStep));
            }

            if (!float.IsFinite(minimumInterval) || minimumInterval < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(minimumInterval));
            }

            DistancePerStep = distancePerStep;
            MinimumInterval = minimumInterval;
        }
    }
}
