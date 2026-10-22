using System;
using EmergencyShift.Core.Time;
using UnityEngine;

namespace EmergencyShift.Features.LampPower.Logic
{
    public sealed class LampPowerModel
    {
        private const float PositionXPhase = .37f;
        private const float PositionYPhase = .19f;
        private readonly IGameClock clock;
        public bool Powered { get; private set; }

        public LampPowerModel(IGameClock clock, bool powered)
        {
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            Powered = powered;
        }

        internal void SetPower(bool on) => Powered = on;

        public float SampleClock(float phase, Vector2 position)
        {
            if (!float.IsFinite(phase) || !float.IsFinite(position.x) || !float.IsFinite(position.y))
            {
                throw new ArgumentOutOfRangeException(nameof(phase), "Lamp phase and position must be finite.");
            }

            double now = clock.Time;
            float sample = (float)now + phase + position.x * PositionXPhase + position.y * PositionYPhase;
            if (!double.IsFinite(now) || !float.IsFinite(sample))
            {
                throw new InvalidOperationException("The lamp requires finite scaled time and phase arithmetic.");
            }

            return sample;
        }
    }
}
