namespace EmergencyShift.Features.Doors.Logic
{
    /// <summary>Only an advancing closing timeline authorizes lethal contacts. Restoration is never motion.</summary>
    public sealed class DoorCrushModel
    {
        public bool CanCrush(DoorLeafStep step)
        {
            return step.IsMotionTick && UnitAmount(step.PreviousAmount) && UnitAmount(step.NextAmount) &&
                step.NextAmount < step.PreviousAmount &&
                Finite(step.Displacement.x) && Finite(step.Displacement.y) &&
                step.Displacement.sqrMagnitude > 0;
        }

        private static bool UnitAmount(float value) => Finite(value) && value >= 0 && value <= 1;
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
