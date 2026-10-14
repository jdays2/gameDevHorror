using System.Collections.Generic;
using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Saved, bounded channels for pickup feedback and optional combat audition.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(PlayerInteraction), typeof(PlayerWeapon2D))]
    public sealed class PlayerAudio2D : MonoBehaviour
    {
        [SerializeField] private PlayerAudioSet sounds;
        [SerializeField] private AudioSource pickupSource, shotSource;
        [SerializeField] private AudioSource[] impactSources;
        [SerializeField, Range(0, 1)] private float pickupVolume = .65f;
        [Header("Combat prototype — disabled until artistic approval")]
        [SerializeField] private bool combatAudioEnabled;
        [SerializeField] private bool indoorShot;
        [SerializeField, Range(0, 1)] private float shotVolume = .75f;
        [SerializeField, Range(0, 1)] private float impactVolume = .65f;
        private PlayerInteraction interaction;
        private PlayerWeapon2D weapon;
        private readonly Dictionary<AudioClip[], int> last = new Dictionary<AudioClip[], int>();
        private readonly int[] pickupCounts = new int[3];
        private System.Random random;
        private float pickupBurstUntil;
        private int pickupPriority;
        public bool CombatAudioEnabled { get => combatAudioEnabled; set => combatAudioEnabled = value; }
        public int PickupCount(PickupKind kind) => pickupCounts[(int)kind];
        public int ShotAudioCount { get; private set; }
        public int ImpactAudioCount { get; private set; }
        public int ResolvedImpactCount { get; private set; }
        public ImpactMaterial LastImpactMaterial { get; private set; }

        private void Awake()
        {
            interaction = GetComponent<PlayerInteraction>(); weapon = GetComponent<PlayerWeapon2D>();
            random = new System.Random(System.Guid.NewGuid().GetHashCode());
        }
        private void OnEnable()
        { interaction.ItemCollected += OnPickup; weapon.ShotFired += OnShot; weapon.SurfaceHit += OnImpact; }
        private void OnDisable()
        {
            interaction.ItemCollected -= OnPickup; weapon.ShotFired -= OnShot; weapon.SurfaceHit -= OnImpact;
            pickupSource?.Stop(); shotSource?.Stop();
            if (impactSources != null) foreach (var source in impactSources) if (source != null) source.Stop();
        }

        private AudioClip Choose(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0) return null;
            int index = random.Next(clips.Length), previous;
            if (clips.Length > 1 && last.TryGetValue(clips, out previous) && index == previous)
                index = (index + random.Next(1, clips.Length)) % clips.Length;
            last[clips] = index; return clips[index];
        }

        private void OnPickup(PickupKind kind)
        {
            if (sounds == null || pickupSource == null) return;
            // One short feedback cue per burst; weapon/upgrade can supersede a simultaneous ammo click.
            int priority = kind == PickupKind.Weapon ? 3 : kind == PickupKind.FlashlightUpgrade ? 2 : 1;
            if (Time.time < pickupBurstUntil && priority <= pickupPriority) return;
            var clip = Choose(sounds.Pickup(kind)); if (clip == null) return;
            pickupPriority = priority; pickupBurstUntil = Time.time + .07f;
            pickupSource.clip = clip; pickupSource.volume = pickupVolume; pickupSource.panStereo = 0; pickupSource.Play();
            pickupCounts[(int)kind]++;
        }

        private void OnShot()
        {
            if (!combatAudioEnabled || sounds == null || shotSource == null) return;
            var clip = Choose(sounds.Shots(indoorShot)); if (clip == null) return;
            shotSource.clip = clip; shotSource.volume = shotVolume; shotSource.Play(); ShotAudioCount++;
        }

        private void OnImpact(Collider2D target, Vector2 point)
        {
            LastImpactMaterial = ImpactSurface2D.Resolve(target); ResolvedImpactCount++;
            if (!combatAudioEnabled || sounds == null || impactSources == null || impactSources.Length == 0) return;
            // Stalker already plays its approved flesh hit/death through its own damage events.
            // Do not double that layer; generic fleshy objects can use the same data directly.
            var creatureAudio = target != null ? target.GetComponentInParent<StalkerAudio2D>() : null;
            if (LastImpactMaterial == ImpactMaterial.Flesh && creatureAudio != null && creatureAudio.isActiveAndEnabled) return;
            var clip = Choose(sounds.Impact(LastImpactMaterial)); if (clip == null) return;
            AudioSource selected = null;
            foreach (var source in impactSources) if (source != null && !source.isPlaying) { selected = source; break; }
            if (selected == null) return; // Bounded channels instead of unbounded temporary objects.
            float distance = Vector2.Distance(transform.position, point);
            selected.clip = clip; selected.volume = impactVolume * Mathf.Clamp01(1 - distance / 16);
            selected.panStereo = Mathf.Clamp((point.x - transform.position.x) / 6, -.75f, .75f);
            selected.Play(); ImpactAudioCount++;
        }

        [ContextMenu("Preview/Enable combat audio (Play only)")]
        private void EnablePreview() { if (Application.isPlaying) combatAudioEnabled = true; }
    }
}
