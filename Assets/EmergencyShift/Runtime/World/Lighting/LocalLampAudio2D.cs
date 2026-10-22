using UnityEngine;
using EmergencyShift.Features.LampPower.Config;
using Zenject;
using UnityEngine.Rendering.Universal;

namespace EmergencyShift
{
    /// <summary>Follows actual 2D light output. No second flicker timer or enemy-light behaviour.</summary>
    [DisallowMultipleComponent]
    public sealed class LocalLampAudio2D : MonoBehaviour
    {
        [SerializeField]
        private LocalLampAudioSet sounds;
        [SerializeField]
        private Light2D sourceLight;
        [SerializeField]
        private AudioSource humSource;
        [SerializeField]
        private AudioSource contactSource;
        [SerializeField]
        private Transform listener;
        [SerializeField]
        private LayerMask soundBlockingLayers = 1 << 8;
        private System.Random random;
        private bool ready;
        private bool initialized;
        private bool wasLit;
        private int lastContact = -1;
        private float lastContactTime = float.NegativeInfinity;
        private LocalLampSceneSettings sceneSettings;
        private LampFlickerSettings flicker = LampFlickerSettings.LegacyDefaults;

        [Inject]
        private void ConstructCurve(LampFlickerSettings settings) => flicker = settings;
        public int ContactsPlayed { get; private set; }
        private float HearingDistance => sceneSettings != null ? sceneSettings.soundRadius : sounds.HearingDistance;
        private float HumVolume => sceneSettings != null ? sceneSettings.humVolume : sounds.HumVolume;
        private float ContactVolume => sceneSettings != null ? sceneSettings.contactVolume : sounds.ContactVolume;

        public void ConfigureScene(LocalLampSceneSettings settings)
        {
            settings.Validate();
            sceneSettings = settings;
        }

        public void Configure(LocalLampAudioSet set, Light2D light, AudioSource hum, AudioSource contact)
        {
            sounds = set;
            sourceLight = light;
            humSource = hum;
            contactSource = contact;
        }

        public void ConfigureListener(Transform target)
        {
            listener = target;
        }

        [Inject]
        private void Construct(PlayerLife2D scenePlayer)
        {
            if (listener == null && scenePlayer != null)
            {
                listener = scenePlayer.transform;
            }
        }

        private void Awake()
        {


            if (sounds == null || sourceLight == null || humSource == null || contactSource == null ||
                humSource == contactSource || listener == null)
            {
                Debug.LogError("LocalLampAudio2D needs its sound set, light, two distinct voices and listener.", this);
                enabled = false;
                return;
            }

            sounds.Validate();
            random = new System.Random(System.Guid.NewGuid().GetHashCode());
            humSource.clip = sounds.Hum;
            humSource.loop = true;
            humSource.playOnAwake = false;
            contactSource.loop = false;
            contactSource.playOnAwake = false;
            ready = true;
        }

        private void OnEnable()
        {
            initialized = false;
            lastContactTime = float.NegativeInfinity;
            Silence();
        }

        private void OnDisable()
        {
            Silence();
        }

        private void Silence()
        {
            if (humSource != null)
            {
                humSource.volume = 0;
                humSource.Stop();
            }

            if (contactSource != null)
            {
                contactSource.volume = 0;
                contactSource.Stop();
            }
        }

        private void LateUpdate()
        {
            if (!ready || listener == null || sourceLight == null)
            {
                Silence();
                return;
            }

            float intensity = sourceLight.isActiveAndEnabled ? sourceLight.intensity : 0;
            bool lit = intensity > sounds.DarkThreshold;
            Vector2 delta = (Vector2)transform.position - (Vector2)listener.position;
            float gain = sceneSettings != null ? sceneSettings.SoundGain(delta.magnitude, flicker) : sounds.DistanceGain(delta.magnitude);
            if (gain > 0 && Physics2D.Linecast(transform.position, listener.position, soundBlockingLayers).collider != null)
            {
                gain = 0;
            }

            contactSource.volume = ContactVolume * gain;
            float pan = Mathf.Clamp(delta.x / Mathf.Max(HearingDistance, .001f), -sounds.MaximumPan, sounds.MaximumPan);
            humSource.panStereo = pan;
            contactSource.panStereo = pan;
            UpdateHum(intensity, gain);
            if (initialized && lit != wasLit && gain > 0 && Time.time - lastContactTime >= sounds.ContactCooldown)
            {
                PlayContact();
            }

            // Track transitions even while out of hearing range. Approaching a
            // steady lamp must not replay a contact that happened elsewhere.
            wasLit = lit;
            initialized = true;
        }

        private void UpdateHum(float intensity, float gain)
        {
            if (gain <= 0 || intensity <= sounds.DarkThreshold || HumVolume <= 0)
            {
                humSource.volume = 0;
                if (humSource.isPlaying)
                {
                    humSource.Stop();
                }

                return;
            }

            if (!humSource.isPlaying)
            {
                humSource.volume = 0;
                humSource.time = (float)random.NextDouble() * sounds.Hum.length;
                humSource.Play();
            }

            float referenceIntensity = sceneSettings != null ? Mathf.Max(sceneSettings.intensity, .01f) : sounds.ReferenceIntensity;
            float brightness = Mathf.Clamp01(intensity / referenceIntensity);
            float target = HumVolume * brightness * gain;
            humSource.volume = Mathf.MoveTowards(humSource.volume, target,
                Time.deltaTime * HumVolume / sounds.HumFadeSeconds);
        }

        private void PlayContact()
        {
            int index = random.Next(sounds.ContactCount);
            if (sounds.ContactCount > 1 && index == lastContact)
            {
                index = (index + 1) % sounds.ContactCount;
            }

            lastContact = index;
            lastContactTime = Time.time;
            contactSource.clip = sounds.ContactAt(index);
            contactSource.pitch = 1;
            contactSource.Play();
            ContactsPlayed++;
        }
    }
}
