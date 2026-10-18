using UnityEngine;
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
        public int ContactsPlayed { get; private set; }
        private float HearingDistance => sceneSettings != null ? sceneSettings.soundRadius : sounds.hearingDistance;
        private float HumVolume => sceneSettings != null ? sceneSettings.humVolume : sounds.humVolume;
        private float ContactVolume => sceneSettings != null ? sceneSettings.contactVolume : sounds.contactVolume;

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

        private void Awake()
        {
            var context = LevelContext2D.For(this);
            if (listener == null && context != null && context.Player != null)
            {
                listener = context.Player.transform;
            }

            if (sounds == null || sourceLight == null || humSource == null || contactSource == null ||
                humSource == contactSource || listener == null)
            {
                Debug.LogError("LocalLampAudio2D needs its sound set, light, two distinct voices and listener.", this);
                enabled = false;
                return;
            }

            sounds.Validate();
            random = new System.Random(System.Guid.NewGuid().GetHashCode());
            humSource.clip = sounds.hum;
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
            bool lit = intensity > sounds.darkThreshold;
            Vector2 delta = (Vector2)transform.position - (Vector2)listener.position;
            float gain = sceneSettings != null ? sceneSettings.SoundGain(delta.magnitude) : sounds.DistanceGain(delta.magnitude);
            if (gain > 0 && Physics2D.Linecast(transform.position, listener.position, soundBlockingLayers).collider != null)
            {
                gain = 0;
            }

            contactSource.volume = ContactVolume * gain;
            float pan = Mathf.Clamp(delta.x / Mathf.Max(HearingDistance, .001f), -.5f, .5f);
            humSource.panStereo = pan;
            contactSource.panStereo = pan;
            UpdateHum(intensity, gain);
            if (initialized && lit != wasLit && gain > 0 && Time.time - lastContactTime >= sounds.contactCooldown)
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
            if (gain <= 0 || intensity <= sounds.darkThreshold || HumVolume <= 0)
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
                humSource.time = (float)random.NextDouble() * sounds.hum.length;
                humSource.Play();
            }

            float referenceIntensity = sceneSettings != null ? Mathf.Max(sceneSettings.intensity, .01f) : sounds.referenceIntensity;
            float brightness = Mathf.Clamp01(intensity / referenceIntensity);
            float target = HumVolume * brightness * gain;
            humSource.volume = Mathf.MoveTowards(humSource.volume, target,
                Time.deltaTime * HumVolume / sounds.humFadeSeconds);
        }

        private void PlayContact()
        {
            int index = random.Next(sounds.contacts.Length);
            if (sounds.contacts.Length > 1 && index == lastContact)
            {
                index = (index + 1) % sounds.contacts.Length;
            }

            lastContact = index;
            lastContactTime = Time.time;
            contactSource.clip = sounds.contacts[index];
            contactSource.pitch = 1;
            contactSource.Play();
            ContactsPlayed++;
        }
    }
}
