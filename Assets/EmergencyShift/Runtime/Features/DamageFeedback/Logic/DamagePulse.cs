using UnityEngine;

namespace EmergencyShift.Features.DamageFeedback.Logic
{
    public readonly struct DamagePulse
    {
        public Vector2 EdgePoint { get; }
        public float Opacity { get; }
        public float Seed { get; }

        public DamagePulse(Vector2 edgePoint, float opacity, float seed)
        {
            EdgePoint = edgePoint;
            Opacity = opacity;
            Seed = seed;
        }
    }
}
