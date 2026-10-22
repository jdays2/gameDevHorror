using UnityEngine;

namespace EmergencyShift.Features.LampPower.Config
{
    [CreateAssetMenu(menuName = "Emergency Shift/Fixture flicker profile")]
    public sealed class LampFlickerProfile : ScriptableObject
    {
        [SerializeField] private float cycleSeconds = 6.4f;
        [SerializeField] private float minimumFactor = .82f;
        [SerializeField] private float noiseFrequency = 1.4f;
        [SerializeField] private Vector2 firstDarkInterval = new Vector2(4.9f, 5.02f);
        [SerializeField] private Vector2 secondDarkInterval = new Vector2(5.16f, 5.25f);
        [SerializeField] private float fullVolumeFraction = .175f;
        public LampFlickerSettings ToSettings() => new LampFlickerSettings(cycleSeconds, minimumFactor,
            noiseFrequency, firstDarkInterval, secondDarkInterval, fullVolumeFraction);
    }
}
