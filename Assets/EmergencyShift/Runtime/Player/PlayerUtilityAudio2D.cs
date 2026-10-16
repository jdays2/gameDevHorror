using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Single saved channel for flashlight switch and empty trigger; no synthetic pickup beeps.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(Flashlight2D), typeof(PlayerWeapon2D))]
    public sealed class PlayerUtilityAudio2D : MonoBehaviour
    {
        [SerializeField]
        private PlayerWorldAudioSet sounds;
        [SerializeField]
        private AudioSource source;
        private Flashlight2D flashlight;
        private PlayerWeapon2D weapon;
        public int SwitchesPlayed
        {
            get; private set;
        }
        public int EmptyTriggersPlayed
        {
            get; private set;
        }

        private void Awake()
        {
            flashlight = GetComponent<Flashlight2D>();
            weapon = GetComponent<PlayerWeapon2D>();
            if (source != null)
            {
                source.playOnAwake = false;
                source.loop = false;
                source.spatialBlend = 0;
                source.priority = 80;
                source.dopplerLevel = 0;
                source.Stop();
            }
        }

        private void OnEnable()
        {
            flashlight.PowerChanged += OnSwitch;
            weapon.EmptyTriggerPulled += OnEmpty;
        }

        private void OnDisable()
        {
            flashlight.PowerChanged -= OnSwitch;
            weapon.EmptyTriggerPulled -= OnEmpty;
            if (source != null)
            {
                source.Stop();
            }
        }

        private void OnSwitch(bool on)
        {
            var life = GetComponent<PlayerLife2D>();
            if (Time.timeSinceLevelLoad < .2f || (life != null && life.IsDead))
            {
                return;
            }

            if (sounds != null && Play(sounds.flashlightSwitch, sounds.switchVolume))
            {
                SwitchesPlayed++;
            }
        }

        private void OnEmpty()
        {
            var profile = weapon.Definition.audioProfile;
            AudioClip clip = sounds != null ? sounds.emptyTrigger : null;
            if (profile != null)
            {
                clip = profile.emptyTrigger;
            }
            float gain = sounds != null ? sounds.emptyVolume : .45f;
            if (Play(clip, gain))
            {
                EmptyTriggersPlayed++;
            }
        }

        private bool Play(AudioClip clip, float gain)
        {
            if (source == null || clip == null)
            {
                return false;
            }

            source.Stop();
            source.clip = clip;
            source.volume = gain;
            source.pitch = 1;
            source.Play();
            return true;
        }
    }
}
