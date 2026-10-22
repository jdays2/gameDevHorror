using System;
using EmergencyShift.Features.Interaction.Config;
using UnityEngine;

namespace EmergencyShift.Features.Interaction.Logic
{
    public sealed class InteractionSelectionModel
    {
        private readonly PlayerInteractionSettings settings;
        private float closest = float.PositiveInfinity;
        private float bestAngle = float.PositiveInfinity;

        public InteractionSelectionModel(PlayerInteractionSettings settings) => this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        public int CurrentIndex { get; private set; } = -1;
        public float Reach => settings.Reach;
        public float PickupRadius => settings.PickupRadius;

        public bool WithinReach(Vector2 offset, float targetRange)
        {
            if (!Finite(offset) || !float.IsFinite(targetRange) || targetRange < 0)
            {
                return false;
            }

            float effective = Mathf.Min(Reach, targetRange);
            return offset.sqrMagnitude <= effective * effective;
        }

        internal void Reset()
        {
            CurrentIndex = -1;
            closest = float.PositiveInfinity;
            bestAngle = float.PositiveInfinity;
        }

        internal void Consider(InteractionCandidate candidate)
        {
            if (!candidate.Available || candidate.Index < 0 || !Finite(candidate.Offset) ||
                (candidate.HasFacing && !Finite(candidate.Facing)))
            {
                return;
            }

            float distance = candidate.Offset.magnitude;
            float angle = candidate.HasFacing ? Vector2.Angle(candidate.Facing, candidate.Offset) : 0;
            if (!float.IsFinite(distance) || !float.IsFinite(angle))
            {
                return;
            }

            if (distance > closest + settings.TieDistance ||
                (Mathf.Abs(distance - closest) <= settings.TieDistance && angle >= bestAngle))
            {
                return;
            }

            closest = distance;
            bestAngle = angle;
            CurrentIndex = candidate.Index;
        }

        private static bool Finite(Vector2 value) => float.IsFinite(value.x) && float.IsFinite(value.y);
    }
}
