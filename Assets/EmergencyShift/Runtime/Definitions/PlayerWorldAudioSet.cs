using System;
using UnityEngine;

namespace EmergencyShift
{
    [CreateAssetMenu(menuName = "Emergency Shift/Player world audio set")]
    public sealed class PlayerWorldAudioSet : ScriptableObject
    {
        [SerializeField]
        private AudioClip[] concreteSteps;
        [SerializeField]
        private AudioClip[] woodSteps;
        [SerializeField]
        private AudioClip[] tileSteps;
        [SerializeField]
        private AudioClip flashlightSwitch;
        [SerializeField]
        private AudioClip emptyTrigger;
        [Range(0, 1)]
        [SerializeField]
        private float stepVolume = .32f;
        [Range(0, 1)]
        [SerializeField]
        private float switchVolume = .35f;
        [Range(0, 1)]
        [SerializeField]
        private float emptyVolume = .45f;
        public AudioClipBank ConcreteSteps => concreteSteps;
        public AudioClipBank WoodSteps => woodSteps;
        public AudioClipBank TileSteps => tileSteps;
        public AudioClip FlashlightSwitch => flashlightSwitch;
        public AudioClip EmptyTrigger => emptyTrigger;
        public float StepVolume => stepVolume;
        public float SwitchVolume => switchVolume;
        public float EmptyVolume => emptyVolume;

        public AudioClipBank Steps(GroundSoundSurface material)
        {
            Validate();
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

        public void Validate()
        {
            if (!AudioProfileValidation.Unit(stepVolume) || !AudioProfileValidation.Unit(switchVolume) ||
                !AudioProfileValidation.Unit(emptyVolume))
            {
                throw new InvalidOperationException("Invalid player world audio set: " + name);
            }
        }
    }
}
