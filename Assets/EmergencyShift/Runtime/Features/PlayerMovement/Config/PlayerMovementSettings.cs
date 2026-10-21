using System;

namespace EmergencyShift.Features.PlayerMovement.Config
{
    /// <summary>Immutable copy of the existing authored per-instance speed during the config migration.</summary>
    public sealed class PlayerMovementSettings
    {
        public PlayerMovementSettings(float speed)
        {
            if (!float.IsFinite(speed) || speed < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(speed), "Movement speed must be finite and nonnegative.");
            }

            Speed = speed;
        }

        public float Speed { get; }
    }
}
