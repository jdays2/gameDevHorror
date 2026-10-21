using UnityEngine;

namespace EmergencyShift.Features.AudioMood.Config
{
    [CreateAssetMenu(menuName = "Emergency Shift/Audio threat signal profile")]
    public sealed class AudioThreatSignalProfile : ScriptableObject
    {
        [SerializeField, Range(0, 1)]
        private float minimumGain = .02f;
        [SerializeField, Min(.01f)]
        private float minimumHeardSeconds = .25f;
        [SerializeField, Min(.01f)]
        private float maximumHeardSeconds = 2;
        [SerializeField, Min(.01f)]
        private float urgentSeconds = .4f;

        public float MinimumGain => minimumGain;
        public float MinimumHeardSeconds => minimumHeardSeconds;
        public float MaximumHeardSeconds => maximumHeardSeconds;
        public float UrgentSeconds => urgentSeconds;

        public AudioThreatSignalSettings ToSettings() =>
            new AudioThreatSignalSettings(minimumGain, minimumHeardSeconds, maximumHeardSeconds, urgentSeconds);
    }
}
