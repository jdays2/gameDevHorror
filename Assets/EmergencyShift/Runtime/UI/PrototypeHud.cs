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
        public void ConfigureWeapon(PlayerWeapon2D gun, Text counter)
        {
            weapon = gun;
            ammoStatus = counter;
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
            defeatPrompt = defeat;
        }

        private void Update()
        {
            bool dead = life != null && life.IsDead;
            flashlightStatus.text = FlashlightLabel(dead);
            if (healthStatus != null && life != null)
            {
                healthStatus.text = "ЗДОРОВЬЕ  " + life.Health + " / " + life.MaximumHealth;
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
                    ammoStatus.text = "ПАТРОНЫ  " + weapon.Ammo + "   [ЛКМ]";
                }
            }

            string prompt = interaction.CurrentPrompt;
            doorPrompt.gameObject.SetActive(!string.IsNullOrEmpty(prompt) && !dead);
            if (!string.IsNullOrEmpty(prompt))
            {
                doorPrompt.text = prompt;
            }

            if (pickupNotification != null)
            {
                string message = interaction.Notification;
                pickupNotification.gameObject.SetActive(!string.IsNullOrEmpty(message) && !dead);
                if (!string.IsNullOrEmpty(message))
                {
                    pickupNotification.text = message;
                }
            }
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
