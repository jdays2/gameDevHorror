using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Pickup cues are live; combat candidates can be auditioned before enabling gameplay audio.</summary>
    [CreateAssetMenu(menuName = "Emergency Shift/Player audio set")]
    public sealed class PlayerAudioSet : ScriptableObject
    {
        [SerializeField] private AudioClip[] ammoPickup, weaponPickup, upgradePickup;
        [SerializeField] private AudioClip[] dryShots, roomShots;
        [SerializeField] private AudioClip[] concreteImpact, woodImpact, fleshImpact;
        [SerializeField] private AudioClip[] concreteCasings, woodCasings, tileCasings;
        public AudioClip[] Pickup(PickupKind kind) => kind == PickupKind.Ammo ? ammoPickup : kind == PickupKind.Weapon ? weaponPickup : upgradePickup;
        public AudioClip[] Shots(bool indoor) => indoor ? roomShots : dryShots;
        public AudioClip[] Impact(ImpactMaterial material) => material == ImpactMaterial.Wood ? woodImpact : material == ImpactMaterial.Flesh ? fleshImpact : concreteImpact;
        public AudioClip[] Casings(GroundSoundSurface material)
        {
            var selected = material == GroundSoundSurface.Wood ? woodCasings : material == GroundSoundSurface.Tile ? tileCasings : concreteCasings;
            return selected != null && selected.Length > 0 ? selected : concreteCasings;
        }
    }
}
