using UnityEngine;
namespace EmergencyShift
{
    /// <summary>Spread simultaneous corpse fragment construction across frames; never remove permanent blood.</summary>
    public static class GoreSpawnBudget2D
    {
        public const int MaximumFragmentsPerFrame=32;
        private static int frame=-1,count;
        public static int PeakFragments { get; private set; }
        public static bool TryReserve()
        {
            if(frame!=Time.frameCount){frame=Time.frameCount;count=0;}
            if(count>=MaximumFragmentsPerFrame)return false;
            count++;PeakFragments=Mathf.Max(PeakFragments,count);return true;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset(){frame=-1;count=PeakFragments=0;}
    }
}
