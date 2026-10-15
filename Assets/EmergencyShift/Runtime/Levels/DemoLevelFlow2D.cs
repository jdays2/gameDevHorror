using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EmergencyShift
{
    /// <summary>Authored demo objective and exit. It does not spawn geometry, loot, UI or enemies.</summary>
    public sealed class DemoLevelFlow2D : MonoBehaviour
    {
        [SerializeField] private PlayerLife2D player;
        [SerializeField] private PlayerAccessCards2D cards;
        [SerializeField] private PlayerWeapon2D weapon;
        [SerializeField] private Text objective;
        [SerializeField] private GameObject completionPanel;
        [SerializeField] private Text completionText;
        [SerializeField] private ShadowStalker2D[] enemies;
        [SerializeField] private string serviceCardId = "service", exitCardId = "evacuation";
        private int lastPhase = -1;
        private float startedAt;
        public bool Completed { get; private set; }
        public int Phase { get; private set; }
        public void Configure(PlayerLife2D life, PlayerAccessCards2D inventory, Text task,
            GameObject panel, Text result, ShadowStalker2D[] actors)
        { player = life; cards = inventory; weapon = life.GetComponent<PlayerWeapon2D>(); objective = task;
            completionPanel = panel; completionText = result; enemies = actors; }
        private void Start() { startedAt = Time.time; completionPanel.SetActive(false); }
        private void Update()
        {
            if (Completed)
            {
                if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) Restart();
                return;
            }
            Phase = cards.Has(exitCardId) ? 3 : !cards.Has(serviceCardId) ? 0 : !weapon.HasWeapon ? 1 : 2;
            if (lastPhase == Phase) return;
            lastPhase = Phase;
            objective.text = Phase == 0 ? "ЦЕЛЬ: найти служебную карту · северная комната ↑"
                : Phase == 1 ? "ЦЕЛЬ: найти пистолет · распределитель →"
                : Phase == 2 ? "ЦЕЛЬ: найти эвакуационную карту · узел управления ↑"
                : "ЦЕЛЬ: восточный шлюз → зелёный выход";
        }
        public bool TryFinish(PlayerLife2D actor)
        {
            if (Completed || actor != player || player.IsDead || !cards.Has(exitCardId)) return false;
            Completed = true;
            player.GetComponent<PlayerMotor2D>().enabled = false;
            player.GetComponent<PlayerInteraction>().enabled = false;
            weapon.enabled = false;
            var body = player.GetComponent<Rigidbody2D>(); body.linearVelocity = Vector2.zero; body.simulated = false;
            // Finish is terminal: a pursuer must not kill the player behind the completion panel.
            foreach (var enemy in enemies)
                if (enemy != null)
                {
                    enemy.enabled = false;
                    var sound = enemy.GetComponent<StalkerAudio2D>(); if (sound != null) sound.enabled = false;
                }
            objective.text = "ВЫХОД НАЙДЕН";
            int seconds = Mathf.FloorToInt(Time.time - startedAt);
            completionText.text = "ВЫ ВЫБРАЛИСЬ\n\nДемонстрационный сектор пройден\n"
                + "Время  " + (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00")
                + "\nМодулей фонаря  " + player.GetComponent<FlashlightUpgrades>().InstalledCount
                + "\n\n[R]  Пройти заново";
            completionPanel.SetActive(true);
            return true;
        }
        public void Restart() { if (Completed) SceneManager.LoadScene(gameObject.scene.buildIndex); }
    }
}
