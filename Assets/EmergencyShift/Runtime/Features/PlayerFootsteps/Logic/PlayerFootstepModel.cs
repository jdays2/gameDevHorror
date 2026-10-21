using System;
using EmergencyShift.Core.Time;
using EmergencyShift.Features.PlayerFootsteps.Config;
using UnityEngine;

namespace EmergencyShift.Features.PlayerFootsteps.Logic
{
    /// <summary>Personal travelled stride and scaled-clock cadence; no sound, device or Rigidbody dependency.</summary>
    public sealed class PlayerFootstepModel
    {
        // Sampling tolerances and discontinuity guard preserve the existing physics/render bridge.
        private const float TeleportDistance = 2;
        private const float MinimumPositionDelta = .00001f;
        private const float StationaryVelocitySquared = .001f;
        private readonly PlayerFootstepSettings settings;
        private readonly IGameClock clock;
        private Vector2 previous;
        private float travelled;
        private double nextStep;
        private bool hasPrevious;

        public PlayerFootstepModel(PlayerFootstepSettings settings, IGameClock clock)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public bool Tick(Vector2 position, float velocitySquared, bool canTravel)
        {
            if (!ValidPosition(position) || !float.IsFinite(velocitySquared) || velocitySquared < 0)
            {
                travelled = 0;
                hasPrevious = false;
                return false;
            }

            if (!hasPrevious)
            {
                previous = position;
                hasPrevious = true;
                return false;
            }

            float distance = Vector2.Distance(position, previous);
            previous = position;
            if (!canTravel || distance > TeleportDistance)
            {
                travelled = 0;
                return false;
            }

            if (distance < MinimumPositionDelta)
            {
                if (velocitySquared < StationaryVelocitySquared)
                {
                    travelled = 0;
                }

                return false;
            }

            travelled += distance;
            if (travelled < settings.DistancePerStep)
            {
                return false;
            }

            double now = clock.Time;
            if (!double.IsFinite(now) || now < 0)
            {
                travelled = 0;
                return false;
            }

            if (now < nextStep)
            {
                return false;
            }

            travelled %= settings.DistancePerStep;
            nextStep = now + settings.MinimumInterval;
            return true;
        }

        internal void Reset(Vector2 position)
        {
            previous = position;
            travelled = 0;
            nextStep = 0;
            hasPrevious = true;
        }

        internal static bool ValidPosition(Vector2 position) => float.IsFinite(position.x)
            && float.IsFinite(position.y) && float.IsFinite(position.sqrMagnitude);
    }
}
