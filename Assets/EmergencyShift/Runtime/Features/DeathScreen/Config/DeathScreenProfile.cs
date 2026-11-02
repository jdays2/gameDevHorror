using UnityEngine;

namespace EmergencyShift.Features.DeathScreen.Config
{
    [CreateAssetMenu(menuName = "Emergency Shift/UI/Death screen transition")]
    public sealed class DeathScreenProfile : ScriptableObject
    {
        [SerializeField, Min(0)] private float worldFadeSeconds = 1.4f;
        [SerializeField, Min(0)] private float contentDelaySeconds = .7f;
        [SerializeField, Min(0)] private float contentFadeSeconds = .5f;
        [SerializeField, Min(0)] private float exitFadeSeconds = .35f;

        public DeathScreenSettings ToSettings() => new DeathScreenSettings(
            worldFadeSeconds, contentDelaySeconds, contentFadeSeconds, exitFadeSeconds);

        public void Validate() => ToSettings();
    }
}
