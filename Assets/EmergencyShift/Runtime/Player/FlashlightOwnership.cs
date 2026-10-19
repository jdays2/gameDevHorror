namespace EmergencyShift
{
    /// <summary>Per-player ownership, independent of Unity input, lighting and presentation.</summary>
    public sealed class FlashlightOwnership
    {
        public bool IsOwned { get; private set; }

        public FlashlightOwnership(bool initiallyOwned)
        {
            IsOwned = initiallyOwned;
        }

        internal void Acquire()
        {
            IsOwned = true;
        }

        public void Restore(bool owned)
        {
            IsOwned = owned;
        }
    }
}
