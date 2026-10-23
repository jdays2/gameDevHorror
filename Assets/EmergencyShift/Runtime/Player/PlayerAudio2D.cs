using System.Collections.Generic;
using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Saved, bounded channels for pickup feedback and optional combat audition.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(PlayerInteraction), typeof(PlayerWeapon2D))]
    public sealed class PlayerAudio2D : MonoBehaviour
    {
        [SerializeField, Tooltip("Use all local values, including values equal to former defaults.")]
        private bool useAuthoredAudioTuning;
        private float PickupVolume => AudioProfileValidation.Resolve(pickupVolume, .65f, sounds != null ? sounds.PickupVolume : .65f, useAuthoredAudioTuning);
        private float ShotVolume => AudioProfileValidation.Resolve(shotVolume, .75f, sounds != null ? sounds.ShotVolume : .75f, useAuthoredAudioTuning);
        private float ImpactVolume => AudioProfileValidation.Resolve(impactVolume, .65f, sounds != null ? sounds.ImpactVolume : .65f, useAuthoredAudioTuning);
        private float CasingVolume => AudioProfileValidation.Resolve(casingVolume, .3f, sounds != null ? sounds.CasingVolume : .3f, useAuthoredAudioTuning);
        private float ShotPitchVariation => AudioProfileValidation.Resolve(shotPitchVariation, .008f, sounds != null ? sounds.ShotPitchVariation : .008f, useAuthoredAudioTuning);
        private float ShotGainVariationDb => AudioProfileValidation.Resolve(shotGainVariationDb, .4f, sounds != null ? sounds.ShotGainVariationDb : .4f, useAuthoredAudioTuning);
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
        [Header("Combat audio and legacy fallback")]
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
        private readonly Dictionary<AudioClipBank, int> last = new Dictionary<AudioClipBank, int>();
        private readonly int[] pickupCounts = new int[System.Enum.GetValues(typeof(PickupKind)).Length];
        private System.Random random;
        private System.Random casingRandom;
        private float pickupBurstUntil;
        private int pickupPriority;
        private int shotCursor;
        private AudioSource reloadVoice;
        private const int RememberedVolleys = 64;
        private readonly Dictionary<int, int> audibleImpacts = new Dictionary<int, int>();
        private readonly Queue<int> impactOrder = new Queue<int>();
        public int MechanismAudioCount { get; private set; }
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

        private void ValidateResolvedTuning()
        {
            if (!AudioProfileValidation.Unit(PickupVolume) ||
                !AudioProfileValidation.Unit(ShotVolume) ||
                !AudioProfileValidation.Unit(ImpactVolume) ||
                !AudioProfileValidation.Unit(CasingVolume) ||
                !AudioProfileValidation.Unit(ShotPitchVariation) ||
                !AudioProfileValidation.AtLeast(ShotGainVariationDb, 0))
            {
                throw new System.InvalidOperationException("Invalid resolved audio tuning: " + name);
            }
        }

        private void Awake()
        {
            sounds?.Validate();
            ValidateResolvedTuning();
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
            weapon.Operation += OnOperation;
        }

        private void OnDisable()
        {
            reloadVoice = null;
            interaction.ItemCollected -= OnPickup;
            weapon.ShotFired -= OnShot;
            weapon.SurfaceHit -= OnImpact;
            weapon.Operation -= OnOperation;
            audibleImpacts.Clear();
            impactOrder.Clear();
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

        private AudioClip Choose(AudioClipBank clips, System.Random generator = null)
        {
            if (clips.Count == 0)
            {
                return null;
            }

            generator = generator ?? random;
            int index = generator.Next(clips.Count);
            int previous;
            if (clips.Count > 1 && last.TryGetValue(clips, out previous) && index == previous)
            {
                index = (index + generator.Next(1, clips.Count)) % clips.Count;
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
            pickupBurstUntil = Time.time + sounds.PickupBurstSeconds;
            pickupSource.clip = clip;
            pickupSource.volume = PickupVolume;
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

            var profile = weapon.Definition.AudioProfile;
            profile?.Validate();
            var clip = Choose(profile != null ? profile.Shots : sounds.Shots(indoorShot));
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

            // Preserve overlapping shot tails; a bounded fallback supports shorter Inspector intervals.
            if (selected == null)
            {
                selected = shotSources[shotCursor];
                shotCursor = (shotCursor + 1) % shotSources.Length;
            }

            if (selected == null)
            {
                return;
            }

            LastShotClip = clip.name;
            LastShotPitch = 1 + ((float)random.NextDouble() * 2 - 1) * ShotPitchVariation;
            float gain = profile != null ? profile.ShotGain : ShotVolume;
            LastShotGain = gain * Mathf.Pow(10, (((float)random.NextDouble() * 2 - 1) * ShotGainVariationDb) / 20);
            selected.clip = clip;
            selected.pitch = LastShotPitch;
            selected.volume = LastShotGain;
            selected.Play();
            ShotAudioCount++;
        }

        private void OnOperation(WeaponOperation operation)
        {
            if (operation == WeaponOperation.ReloadCancelled)
            {
                if (reloadVoice != null)
                {
                    reloadVoice.Stop();
                    reloadVoice = null;
                }

                return;
            }

            var profile = weapon.Definition.AudioProfile;
            profile?.Validate();
            if (!combatAudioEnabled || profile == null || casingSources == null)
            {
                return;
            }

            AudioClipBank clips;
            switch (operation)
            {
                case WeaponOperation.Pump:
                    clips = profile.Pump;
                    break;
                case WeaponOperation.ShellInserted:
                    clips = profile.ShellInsert;
                    break;
                case WeaponOperation.MagazineReloadStarted:
                    clips = profile.MagazineReload;
                    break;
                case WeaponOperation.MagazineLoaded:
                    reloadVoice = null;
                    return;
                default:
                    return;
            }

            var clip = Choose(clips);
            if (clip == null)
            {
                return;
            }

            foreach (var source in casingSources)
            {
                if (source != null && !source.isPlaying)
                {
                    source.clip = clip;
                    source.volume = profile.MechanismGain;
                    source.pitch = 1;
                    source.panStereo = 0;
                    source.Play();
                    if (operation == WeaponOperation.MagazineReloadStarted)
                    {
                        reloadVoice = source;
                    }
                    MechanismAudioCount++;
                    return;
                }
            }
        }

        private void OnImpact(Collider2D target, Vector2 point)
        {
            LastImpactMaterial = ImpactSurface2D.Resolve(target);
            ResolvedImpactCount++;
            if (!combatAudioEnabled || !impactAudioEnabled || sounds == null || impactSources == null || impactSources.Length == 0)
            {
                return;
            }

            // A physical contact is independent of the creature's optional hurt/death voice.
            int volley = weapon.LastImpactVolleyId;
            int materialBit = 1 << (int)LastImpactMaterial;
            if (volley > 0 && audibleImpacts.TryGetValue(volley, out int played) && (played & materialBit) != 0)
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
            float gain = LastImpactMaterial == ImpactMaterial.Flesh ? sounds.FleshImpactVolume : ImpactVolume;
            selected.volume = gain * Mathf.Clamp01(1 - distance / sounds.ImpactHearingDistance);
            selected.pitch = Mathf.Lerp(sounds.ImpactPitch.x, sounds.ImpactPitch.y, (float)random.NextDouble());
            selected.panStereo = Mathf.Clamp((point.x - transform.position.x) / sounds.PanDistance, -sounds.MaximumPan, sounds.MaximumPan);
            selected.Play();
            ImpactAudioCount++;
            if (volley > 0)
            {
                if (!audibleImpacts.ContainsKey(volley))
                {
                    impactOrder.Enqueue(volley);
                    audibleImpacts.Add(volley, 0);
                }

                audibleImpacts[volley] |= materialBit;
                while (impactOrder.Count > RememberedVolleys)
                {
                    audibleImpacts.Remove(impactOrder.Dequeue());
                }
            }
        }

        public void PlayCasingAt(Vector2 point, GroundSoundSurface surface, WeaponAudioProfile profile = null)
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

            profile?.Validate();
            var clip = Choose(profile != null ? profile.Casings : sounds.Casings(surface), casingRandom);
            if (clip == null)
            {
                return;
            }

            float distance = Vector2.Distance(transform.position, point);
            if (distance >= sounds.CasingHearingDistance)
            {
                return;
            }

            voice.clip = clip;
            voice.pitch = 1;
            float gain = profile != null ? profile.CasingGain : CasingVolume;
            voice.volume = gain * Mathf.Clamp01(1 - distance / sounds.CasingHearingDistance);
            voice.panStereo = Mathf.Clamp((point.x - transform.position.x) / sounds.PanDistance, -sounds.MaximumPan, sounds.MaximumPan);
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
