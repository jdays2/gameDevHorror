using UnityEngine;

namespace EmergencyShift
{
    public interface IGroundAudioRegion
    {
        GroundSoundSurface Surface { get; }
        int Priority { get; }
        bool Contains(Vector2 point);
    }
}
