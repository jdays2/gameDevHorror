using System;
using EmergencyShift.Features.AccessControl.Logic;
using UnityEngine;

namespace EmergencyShift
{
    [DisallowMultipleComponent, RequireComponent(typeof(PersistentEntity2D))]
    public sealed class PlayerCheckpoint2D : MonoBehaviour, ICheckpointParticipant
    {
        [SerializeField]
        private WeaponCatalog weapons;
        [Serializable]
        public sealed class State
        {
            public int format;
            public int health;
            public string[] cards;
            public bool weapon;
            public string weaponId;
            public int ammo;
            public FlashlightUpgrades.ModuleState[] modules;
            public int moduleCount;
            public bool light;
            public Vector2 direction;
            public bool hasInventory;
            public WeaponInventorySnapshot inventory;
            public bool hasFlashlightState;
            public bool flashlightOwned;
        }

        public string SaveKey => "player.v1";

        public void Configure(WeaponCatalog catalog)
        {
            weapons = catalog;
        }

        public string CaptureCheckpoint()
        {
            var gun = GetComponent<PlayerWeapon2D>();
            var light = GetComponent<Flashlight2D>();
            var upgrades = GetComponent<FlashlightUpgrades>();
            return JsonUtility.ToJson(new State
            {
                format = 3,
                health = GetComponent<PlayerLife2D>().Health,
                cards = GetComponent<PlayerAccessCards2D>().Capture(),
                weapon = gun.HasWeapon,
                weaponId = gun.Definition.contentId,
                ammo = gun.Ammo,
                modules = upgrades.Capture(),
                moduleCount = upgrades.InstalledCount,
                light = light.IsOn,
                direction = light.Direction,
                hasInventory = true,
                inventory = gun.CaptureInventory(),
                hasFlashlightState = true,
                flashlightOwned = light.IsOwned
            });
        }

        public void ValidateCheckpoint(string json)
        {
            var state = JsonUtility.FromJson<State>(json);
            if (state == null || state.health < 1 || weapons == null)
            {
                throw new InvalidOperationException("Invalid player checkpoint");
            }

            FlashlightModuleValidation.Validate(state.modules, state.moduleCount);
            var cards = AccessCardModel.ValidateSnapshot(state.cards);
            if (!cards.Success)
            {
                throw new InvalidOperationException(cards.Reason);
            }

            if (state.format == 1)
            {
                var legacy = weapons.Find(state.weaponId);
                if (legacy == null || state.ammo < 0 || state.ammo > legacy.maximumAmmo)
                {
                    throw new InvalidOperationException("Invalid legacy player weapon payload");
                }

                return;
            }

            if ((state.format != 2 && state.format != 3) || !state.hasInventory)
            {
                throw new InvalidOperationException("Missing/unsupported player inventory payload");
            }

            WeaponInventory.Validate(state.inventory, weapons.ToSettings());
            if (state.format == 3 && (!state.hasFlashlightState || (!state.flashlightOwned && state.light)))
            {
                throw new InvalidOperationException("Missing/invalid flashlight ownership payload");
            }
        }

        public void RestoreCheckpoint(string json)
        {
            ValidateCheckpoint(json);
            var saved = JsonUtility.FromJson<State>(json);
            GetComponent<PlayerLife2D>().RestoreHealth(saved.health);
            GetComponent<PlayerAccessCards2D>().Restore(saved.cards);
            var gun = GetComponent<PlayerWeapon2D>();
            if (saved.format == 1)
            {
                gun.RestoreState(saved.weapon, saved.ammo, weapons.Find(saved.weaponId));
            }
            else
            {
                gun.RestoreInventory(saved.inventory, weapons);
            }
            GetComponent<FlashlightUpgrades>().Restore(saved.modules, saved.moduleCount);
            var light = GetComponent<Flashlight2D>();
            light.RestoreOwnership(saved.format < 3 || saved.flashlightOwned);
            light.AimAt((Vector2)transform.position + saved.direction);
            light.SetPower(saved.light);
        }
    }
}
