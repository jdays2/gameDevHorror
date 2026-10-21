using System;
using System.Collections.Generic;
using UnityEngine;

namespace EmergencyShift
{
    [CreateAssetMenu(menuName = "Emergency Shift/Weapon catalog")]
    public sealed class WeaponCatalog : ScriptableObject
    {
        public WeaponDefinition[] weapons;

        public WeaponSettingsCatalog ToSettings()
        {
            if (weapons == null)
            {
                throw new InvalidOperationException("Weapon catalog is missing its definitions: " + name);
            }

            var settings = new List<WeaponSettings>(weapons.Length);
            foreach (WeaponDefinition weapon in weapons)
            {
                if (weapon == null)
                {
                    throw new InvalidOperationException("Weapon catalog contains a missing definition: " + name);
                }

                settings.Add(weapon.ToSettings());
            }

            return new WeaponSettingsCatalog(settings);
        }
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
