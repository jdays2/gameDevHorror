using System;
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
            return JsonUtility.ToJson(new State { format = 1, health = GetComponent<PlayerLife2D>().Health, cards = GetComponent<PlayerAccessCards2D>().Capture(), weapon = gun.HasWeapon, weaponId = gun.Definition.contentId, ammo = gun.Ammo, modules = upgrades.Capture(), moduleCount = upgrades.InstalledCount, light = light.IsOn, direction = light.Direction });
        }

        public void ValidateCheckpoint(string json)
        {
            var state = JsonUtility.FromJson<State>(json);
            if (state == null || state.format != 1 || state.health < 1 || state.ammo < 0 || weapons == null || weapons.Find(state.weaponId) == null)
            {
                throw new InvalidOperationException("Invalid player checkpoint or missing weapon definition");
            }
        }

        public void RestoreCheckpoint(string json)
        {
            var saved = JsonUtility.FromJson<State>(json);
            GetComponent<PlayerLife2D>().RestoreHealth(saved.health);
            GetComponent<PlayerAccessCards2D>().Restore(saved.cards);
            GetComponent<PlayerWeapon2D>().RestoreState(saved.weapon, saved.ammo, weapons.Find(saved.weaponId));
            GetComponent<FlashlightUpgrades>().Restore(saved.modules, saved.moduleCount);
            var light = GetComponent<Flashlight2D>();
            light.AimAt((Vector2)transform.position + saved.direction);
            light.SetPower(saved.light);
        }
    }
}
