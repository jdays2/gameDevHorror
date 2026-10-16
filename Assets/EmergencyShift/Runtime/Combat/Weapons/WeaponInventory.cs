using System;
using System.Collections.Generic;

namespace EmergencyShift
{
    [Serializable]
    public sealed class OwnedWeaponSnapshot
    {
        public string contentId;
        public WeaponCycleSnapshot cycle;
    }

    [Serializable]
    public sealed class WeaponInventorySnapshot
    {
        public int format;
        public bool initialized;
        public string activeId;
        public OwnedWeaponSnapshot[] weapons;
    }

    /// <summary>Owns independent weapon instances. Definitions are immutable content, snapshots contain only values/IDs.</summary>
    public sealed class WeaponInventory
    {
        private sealed class OwnedWeapon
        {
            public readonly WeaponDefinition Definition;
            public readonly WeaponCycle Cycle;

            public OwnedWeapon(WeaponDefinition definition, WeaponCycle cycle)
            {
                Definition = definition;
                Cycle = cycle;
            }
        }

        private readonly Dictionary<string, OwnedWeapon> owned = new Dictionary<string, OwnedWeapon>();
        private OwnedWeapon active;
        public WeaponDefinition ActiveDefinition => active?.Definition;
        public WeaponCycle ActiveCycle => active?.Cycle;
        public int Count => owned.Count;

        public bool Acquire(WeaponDefinition definition, int cartridges)
        {
            definition.Validate();
            if (owned.ContainsKey(definition.contentId) || (active != null && !active.Cycle.CanHolster))
            {
                return false;
            }

            foreach (var entry in owned.Values)
            {
                if (entry.Definition.selectionSlot == definition.selectionSlot)
                {
                    throw new InvalidOperationException("Two different weapons share a selection slot");
                }
            }

            active?.Cycle.CancelReload();
            active = new OwnedWeapon(definition, definition.CreateCycle(cartridges));
            owned.Add(definition.contentId, active);
            return true;
        }

        public bool Equip(int selectionSlot)
        {
            if (active != null && !active.Cycle.CanHolster)
            {
                return false;
            }

            foreach (var entry in owned.Values)
            {
                if (entry.Definition.selectionSlot == selectionSlot && entry != active)
                {
                    active?.Cycle.CancelReload();
                    active = entry;
                    return true;
                }
            }

            return false;
        }

        public int AddAmmo(int cartridges, WeaponDefinition target = null)
        {
            var entry = active;
            if (target != null && !owned.TryGetValue(target.contentId, out entry))
            {
                return 0;
            }

            return entry != null ? entry.Cycle.AddAmmo(cartridges) : 0;
        }

        public void ReplaceActiveDefinition(WeaponDefinition definition)
        {
            if (active == null)
            {
                return;
            }

            definition.Validate();
            foreach (var entry in owned.Values)
            {
                if (entry != active && (entry.Definition.contentId == definition.contentId ||
                    entry.Definition.selectionSlot == definition.selectionSlot))
                {
                    throw new InvalidOperationException("Replacement conflicts with another owned weapon");
                }
            }

            var replacement = new OwnedWeapon(definition, definition.CreateCycle(active.Cycle.TotalAmmo));
            owned.Remove(active.Definition.contentId);
            active = replacement;
            owned.Add(definition.contentId, active);
        }

        public WeaponInventorySnapshot Capture(float now)
        {
            var records = new List<OwnedWeaponSnapshot>();
            foreach (var entry in owned.Values)
            {
                records.Add(new OwnedWeaponSnapshot
                {
                    contentId = entry.Definition.contentId,
                    cycle = entry.Cycle.Capture(now)
                });
            }

            return new WeaponInventorySnapshot
            {
                format = 1,
                initialized = true,
                activeId = active != null ? active.Definition.contentId : "",
                weapons = records.ToArray()
            };
        }

        public static void Validate(WeaponInventorySnapshot state, WeaponCatalog catalog)
        {
            if (state == null || state.format != 1 || !state.initialized || state.weapons == null || catalog == null)
            {
                throw new InvalidOperationException("Missing weapon inventory payload/catalog");
            }

            var ids = new HashSet<string>();
            var slots = new HashSet<int>();
            foreach (var entry in state.weapons)
            {
                var data = entry != null ? catalog.Find(entry.contentId) : null;
                if (data == null || !ids.Add(data.contentId) || !slots.Add(data.selectionSlot))
                {
                    throw new InvalidOperationException("Unknown/duplicate weapon in inventory snapshot");
                }

                data.CreateCycle(0).Validate(entry.cycle);
            }

            if ((ids.Count == 0 && !string.IsNullOrEmpty(state.activeId)) ||
                (ids.Count > 0 && !ids.Contains(state.activeId)))
            {
                throw new InvalidOperationException("Saved active weapon is missing");
            }
        }

        public void Restore(WeaponInventorySnapshot state, WeaponCatalog catalog, float now)
        {
            Validate(state, catalog);
            owned.Clear();
            active = null;
            foreach (var entry in state.weapons)
            {
                var data = catalog.Find(entry.contentId);
                var cycle = data.CreateCycle(0);
                cycle.Restore(entry.cycle, now);
                var restored = new OwnedWeapon(data, cycle);
                owned.Add(data.contentId, restored);
                if (data.contentId == state.activeId)
                {
                    active = restored;
                }
            }
        }

        public void RestoreLegacy(bool hasWeapon, int cartridges, WeaponDefinition definition)
        {
            owned.Clear();
            active = null;
            if (hasWeapon)
            {
                Acquire(definition, cartridges);
            }
        }
    }
}
