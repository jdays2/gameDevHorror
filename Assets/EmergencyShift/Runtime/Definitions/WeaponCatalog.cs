using System;
using System.Collections.Generic;
using UnityEngine;

namespace EmergencyShift
{
    [CreateAssetMenu(menuName = "Emergency Shift/Weapon catalog")]
    public sealed class WeaponCatalog : ScriptableObject
    {
        [SerializeField]
        private WeaponDefinition[] weapons;
        public int DefinitionCount => weapons?.Length ?? 0;

        public WeaponDefinition DefinitionAt(int index)
        {
            if (index < 0 || index >= DefinitionCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return weapons[index];
        }

        public WeaponSettingsCatalog ToSettings()
        {
            Validate();
            var settings = new List<WeaponSettings>(weapons.Length);
            foreach (WeaponDefinition weapon in weapons)
            {
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

            Validate();

            foreach (var weapon in weapons)
            {
                if (weapon != null && weapon.ContentId == id)
                {
                    return weapon;
                }
            }

            return null;
        }

        public void Validate()
        {
            if (weapons == null)
            {
                throw new InvalidOperationException("Weapon catalog is missing its definitions: " + name);
            }

            for (int index = 0; index < weapons.Length; index++)
            {
                WeaponDefinition weapon = weapons[index];
                if (weapon == null)
                {
                    throw new InvalidOperationException("Weapon catalog contains a missing definition: " + name);
                }

                weapon.Validate();
                for (int earlier = 0; earlier < index; earlier++)
                {
                    if (string.Equals(weapons[earlier].ContentId, weapon.ContentId, StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException("Duplicate weapon content ID: " + weapon.ContentId);
                    }

                    if (weapons[earlier].SelectionSlot == weapon.SelectionSlot)
                    {
                        throw new InvalidOperationException("Conflicting weapon selection slot: " + weapon.SelectionSlot);
                    }
                }
            }
        }
    }
}
