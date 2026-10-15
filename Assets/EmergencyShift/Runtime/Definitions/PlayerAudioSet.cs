using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Pickup cues are live; combat candidates can be auditioned before enabling gameplay audio.</summary>
    [CreateAssetMenu(menuName = "Emergency Shift/Player audio set")]
    public sealed class PlayerAudioSet : ScriptableObject
    {
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
        public AudioClip[] Pickup(PickupKind kind)
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

        public AudioClip[] Shots(bool indoor) => indoor ? roomShots : dryShots;
        public AudioClip[] Impact(ImpactMaterial material)
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

        public AudioClip[] Casings(GroundSoundSurface material)
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
    }
}
