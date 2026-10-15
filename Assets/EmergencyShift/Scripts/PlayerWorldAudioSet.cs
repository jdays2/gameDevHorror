using UnityEngine;

namespace EmergencyShift
{
    [CreateAssetMenu(menuName = "Emergency Shift/Player world audio set")]
    public sealed class PlayerWorldAudioSet : ScriptableObject
    {
        public AudioClip[] concreteSteps, woodSteps, tileSteps;
        public AudioClip flashlightSwitch, emptyTrigger;
        [Range(0, 1)] public float stepVolume = .32f;
        [Range(0, 1)] public float switchVolume = .35f;
        [Range(0, 1)] public float emptyVolume = .45f;
        public AudioClip[] Steps(GroundSoundSurface material)
        {
            var selected = material == GroundSoundSurface.Wood ? woodSteps
                : material == GroundSoundSurface.Tile ? tileSteps : concreteSteps;
            return selected != null && selected.Length > 0 ? selected : concreteSteps;
        }
    }
}
