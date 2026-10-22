using System;
using System.Collections.Generic;
using EmergencyShift.Core.Commands;
using EmergencyShift.Features.DamageFeedback.Config;
using UnityEngine;

namespace EmergencyShift.Features.DamageFeedback.Logic
{
    public sealed class DirectionalDamageModel
    {
        public const int Capacity = 8;
        private readonly DamageFeedbackSettings settings;
        private readonly DamagePulse[] pulses = new DamagePulse[Capacity];
        private readonly float[] ages = new float[Capacity];
        private int nextSlot;
        private int sequence;
        private Vector2 impactDirection;
        private float impactAge;

        public IReadOnlyList<DamagePulse> Pulses { get; }
        public int ActiveCount { get; private set; }
        public Vector2 ImpactOffset { get; private set; }
        public bool ImpactActive { get; private set; }

        public DirectionalDamageModel(DamageFeedbackSettings settings)
        {
            // Reject the default(struct) value as well as malformed authored data.
            this.settings = new DamageFeedbackSettings(settings.HoldSeconds, settings.FadeSeconds, settings.Opacity,
                settings.CameraDistance, settings.CameraSeconds);
            Pulses = Array.AsReadOnly(pulses);
        }

        public CommandResult CanShow(int amount, Vector2 towardSource)
        {
            if (amount <= 0)
            {
                return CommandResult.Fail("Only accepted positive damage has a feedback pulse.");
            }

            if (float.IsNaN(towardSource.x) || float.IsInfinity(towardSource.x) ||
                float.IsNaN(towardSource.y) || float.IsInfinity(towardSource.y) || towardSource == Vector2.zero)
            {
                return CommandResult.Fail("A finite known source direction is required.");
            }

            return CommandResult.Ok();
        }

        internal void Show(Vector2 towardSource)
        {
            float largest = Mathf.Max(Mathf.Abs(towardSource.x), Mathf.Abs(towardSource.y));
            Vector2 edge = Vector2.one * .5f + towardSource / largest * .5f;
            if (pulses[nextSlot].Opacity <= 0)
            {
                ActiveCount++;
            }

            sequence = (sequence + 1) % 1024;
            float seed = Mathf.Repeat(sequence * .6180339f, 1);
            pulses[nextSlot] = new DamagePulse(edge, settings.Opacity, seed);
            ages[nextSlot] = 0;
            nextSlot = (nextSlot + 1) % Capacity;
            // Replace, rather than accumulate, camera displacement on consecutive hits.
            impactDirection = -(towardSource / largest).normalized;
            impactAge = 0;
            ImpactOffset = Vector2.zero;
            ImpactActive = settings.CameraDistance > 0;
        }

        public void Advance(float seconds)
        {
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(seconds));
            }

            if (ImpactActive)
            {
                impactAge += seconds;
                float progress = Mathf.Clamp01(impactAge / settings.CameraSeconds);
                const float PeakTime = .12f;
                float envelope = progress / PeakTime;
                if (progress >= PeakTime)
                {
                    float remaining = (1 - progress) / (1 - PeakTime);
                    envelope = remaining * remaining;
                }

                ImpactOffset = impactDirection * (settings.CameraDistance * envelope);
                ImpactActive = progress < 1;
            }

            for (int i = 0; i < Capacity; i++)
            {
                DamagePulse pulse = pulses[i];
                if (pulse.Opacity <= 0)
                {
                    continue;
                }

                ages[i] += seconds;
                float progress = Mathf.Clamp01((ages[i] - settings.HoldSeconds) / settings.FadeSeconds);
                float opacity = settings.Opacity * (1 - progress * progress * (3 - 2 * progress));
                pulses[i] = new DamagePulse(pulse.EdgePoint, opacity, pulse.Seed);
                if (opacity <= 0)
                {
                    ActiveCount--;
                }
            }
        }

        public void Clear()
        {
            Array.Clear(pulses, 0, Capacity);
            Array.Clear(ages, 0, Capacity);
            ActiveCount = 0;
            nextSlot = 0;
            ImpactOffset = Vector2.zero;
            ImpactActive = false;
        }
    }
}
