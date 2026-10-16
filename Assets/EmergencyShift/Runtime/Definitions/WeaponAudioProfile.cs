using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Weapon-specific recordings; impact materials remain in the shared world palette.</summary>
    [CreateAssetMenu(menuName = "Emergency Shift/Weapons/Audio profile")]
    public sealed class WeaponAudioProfile : ScriptableObject
    {
        public AudioClip[] shots;
        public AudioClip[] pump;
        public AudioClip[] shellInsert;
        public AudioClip[] magazineReload;
        public AudioClip[] casings;
        public AudioClip emptyTrigger;
        [Range(0, 1)]
        public float shotGain = .8f;
        [Range(0, 1)]
        public float mechanismGain = .55f;
        [Range(0, 1)]
        public float casingGain = .3f;
        [Range(0, 1)]
        public float emptyGain = .22f;
        public Color casingColor = new Color(.5f, .12f, .08f);
        public Vector2 casingSize = new Vector2(.11f, .045f);
    }
}
