using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Caps expensive path rebuilds across actors, preserving cached steering between requests.</summary>
    public static class NavigationWorkBudget2D
    {
        public const int MaximumBuildsPerFrame = 2;
        private static int frame = -1;
        private static int count;
        public static int TotalBuilds
        {
            get; private set;
        }
        public static int PeakBuilds
        {
            get; private set;
        }

        public static bool TryReserve()
        {
            if (frame != Time.frameCount)
            {
                frame = Time.frameCount;
                count = 0;
            }

            if (count >= MaximumBuildsPerFrame)
            {
                return false;
            }

            count++;
            TotalBuilds++;
            PeakBuilds = Mathf.Max(PeakBuilds, count);
            return true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            frame = -1;
            count = TotalBuilds = PeakBuilds = 0;
        }
    }
}
