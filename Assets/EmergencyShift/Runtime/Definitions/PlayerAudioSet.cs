using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Pickup cues are live; combat candidates can be auditioned before enabling gameplay audio.</summary>
    [CreateAssetMenu(menuName = "Emergency Shift/Player audio set")]
    public sealed class PlayerAudioSet : ScriptableObject
    {

        [Header("Shared playback tuning")]
        [SerializeField]
        private float pickupVolume = .65f;
        [SerializeField]
        private float shotVolume = .75f;
        [SerializeField]
        private float impactVolume = .65f;
        [SerializeField, Range(0, 1), Tooltip("Short physical contact, independent of creature voices and legacy wall impact tuning.")]
        private float fleshImpactVolume = .4f;
        [SerializeField]
        private float casingVolume = .3f;
        [SerializeField]
        private float shotPitchVariation = .008f;
        [SerializeField]
        private float shotGainVariationDb = .4f;
        [SerializeField]
        private float pickupBurstSeconds = .07f;
        [SerializeField]
        private float impactHearingDistance = 16;
        [SerializeField]
        private float casingHearingDistance = 12;
        [SerializeField]
        private float panDistance = 6;
        [SerializeField]
        private float maximumPan = .75f;
        [SerializeField]
        private Vector2 impactPitch = new Vector2(.97f, 1.03f);
        [SerializeField]
        private AudioClip[] ammoPickup;
        [SerializeField]
        private AudioClip[] weaponPickup;
        [SerializeField]
        private AudioClip[] upgradePickup;
        [SerializeField]
        private AudioClip[] dryShots;
        [SerializeField]
        private AudioClip[] roomShots;
        [SerializeField]
        private AudioClip[] concreteImpact;
        [SerializeField]
        private AudioClip[] woodImpact;
        [SerializeField]
        private AudioClip[] fleshImpact;
        [SerializeField]
        private AudioClip[] concreteCasings;
        [SerializeField]
        private AudioClip[] woodCasings;
        [SerializeField]
        private AudioClip[] tileCasings;
        public float PickupVolume => pickupVolume;
        public float ShotVolume => shotVolume;
        public float ImpactVolume => impactVolume;
        public float FleshImpactVolume => fleshImpactVolume;
        public float CasingVolume => casingVolume;
        public float ShotPitchVariation => shotPitchVariation;
        public float ShotGainVariationDb => shotGainVariationDb;
        public float PickupBurstSeconds => pickupBurstSeconds;
        public float ImpactHearingDistance => impactHearingDistance;
        public float CasingHearingDistance => casingHearingDistance;
        public float PanDistance => panDistance;
        public float MaximumPan => maximumPan;
        public Vector2 ImpactPitch => impactPitch;
        public AudioClipBank Shots(bool indoor) => indoor ? roomShots : dryShots;


        public AudioClipBank Pickup(PickupKind kind)
        {
            switch (kind)
            {
                case PickupKind.Ammo:
                    return ammoPickup;
                case PickupKind.Weapon:
                    return weaponPickup;
                default:
                    return upgradePickup;
            }
        }

        public AudioClipBank Impact(ImpactMaterial material)
        {
            switch (material)
            {
                case ImpactMaterial.Wood:
                    return woodImpact;
                case ImpactMaterial.Flesh:
                    return fleshImpact;
                default:
                    return concreteImpact;
            }
        }

        public AudioClipBank Casings(GroundSoundSurface material)
        {
            AudioClip[] selected;
            switch (material)
            {
                case GroundSoundSurface.Wood:
                    selected = woodCasings;
                    break;
                case GroundSoundSurface.Tile:
                    selected = tileCasings;
                    break;
                default:
                    selected = concreteCasings;
                    break;
            }

            return selected != null && selected.Length > 0 ? selected : concreteCasings;
        }
        public void Validate()
        {
            if (!AudioProfileValidation.Unit(pickupVolume) ||
                !AudioProfileValidation.Unit(shotVolume) ||
                !AudioProfileValidation.Unit(impactVolume) ||
                !AudioProfileValidation.Unit(fleshImpactVolume) ||
                !AudioProfileValidation.Unit(casingVolume) ||
                !AudioProfileValidation.Unit(shotPitchVariation) ||
                !AudioProfileValidation.AtLeast(shotGainVariationDb, 0) ||
                !AudioProfileValidation.AtLeast(pickupBurstSeconds, 0) ||
                !AudioProfileValidation.Positive(impactHearingDistance) ||
                !AudioProfileValidation.Positive(casingHearingDistance) ||
                !AudioProfileValidation.Positive(panDistance) ||
                !AudioProfileValidation.Unit(maximumPan) ||
                !AudioProfileValidation.Interval(impactPitch))
            {
                throw new System.InvalidOperationException("Invalid playback tuning: " + name);
            }
        }
    }
}
