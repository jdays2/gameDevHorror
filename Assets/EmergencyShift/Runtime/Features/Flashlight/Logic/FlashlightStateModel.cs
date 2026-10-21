using System;
using EmergencyShift.Core.Commands;
using UnityEngine;

namespace EmergencyShift.Features.Flashlight.Logic
{
    /// <summary>One player's ownership, power and optics, independent of Light2D or transforms.</summary>
    public sealed class FlashlightStateModel
    {
        private readonly FlashlightOwnership ownership;

        public FlashlightStateModel(bool initiallyOwned, bool initiallyOn, Vector2 optics)
        {
            if (!CanSetOptics(optics).Success)
            {
                throw new ArgumentException("Finite flashlight optics required.", nameof(optics));
            }

            ownership = new FlashlightOwnership(initiallyOwned);
            IsOn = initiallyOn && initiallyOwned;
            SetOptics(optics);
        }

        public bool IsOwned => ownership.IsOwned;
        public bool IsOn { get; private set; }
        public float Range { get; private set; }
        public float BeamAngle { get; private set; }

        public static CommandResult CanSetOptics(Vector2 optics) => float.IsFinite(optics.x) && float.IsFinite(optics.y)
            ? CommandResult.Ok() : CommandResult.Fail("Flashlight optics must be finite.");

        /// <summary>Existing defensive inner beam, before the separate Unity obstruction query.</summary>
        public bool Covers(Vector2 origin, Vector2 direction, Vector2 point)
        {
            if (!IsOn || !Finite(origin) || !Finite(direction) || !Finite(point))
            {
                return false;
            }

            Vector2 offset = point - origin;
            if (!float.IsFinite(offset.sqrMagnitude) || offset.sqrMagnitude > Range * Range * .81f)
            {
                return false;
            }

            return offset.sqrMagnitude <= .0001f || Vector2.Angle(direction, offset) <= BeamAngle * .45f;
        }

        internal void Acquire() => ownership.Acquire();

        internal void RestoreOwnership(bool owned)
        {
            ownership.Restore(owned);
            if (!owned)
            {
                IsOn = false;
            }
        }

        internal void SetPower(bool on) => IsOn = on && IsOwned;

        internal void SetOptics(Vector2 optics)
        {
            Range = Mathf.Max(.5f, optics.x);
            BeamAngle = Mathf.Clamp(optics.y, 20, 140);
        }

        private static bool Finite(Vector2 value) => float.IsFinite(value.x) && float.IsFinite(value.y);
    }
}
