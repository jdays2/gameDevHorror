namespace EmergencyShift.Core.Time
{
    public sealed class UnityFrameClock : IFrameClock
    {
        public int Frame => UnityEngine.Time.frameCount;
    }
}
