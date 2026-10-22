using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Shared weapon data; ammunition/ownership stay on the player, never in this asset.</summary>
    [CreateAssetMenu(menuName = "Emergency Shift/Weapon definition")]
    public class WeaponDefinition : ScriptableObject
    {
        [SerializeField]
        private string contentId = "pistol";
        [SerializeField]
        private string displayName = "Пистолет";
        [Min(1)]
        [SerializeField]
        private int damage = 1;
        [Min(1)]
        [SerializeField]
        private int maximumAmmo = 120;
        [Min(.05f)]
        [SerializeField]
        private float secondsBetweenShots = .3f;
        [Min(.5f)]
        [SerializeField]
        private float range = 10;
        [SerializeField]
        private ShotVisualProfile visuals;
        [SerializeField]
        private bool automatic;
        [Min(0)]
        [SerializeField]
        private int selectionSlot;
        [SerializeField]
        private WeaponAudioProfile audioProfile;
        public string ContentId => contentId;
        public string DisplayName => displayName;
        public int Damage => damage;
        public int MaximumAmmo => maximumAmmo;
        public float SecondsBetweenShots => secondsBetweenShots;
        public float Range => range;
        public ShotVisualProfile Visuals => visuals;
        public bool Automatic => automatic;
        public int SelectionSlot => selectionSlot;
        public WeaponAudioProfile AudioProfile => audioProfile;

        public virtual int ProjectileCount => 1;
        public virtual Vector3 HeldScale => Vector3.one;

        public virtual WeaponCycle CreateCycle(int cartridges)
        {
            Validate();
            return ToSettings().CreateCycle(cartridges);
        }

        public virtual WeaponSettings ToSettings()
        {
            Validate();
            return new WeaponSettings(contentId, selectionSlot, damage, maximumAmmo, secondsBetweenShots, range, automatic);
        }

        public virtual void Validate()
        {
            if (string.IsNullOrWhiteSpace(contentId) || maximumAmmo < 1 || damage < 1 ||
                !float.IsFinite(secondsBetweenShots) || secondsBetweenShots < .05f ||
                !float.IsFinite(range) || range < .5f || selectionSlot < 0)
            {
                throw new System.InvalidOperationException("Invalid weapon definition: " + name);
            }

            if (visuals != null)
            {
                visuals.Validate();
            }
        }
    }
}
