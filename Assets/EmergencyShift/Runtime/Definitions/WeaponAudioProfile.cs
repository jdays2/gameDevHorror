using System;
using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Weapon-specific recordings; impact materials remain in the shared world palette.</summary>
    [CreateAssetMenu(menuName = "Emergency Shift/Weapons/Audio profile")]
    public sealed class WeaponAudioProfile : ScriptableObject
    {
        [SerializeField]
        private AudioClip[] shots;
        [SerializeField]
        private AudioClip[] pump;
        [SerializeField]
        private AudioClip[] shellInsert;
        [SerializeField]
        private AudioClip[] magazineReload;
        [SerializeField]
        private AudioClip[] casings;
        [SerializeField]
        private AudioClip emptyTrigger;
        [Range(0, 1)]
        [SerializeField]
        private float shotGain = .8f;
        [Range(0, 1)]
        [SerializeField]
        private float mechanismGain = .55f;
        [Range(0, 1)]
        [SerializeField]
        private float casingGain = .3f;
        [Range(0, 1)]
        [SerializeField]
        private float emptyGain = .22f;
        [SerializeField]
        private Color casingColor = new Color(.5f, .12f, .08f);
        [SerializeField]
        private Vector2 casingSize = new Vector2(.11f, .045f);
        public AudioClipBank Shots => shots;
        public AudioClipBank Pump => pump;
        public AudioClipBank ShellInsert => shellInsert;
        public AudioClipBank MagazineReload => magazineReload;
        public AudioClipBank Casings => casings;
        public AudioClip EmptyTrigger => emptyTrigger;
        public float ShotGain => shotGain;
        public float MechanismGain => mechanismGain;
        public float CasingGain => casingGain;
        public float EmptyGain => emptyGain;
        public Color CasingColor => casingColor;
        public Vector2 CasingSize => casingSize;

        public void Validate()
        {
            if (!AudioProfileValidation.Unit(shotGain) || !AudioProfileValidation.Unit(mechanismGain) ||
                !AudioProfileValidation.Unit(casingGain) || !AudioProfileValidation.Unit(emptyGain) ||
                !AudioProfileValidation.Color(casingColor) || !AudioProfileValidation.Positive(casingSize.x) ||
                !AudioProfileValidation.Positive(casingSize.y))
            {
                throw new InvalidOperationException("Invalid weapon audio profile: " + name);
            }
        }
    }
}
