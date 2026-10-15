using System.Collections.Generic;
using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Saved, bounded channels for pickup feedback and optional combat audition.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(PlayerInteraction), typeof(PlayerWeapon2D))]
    public sealed class PlayerAudio2D : MonoBehaviour
    {
        [SerializeField]
        private PlayerAudioSet sounds;
        [SerializeField]
        private AudioSource pickupSource;
        [SerializeField]
        private AudioSource[] shotSources;
        [SerializeField]
        private AudioSource[] casingSources;
        [SerializeField]
        private AudioSource[] impactSources;
        [SerializeField, Range(0, 1)]
        private float pickupVolume = .65f;
        [Header("Selected indoor pistol")]
        [SerializeField]
        private bool combatAudioEnabled;
        [SerializeField]
        private bool indoorShot;
        [SerializeField]
        private bool impactAudioEnabled;
        [SerializeField, Range(0, 1)]
        private float shotVolume = .75f;
        [SerializeField, Range(0, 1)]
        private float impactVolume = .65f;
        [SerializeField, Range(0, 1)]
        private float casingVolume = .3f;
        [SerializeField, Range(0, .03f)]
        private float shotPitchVariation = .008f;
        [SerializeField, Range(0, 1)]
        private float shotGainVariationDb = .4f;
        private PlayerInteraction interaction;
        private PlayerWeapon2D weapon;
        private readonly Dictionary<AudioClip[], int> last = new Dictionary<AudioClip[], int>();
        private readonly int[] pickupCounts = new int[System.Enum.GetValues(typeof(PickupKind)).Length];
        private System.Random random;
        private System.Random casingRandom;
        private float pickupBurstUntil;
        private int pickupPriority;
        private int shotCursor;
        public bool CombatAudioEnabled
        {
            get => combatAudioEnabled; set => combatAudioEnabled = value;
        }

        public int PickupCount(PickupKind kind) => pickupCounts[(int)kind];
        public int ShotAudioCount
        {
            get; private set;
        }
        public int ImpactAudioCount
        {
            get; private set;
        }
        public int ResolvedImpactCount
        {
            get; private set;
        }
        public ImpactMaterial LastImpactMaterial
        {
            get; private set;
        }
        public int CasingAudioCount
        {
            get; private set;
        }
        public string LastShotClip
        {
            get; private set;
        }
        public float LastShotPitch
        {
            get; private set;
        }
        public float LastShotGain
        {
            get; private set;
        }
        public string LastCasingClip
        {
            get; private set;
        }
        public bool ImpactAudioEnabled
        {
            get => impactAudioEnabled; set => impactAudioEnabled = value;
        }

        private void Awake()
        {
            interaction = GetComponent<PlayerInteraction>();
            weapon = GetComponent<PlayerWeapon2D>();
            random = new System.Random(System.Guid.NewGuid().GetHashCode());
            casingRandom = new System.Random(System.Guid.NewGuid().GetHashCode());
        }

        private void OnEnable()
        {
            interaction.ItemCollected += OnPickup;
            weapon.ShotFired += OnShot;
            weapon.SurfaceHit += OnImpact;
        }

        private void OnDisable()
        {
            interaction.ItemCollected -= OnPickup;
            weapon.ShotFired -= OnShot;
            weapon.SurfaceHit -= OnImpact;
            pickupSource?.Stop();
            if (shotSources != null)
            {
                foreach (var source in shotSources)
                {
                    if (source != null)
                    {
                        source.Stop();
                    }
                }
            }

            if (casingSources != null)
            {
                foreach (var source in casingSources)
                {
                    if (source != null)
                    {
                        source.Stop();
                    }
                }
            }

            if (impactSources != null)
            {
                foreach (var source in impactSources)
                {
                    if (source != null)
                    {
                        source.Stop();
                    }
                }
            }
        }

        private AudioClip Choose(AudioClip[] clips, System.Random generator = null)
        {
            if (clips == null || clips.Length == 0)
            {
                return null;
            }

            generator = generator ?? random;
            int index = generator.Next(clips.Length), previous;
            if (clips.Length > 1 && last.TryGetValue(clips, out previous) && index == previous)
            {
                index = (index + generator.Next(1, clips.Length)) % clips.Length;
            }

            last[clips] = index;
            return clips[index];
        }

        private static int PickupPriority(PickupKind kind)
        {
            switch (kind)
            {
                case PickupKind.Weapon:
                    return 3;
                case PickupKind.FlashlightUpgrade:
                case PickupKind.AccessCard:
                    return 2;
                default:
                    return 1;
            }
        }

        private void OnPickup(PickupKind kind)
        {
            if (sounds == null || pickupSource == null)
            {
                return;
            }

            // One short feedback cue per burst; weapon/upgrade can supersede a simultaneous ammo click.
            int priority = PickupPriority(kind);
            if (Time.time < pickupBurstUntil && priority <= pickupPriority)
            {
                return;
            }

            var clip = Choose(sounds.Pickup(kind));
            if (clip == null)
            {
                return;
            }

            pickupPriority = priority;
            pickupBurstUntil = Time.time + .07f;
            pickupSource.clip = clip;
            pickupSource.volume = pickupVolume;
            pickupSource.panStereo = 0;
            pickupSource.Play();
            pickupCounts[(int)kind]++;
        }

        private void OnShot()
        {
            if (!combatAudioEnabled || sounds == null || shotSources == null || shotSources.Length == 0)
            {
                return;
            }

            var clip = Choose(sounds.Shots(indoorShot));
            if (clip == null)
            {
                return;
            }

            AudioSource selected = null;
            foreach (var voice in shotSources)
            {
                if (voice != null && !voice.isPlaying)
                {
                    selected = voice;
                    break;
                }
            }

            // At the current 0.3 s fire interval two voices suffice; a bounded fallback supports Inspector changes.
            if (selected == null)
            {
                selected = shotSources[shotCursor++ % shotSources.Length];
            }

            if (selected == null)
            {
                return;
            }

            LastShotClip = clip.name;
            LastShotPitch = 1 + ((float)random.NextDouble() * 2 - 1) * shotPitchVariation;
            LastShotGain = shotVolume * Mathf.Pow(10, (((float)random.NextDouble() * 2 - 1) * shotGainVariationDb) / 20);
            selected.clip = clip;
            selected.pitch = LastShotPitch;
            selected.volume = LastShotGain;
            selected.Play();
            ShotAudioCount++;
        }

        private void OnImpact(Collider2D target, Vector2 point)
        {
            LastImpactMaterial = ImpactSurface2D.Resolve(target);
            ResolvedImpactCount++;
            if (!combatAudioEnabled || !impactAudioEnabled || sounds == null || impactSources == null || impactSources.Length == 0)
            {
                return;
            }

            // Stalker already plays its approved flesh hit/death through its own damage events.
            // Do not double that layer; generic fleshy objects can use the same data directly.
            var receiver = target != null ? target.GetComponentInParent<Damageable2D>() : null;
            if (LastImpactMaterial == ImpactMaterial.Flesh && receiver != null && receiver.OwnsHitAudio)
            {
                return;
            }

            var clip = Choose(sounds.Impact(LastImpactMaterial));
            if (clip == null)
            {
                return;
            }

            AudioSource selected = null;
            foreach (var source in impactSources)
            {
                if (source != null && !source.isPlaying)
                {
                    selected = source;
                    break;
                }
            }

            if (selected == null)
            {
                return; // Bounded channels instead of unbounded temporary objects.
            }

            float distance = Vector2.Distance(transform.position, point);
            selected.clip = clip;
            selected.volume = impactVolume * Mathf.Clamp01(1 - distance / 16);
            selected.pitch = Mathf.Lerp(.97f, 1.03f, (float)random.NextDouble());
            selected.panStereo = Mathf.Clamp((point.x - transform.position.x) / 6, -.75f, .75f);
            selected.Play();
            ImpactAudioCount++;
        }

        public void PlayCasingAt(Vector2 point, GroundSoundSurface surface)
        {
            if (!isActiveAndEnabled || !combatAudioEnabled || sounds == null || casingSources == null)
            {
                return;
            }

            AudioSource voice = null;
            foreach (var source in casingSources)
            {
                if (source != null && !source.isPlaying)
                {
                    voice = source;
                    break;
                }
            }

            if (voice == null)
            {
                return;
            }

            var clip = Choose(sounds.Casings(surface), casingRandom);
            if (clip == null)
            {
                return;
            }

            float distance = Vector2.Distance(transform.position, point);
            if (distance >= 12)
            {
                return;
            }

            voice.clip = clip;
            voice.pitch = 1;
            voice.volume = casingVolume * Mathf.Clamp01(1 - distance / 12);
            voice.panStereo = Mathf.Clamp((point.x - transform.position.x) / 6, -.75f, .75f);
            voice.Play();
            LastCasingClip = clip.name;
            CasingAudioCount++;
        }

#if UNITY_EDITOR
        public void SetPreviewSeed(int seed)
        {
            random = new System.Random(seed);
            casingRandom = new System.Random(seed + 1);
            last.Clear();
        }

#endif
        [ContextMenu("Preview/Enable combat audio (Play only)")]
        private void EnablePreview()
        {
            if (Application.isPlaying)
            {
                combatAudioEnabled = true;
            }
        }
    }
}
