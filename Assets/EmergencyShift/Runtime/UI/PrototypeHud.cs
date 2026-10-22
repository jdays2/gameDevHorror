using UnityEngine;
using UnityEngine.UI;

namespace EmergencyShift
{
    /// <summary>Отображает состояние игрока, одну контекстную подсказку и результат подбора.</summary>
    public sealed class PrototypeHud : MonoBehaviour
    {
        [SerializeField]
        private Flashlight2D flashlight;
        [SerializeField]
        private PlayerInteraction interaction;
        [SerializeField]
        private Text flashlightStatus;
        [SerializeField]
        private Text doorPrompt;
        [SerializeField]
        private PlayerLife2D life;
        [SerializeField]
        private Text healthStatus;
        [SerializeField]
        private Text defeatPrompt;
        [SerializeField]
        private Text pickupNotification;
        [SerializeField]
        private PlayerWeapon2D weapon;
        [SerializeField]
        private Text ammoStatus;
        private int displayedHealth = -1;
        private int displayedMaximum = -1;
        private bool hasAmmoSnapshot;
        private (WeaponDefinition definition, int capacity, int loaded, int reserve, int ammo, WeaponPhase phase) lastAmmo;

        public void ConfigureWeapon(PlayerWeapon2D gun, Text counter)
        {
            weapon = gun;
            ammoStatus = counter;
            hasAmmoSnapshot = false;
        }

        public void ConfigurePickupNotification(Text label) => pickupNotification = label;
        public void Configure(Flashlight2D light, PlayerInteraction actor, Text status, Text prompt)
        {
            flashlight = light;
            interaction = actor;
            flashlightStatus = status;
            doorPrompt = prompt;
        }

        public void ConfigureLife(PlayerLife2D playerLife, Text health, Text defeat)
        {
            life = playerLife;
            healthStatus = health;
            displayedHealth = -1;
            displayedMaximum = -1;
            defeatPrompt = defeat;
        }

        private void Update()
        {
            bool dead = life != null && life.IsDead;
            flashlightStatus.text = FlashlightLabel(dead);
            if (healthStatus != null && life != null)
            {
                if (displayedHealth != life.Health || displayedMaximum != life.MaximumHealth)
                {
                    displayedHealth = life.Health;
                    displayedMaximum = life.MaximumHealth;
                    healthStatus.text = "ЗДОРОВЬЕ  " + displayedHealth + " / " + displayedMaximum;
                }
            }

            if (defeatPrompt != null)
            {
                defeatPrompt.gameObject.SetActive(dead);
            }

            if (ammoStatus != null)
            {
                ammoStatus.gameObject.SetActive(weapon != null && weapon.HasWeapon && !dead);
                if (weapon != null)
                {
                    RefreshAmmo();
                }
            }

            string prompt = interaction.CurrentPrompt;
            string message = interaction.Notification;
            bool showNotification = pickupNotification != null && !string.IsNullOrEmpty(message) && !dead;
            doorPrompt.gameObject.SetActive(!showNotification && !string.IsNullOrEmpty(prompt) && !dead);
            if (!string.IsNullOrEmpty(prompt))
            {
                doorPrompt.text = prompt;
            }

            if (pickupNotification != null)
            {
                pickupNotification.gameObject.SetActive(showNotification);
                if (!string.IsNullOrEmpty(message))
                {
                    pickupNotification.text = message;
                }
            }
        }

        private void RefreshAmmo()
        {
            var snapshot = (weapon.Definition, weapon.MagazineCapacity, weapon.LoadedAmmo,
                weapon.ReserveAmmo, weapon.Ammo, weapon.Phase);
            if (hasAmmoSnapshot && snapshot == lastAmmo)
            {
                return;
            }

            hasAmmoSnapshot = true;
            lastAmmo = snapshot;
            string ammunition = weapon.MagazineCapacity > 0 ? weapon.LoadedAmmo + " / " + weapon.ReserveAmmo : weapon.Ammo.ToString();
            string operation = "";
            if (weapon.Phase == WeaponPhase.Pumping)
            {
                operation = " · ПОМПА";
            }
            else if (weapon.Phase == WeaponPhase.Loading)
            {
                operation = " · ЗАРЯДКА";
            }

            ammoStatus.text = weapon.Definition.DisplayName.ToUpperInvariant() + "  " + ammunition + operation + "   [ЛКМ]";
        }

        private string FlashlightLabel(bool dead)
        {
            if (dead)
            {
                return "ФОНАРЬ ПОГАШЕН";
            }

            return flashlight.IsOn ? "[F]  ФОНАРЬ ВКЛЮЧЁН" : "[F]  ВКЛЮЧИТЬ ФОНАРЬ";
        }
    }
}
