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
        public float PickupVolume => pickupVolume;
        [SerializeField]
        private float shotVolume = .75f;
        public float ShotVolume => shotVolume;
        [SerializeField]
        private float impactVolume = .65f;
        public float ImpactVolume => impactVolume;
        [SerializeField]
        private float casingVolume = .3f;
        public float CasingVolume => casingVolume;
        [SerializeField]
        private float shotPitchVariation = .008f;
        public float ShotPitchVariation => shotPitchVariation;
        [SerializeField]
        private float shotGainVariationDb = .4f;
        public float ShotGainVariationDb => shotGainVariationDb;
        [SerializeField]
        private float pickupBurstSeconds = .07f;
        public float PickupBurstSeconds => pickupBurstSeconds;
        [SerializeField]
        private float impactHearingDistance = 16;
        public float ImpactHearingDistance => impactHearingDistance;
        [SerializeField]
        private float casingHearingDistance = 12;
        public float CasingHearingDistance => casingHearingDistance;
        [SerializeField]
        private float panDistance = 6;
        public float PanDistance => panDistance;
        [SerializeField]
        private float maximumPan = .75f;
        public float MaximumPan => maximumPan;
        [SerializeField]
        private Vector2 impactPitch = new Vector2(.97f, 1.03f);
        public Vector2 ImpactPitch => impactPitch;
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

        public AudioClipBank Shots(bool indoor) => indoor ? roomShots : dryShots;
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
