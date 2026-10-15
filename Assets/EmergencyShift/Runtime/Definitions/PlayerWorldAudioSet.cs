using UnityEngine;

namespace EmergencyShift
{
    [CreateAssetMenu(menuName = "Emergency Shift/Player world audio set")]
    public sealed class PlayerWorldAudioSet : ScriptableObject
    {
        public AudioClip[] concreteSteps;
        public AudioClip[] woodSteps;
        public AudioClip[] tileSteps;
        public AudioClip flashlightSwitch;
        public AudioClip emptyTrigger;
        [Range(0, 1)]
        public float stepVolume = .32f;
        [Range(0, 1)]
        public float switchVolume = .35f;
        [Range(0, 1)]
        public float emptyVolume = .45f;
        public AudioClip[] Steps(GroundSoundSurface material)
        {
            AudioClip[] selected;
            switch (material)
            {
                case GroundSoundSurface.Wood:
                    selected = woodSteps;
                    break;
                case GroundSoundSurface.Tile:
                    selected = tileSteps;
                    break;
                default:
                    selected = concreteSteps;
                    break;
            }

            return selected != null && selected.Length > 0 ? selected : concreteSteps;
        }
    }
}
