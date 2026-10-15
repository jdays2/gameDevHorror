using UnityEngine;

namespace EmergencyShift
{
    [CreateAssetMenu(menuName = "Emergency Shift/Weapon catalog")]
    public sealed class WeaponCatalog : ScriptableObject
    {
        public WeaponDefinition[] weapons;
        public WeaponDefinition Find(string id)
        {
            if (weapons == null)
            {
                return null;
            }

            foreach (var weapon in weapons)
            {
                if (weapon != null && weapon.contentId == id)
                {
                    return weapon;
                }
            }

            return null;
        }
    }
}
