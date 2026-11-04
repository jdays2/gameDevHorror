using UnityEngine;

namespace EmergencyShift.Features.DeathScreen.Config
{
    [CreateAssetMenu(menuName = "Emergency Shift/UI/Death screen transition")]
    public sealed class DeathScreenProfile : ScriptableObject
    {
        [SerializeField, Min(0)] private float worldFadeSeconds = .32f;
        [SerializeField, Min(0)] private float contentDelaySeconds = .32f;
        [SerializeField, Min(0)] private float contentFadeSeconds = 0;
        [SerializeField, Min(0)] private float exitFadeSeconds = .35f;

        [SerializeField, Range(.01f, 1)] private float irisFeather = .18f;
        [SerializeField, Min(0)] private float worldPauseDelaySeconds = 1.4f;

        public DeathScreenSettings ToSettings() => new DeathScreenSettings(
            worldFadeSeconds, contentDelaySeconds, contentFadeSeconds, exitFadeSeconds, irisFeather, worldPauseDelaySeconds);

        public void Validate() => ToSettings();
    }
}
