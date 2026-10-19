namespace EmergencyShift.Core.Time
{
    /// <summary>Environment adapter for the project's scaled-world time policy.</summary>
    public sealed class UnityGameClock : IGameClock
    {
        public double Time => UnityEngine.Time.time;
    }
}
