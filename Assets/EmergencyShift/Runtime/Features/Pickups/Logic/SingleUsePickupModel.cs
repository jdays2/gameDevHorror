namespace EmergencyShift.Features.Pickups.Logic
{
    public sealed class SingleUsePickupModel
    {
        public bool Collected { get; private set; }
        public bool Collecting { get; private set; }
        internal void Begin() => Collecting = true;
        internal void Commit() => Collected = true;
        internal void End() => Collecting = false;
        internal void Restore(bool consumed) => Collected = consumed;
    }
}
