using System;
using EmergencyShift.Features.Projectiles.Config;
using UnityEngine;

namespace EmergencyShift.Features.Projectiles.Logic
{
    public sealed class ProjectileFlightModel
    {
        public ProjectileFlightModel(ProjectileFlightSettings settings)
        {
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public ProjectileFlightSettings Settings { get; }
        public Vector2 Position { get; private set; }
        public Vector2 Direction { get; private set; }
        public float Distance { get; private set; }
        public float Age { get; private set; }
        public bool HasLaunched { get; private set; }
        public bool IsActive { get; private set; }

        internal void Launch(Vector2 origin, Vector2 heading)
        {
            Position = origin;
            Direction = heading.normalized;
            HasLaunched = true;
            IsActive = true;
        }

        internal bool BeginStep(float deltaSeconds)
        {
            Age += deltaSeconds;
            if (Age > Settings.MaximumAge || Distance >= Settings.Range)
            {
                Stop();
            }

            return IsActive;
        }

        internal float StepDistance(float deltaSeconds) => (float)Math.Min((double)Settings.Speed * deltaSeconds, Settings.Range - Distance);

        internal void Advance(float step)
        {
            Position += Direction * step;
            Distance += step;
        }

        internal void Stop() => IsActive = false;
        internal static bool Finite(Vector2 value) => float.IsFinite(value.x) && float.IsFinite(value.y);
    }
}
