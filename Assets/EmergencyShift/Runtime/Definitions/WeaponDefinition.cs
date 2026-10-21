using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Shared weapon data; ammunition/ownership stay on the player, never in this asset.</summary>
    [CreateAssetMenu(menuName = "Emergency Shift/Weapon definition")]
    public class WeaponDefinition : ScriptableObject
    {
        public string contentId = "pistol";
        public string displayName = "Пистолет";
        [Min(1)]
        public int damage = 1;
        [Min(1)]
        public int maximumAmmo = 120;
        [Min(.05f)]
        public float secondsBetweenShots = .3f;
        [Min(.5f)]
        public float range = 10;
        public ShotVisualProfile visuals;
        public bool automatic;
        [Min(0)]
        public int selectionSlot;
        public WeaponAudioProfile audioProfile;
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
        }
    }
}
