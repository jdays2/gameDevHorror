using UnityEngine;

namespace EmergencyShift
{
    [CreateAssetMenu(menuName = "Emergency Shift/Enemies/Sleeper behaviour")]
    public sealed class SleeperBehaviourProfile : ScriptableObject
    {
        [Min(.05f)] public float lightExposureSeconds = .65f;
        [Min(.01f), Tooltip("Exposure seconds lost per second outside the directed beam.")]
        public float exposureDecay = .75f;
        [Min(.05f)] public float wakingSeconds = .9f;
        [Min(0)] public float recoverySeconds = .65f;

        public bool IsValid => lightExposureSeconds > 0 && exposureDecay > 0 && wakingSeconds > 0 && recoverySeconds >= 0;
    }
}
