namespace EmergencyShift.Features.Doors.Logic
{
    public interface IDoorMotionView
    {
        void ApplyOpenAmount(float amount);
        void MotionStarted(bool open);
        void MotionStopped(bool open);
    }
}
