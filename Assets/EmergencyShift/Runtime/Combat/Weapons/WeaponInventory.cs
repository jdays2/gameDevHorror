using System;
using System.Collections.Generic;
using EmergencyShift.Core.Commands;

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
            public readonly WeaponSettings Definition;
            public readonly WeaponCycle Cycle;

            public OwnedWeapon(WeaponSettings definition, WeaponCycle cycle)
            {
                Definition = definition;
                Cycle = cycle;
            }
        }

        private readonly Dictionary<string, OwnedWeapon> owned = new Dictionary<string, OwnedWeapon>();
        private OwnedWeapon active;
        public WeaponSettings ActiveDefinition => active?.Definition;
        public WeaponCycle ActiveCycle => active?.Cycle;
        public int Count => owned.Count;

        public bool Acquire(WeaponSettings definition, int cartridges)
        {
            return new AcquireWeaponCommand(this, definition, cartridges).Execute().Success;
        }

        internal CommandResult CheckAcquire(WeaponSettings definition)
        {
            if (definition == null || owned.ContainsKey(definition.ContentId)
                || (active != null && !active.Cycle.CanHolster))
            {
                return CommandResult.Fail("The weapon is already owned or cannot be equipped.");
            }

            foreach (OwnedWeapon entry in owned.Values)
            {
                if (entry.Definition.SelectionSlot == definition.SelectionSlot)
                {
                    return CommandResult.Fail("Two different weapons share a selection slot.");
                }
            }

            return CommandResult.Ok();
        }

        internal void ApplyAcquire(WeaponSettings definition, int cartridges)
        {
            var next = new OwnedWeapon(definition, definition.CreateCycle(cartridges));
            active?.Cycle.CancelReload();
            active = next;
            owned.Add(definition.ContentId, next);
        }

        public bool Equip(int selectionSlot) => new EquipWeaponCommand(this, selectionSlot).Execute().Success;

        internal CommandResult CheckEquip(int selectionSlot)
        {
            OwnedWeapon next = FindSelection(selectionSlot);
            return next != null && next != active && (active == null || active.Cycle.CanHolster)
                ? CommandResult.Ok() : CommandResult.Fail("The requested weapon cannot be equipped now.");
        }

        internal void ApplyEquip(int selectionSlot)
        {
            OwnedWeapon next = FindSelection(selectionSlot);
            active?.Cycle.CancelReload();
            active = next;
        }

        private OwnedWeapon FindSelection(int selectionSlot)
        {
            foreach (OwnedWeapon entry in owned.Values)
            {
                if (entry.Definition.SelectionSlot == selectionSlot)
                {
                    return entry;
                }
            }

            return null;
        }

        public int AddAmmo(int cartridges, WeaponSettings target = null)
        {
            var entry = active;
            if (target != null && !owned.TryGetValue(target.ContentId, out entry))
            {
                return 0;
            }

            return entry != null ? entry.Cycle.AddAmmo(cartridges) : 0;
        }

        public void ReplaceActiveDefinition(WeaponSettings definition)
        {
            if (active == null)
            {
                return;
            }

            CommandResult result = new ReplaceWeaponDefinitionCommand(this, definition).Execute();
            if (!result.Success)
            {
                throw new InvalidOperationException(result.Reason);
            }
        }

        internal CommandResult CheckReplacement(WeaponSettings definition)
        {
            if (active == null || definition == null)
            {
                return CommandResult.Fail("No active weapon or replacement definition.");
            }

            foreach (OwnedWeapon entry in owned.Values)
            {
                if (entry != active && (entry.Definition.ContentId == definition.ContentId
                    || entry.Definition.SelectionSlot == definition.SelectionSlot))
                {
                    return CommandResult.Fail("Replacement conflicts with another owned weapon.");
                }
            }

            return CommandResult.Ok();
        }

        internal void ApplyReplacement(WeaponSettings definition)
        {
            var replacement = new OwnedWeapon(definition, definition.CreateCycle(active.Cycle.TotalAmmo));
            owned.Remove(active.Definition.ContentId);
            active = replacement;
            owned.Add(definition.ContentId, replacement);
        }

        public WeaponInventorySnapshot Capture(float now)
        {
            var records = new List<OwnedWeaponSnapshot>();
            foreach (var entry in owned.Values)
            {
                records.Add(new OwnedWeaponSnapshot
                {
                    contentId = entry.Definition.ContentId,
                    cycle = entry.Cycle.Capture(now)
                });
            }

            return new WeaponInventorySnapshot
            {
                format = 1,
                initialized = true,
                activeId = active != null ? active.Definition.ContentId : "",
                weapons = records.ToArray()
            };
        }

        public static void Validate(WeaponInventorySnapshot state, WeaponSettingsCatalog catalog)
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
                if (data == null || !ids.Add(data.ContentId) || !slots.Add(data.SelectionSlot))
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

        public void Restore(WeaponInventorySnapshot state, WeaponSettingsCatalog catalog, float now)
        {
            CommandResult result = new RestoreWeaponInventoryCommand(this, state, catalog, now).Execute();
            if (!result.Success)
            {
                throw new InvalidOperationException(result.Reason);
            }
        }

        internal void ApplyRestore(WeaponInventorySnapshot state, WeaponSettingsCatalog catalog, float now)
        {
            var next = new Dictionary<string, OwnedWeapon>(StringComparer.Ordinal);
            foreach (OwnedWeaponSnapshot entry in state.weapons)
            {
                WeaponSettings data = catalog.Find(entry.contentId);
                WeaponCycle cycle = data.CreateCycle(0);
                cycle.Restore(entry.cycle, now);
                next.Add(data.ContentId, new OwnedWeapon(data, cycle));
            }

            // Construct the complete replacement before discarding any live state.
            owned.Clear();
            foreach (KeyValuePair<string, OwnedWeapon> entry in next)
            {
                owned.Add(entry.Key, entry.Value);
            }

            active = string.IsNullOrEmpty(state.activeId) ? null : owned[state.activeId];
        }

        public void RestoreLegacy(bool hasWeapon, int cartridges, WeaponSettings definition)
        {
            WeaponSettings[] definitions = hasWeapon ? new[] { definition } : Array.Empty<WeaponSettings>();
            var catalog = new WeaponSettingsCatalog(definitions);
            OwnedWeaponSnapshot[] records = hasWeapon
                ? new[] { new OwnedWeaponSnapshot { contentId = definition.ContentId, cycle = definition.CreateCycle(cartridges).Capture(0) } }
                : Array.Empty<OwnedWeaponSnapshot>();
            Restore(new WeaponInventorySnapshot
            {
                format = 1,
                initialized = true,
                activeId = hasWeapon ? definition.ContentId : "",
                weapons = records
            }, catalog, 0);
        }
    }
}
