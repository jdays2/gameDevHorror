using System;

namespace EmergencyShift.Features.Doors.Config
{
    public sealed class DoorMotionSettings
    {
        public float Duration { get; }

        public DoorMotionSettings(float duration)
        {
            if (!float.IsFinite(duration) || duration <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(duration));
            }

            Duration = duration;
        }
    }
}
