using System;
using System.Collections.Generic;

namespace EmergencyShift
{
    /// <summary>Validated content lookup for simulation and save validation, without Unity assets.</summary>
    public sealed class WeaponSettingsCatalog
    {
        private readonly Dictionary<string, WeaponSettings> weapons = new Dictionary<string, WeaponSettings>(StringComparer.Ordinal);

        public WeaponSettingsCatalog(IEnumerable<WeaponSettings> definitions)
        {
            if (definitions == null)
            {
                throw new ArgumentNullException(nameof(definitions));
            }

            var slots = new HashSet<int>();
            foreach (WeaponSettings definition in definitions)
            {
                if (definition == null || weapons.ContainsKey(definition.ContentId) || !slots.Add(definition.SelectionSlot))
                {
                    throw new InvalidOperationException("Weapon catalog contains a missing, duplicate or conflicting definition.");
                }

                weapons.Add(definition.ContentId, definition);
            }
        }

        public WeaponSettings Find(string id)
        {
            return id != null && weapons.TryGetValue(id, out WeaponSettings definition) ? definition : null;
        }
    }
}
