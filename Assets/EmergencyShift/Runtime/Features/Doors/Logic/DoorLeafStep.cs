using UnityEngine;

namespace EmergencyShift.Features.Doors.Logic
{
    /// <summary>One proposed leaf translation, before the native pose is written.</summary>
    public readonly struct DoorLeafStep
    {
        public bool IsMotionTick { get; }
        public float PreviousAmount { get; }
        public float NextAmount { get; }
        public Vector2 Displacement { get; }

        public DoorLeafStep(bool isMotionTick, float previousAmount, float nextAmount, Vector2 displacement)
        {
            IsMotionTick = isMotionTick;
            PreviousAmount = previousAmount;
            NextAmount = nextAmount;
            Displacement = displacement;
        }
    }
}
