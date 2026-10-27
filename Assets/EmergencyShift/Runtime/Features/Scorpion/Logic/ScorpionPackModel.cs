namespace EmergencyShift.Features.Scorpion.Logic
{
    /// <summary>One alarm per encounter; separate room packs never share activation state.</summary>
    public sealed class ScorpionPackModel
    {
        public bool IsAlerted { get; private set; }
        internal void Alert() => IsAlerted = true;
    }
}
