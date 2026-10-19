namespace EmergencyShift.Core.Randomness
{
    public interface IRandomSource
    {
        int NextInt(int minimumInclusive, int maximumExclusive);
        double NextUnit();
    }
}
