namespace EmergencyShift.Features.LampPower.Logic
{
    public interface ILampPowerView
    {
        void ApplyPower(bool powered);
        void ApplyFlicker(float clock);
    }
}
