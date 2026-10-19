namespace EmergencyShift.Core.Time
{
    public sealed class UnityWorldTimeScale : IWorldTimeScale
    {
        public float Scale
        {
            get => UnityEngine.Time.timeScale;
            set => UnityEngine.Time.timeScale = value;
        }
    }
}
