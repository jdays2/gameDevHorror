using UnityEngine;

namespace EmergencyShift
{
    /// <summary>A defensive light stimulus. Ordinary decorative lights need not implement it.</summary>
    public interface IThreatLight2D
    {
        bool IsOn
        {
            get;
        }

        Vector2 Origin
        {
            get;
        }

        Vector2 Direction
        {
            get;
        }

        float Range
        {
            get;
        }

        float BeamAngle
        {
            get;
        }

        bool Illuminates(Vector2 worldPoint, LayerMask obstacles);
    }
}
