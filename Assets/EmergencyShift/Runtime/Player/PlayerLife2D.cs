using System.Collections;
using EmergencyShift.Features.Health.Logic;
using EmergencyShift.Features.Health.View;
using EmergencyShift.Features.PlayerRules.Config;
using UnityEngine;
using EmergencyShift.Features.PlayerInput.Logic;
using Zenject;

namespace EmergencyShift
{
    /// <summary>Native health/presentation facade; personal rules and commands belong to its controller/model.</summary>
    public sealed class PlayerLife2D : Damageable2D, IPlayerLifeView
    {
        [SerializeField, Min(1)]
        private int maximumHealth = 2;
        [SerializeField, Min(0)]
        private float hitProtectionSeconds = 0.7f;
        [SerializeField, Tooltip("Keep all authored values, even old defaults. Non-default legacy values are always retained.")]
        private bool useAuthoredRules;
        private PlayerRulesSettings rules = PlayerRulesSettings.LegacyDefaults;
        private PlayerLifeEnvironment2D environment;
        private SpriteRenderer visual;
        private Color normalColor;
        private HealthModel model;
        private PlayerLifeController controller;
        private HealthModelFactory models;
        private IPlayerInput input;
        public int Health => model?.Current ?? 0;
        public int MaximumHealth => model?.Maximum ?? maximumHealth;
        public bool IsDead => Health <= 0;

        public event System.Action<int> HealthChanged;
        public event System.Action<int, Vector2> Damaged;
        public override bool IsAlive => this != null && !IsDead;

        [Inject]
        public void Construct(HealthModelFactory modelFactory, IPlayerInput playerInput, PlayerRulesSettings playerRules = null)
        {
            models = modelFactory ?? throw new System.ArgumentNullException(nameof(modelFactory));
            input = playerInput ?? throw new System.ArgumentNullException(nameof(playerInput));
            rules = playerRules ?? PlayerRulesSettings.LegacyDefaults;
        }

        public override bool TryTakeDamage(int amount, Vector2 point, Vector2 direction) => ApplyDamage(amount, direction);
        public override void ReceiveBulletHit(int damage, Vector2 point, Vector2 direction)
        {
            ApplyDamage(damage, direction);
        }

        private void Awake()
        {
            environment = new PlayerLifeEnvironment2D(transform);
            visual = transform.Find("Capsule visual").GetComponent<SpriteRenderer>();
            normalColor = visual.color;
            if (models == null)
            {
                throw new System.InvalidOperationException("Player health requires level composition before Awake.");
            }

            controller = models.CreatePlayer(rules.ProjectHealth(maximumHealth, hitProtectionSeconds, useAuthoredRules), environment, this);
            model = controller.Model;
        }

        public void RestoreHealth(int health)
        {
            RequireController().Restore(health);
        }

        /// <summary>Changes this player's capacity without reviving a defeated player.</summary>
        public void SetHealthCapacity(int capacity, int currentHealth)
        {
            if (this == null || model == null)
            {
                return;
            }

            RequireController().SetCapacity(capacity, currentHealth);
        }

        public bool TakeDamage(int amount)
        {
            return ApplyDamage(amount, Vector2.zero);
        }

        private bool ApplyDamage(int amount, Vector2 direction)
        {
            if (this == null || model == null)
            {
                return false;
            }

            return RequireController().TakeDamage(amount, direction).Success;
        }

        void IPlayerLifeView.PresentDamage(int amount, Vector2 direction, bool defeated)
        {
            if (defeated)
            {
                StopAllCoroutines();
                visual.color = rules.DefeatedTint;
            }
            else if (isActiveAndEnabled)
            {
                StartCoroutine(HitFlash());
            }

            Damaged?.Invoke(amount, direction);
        }

        void IPlayerLifeView.PresentHealth(int current, bool restored)
        {
            if (restored)
            {
                StopAllCoroutines();
                visual.color = normalColor;
            }

            HealthChanged?.Invoke(current);
        }

        private PlayerLifeController RequireController()
        {
            if (this == null || model == null)
            {
                throw new System.InvalidOperationException("Player health requires its composed personal model.");
            }

            environment ??= new PlayerLifeEnvironment2D(transform);
            return controller ??= new PlayerLifeController(model, environment, this);
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            if (!IsDead && visual != null)
            {
                visual.color = normalColor;
            }
        }

        private IEnumerator HitFlash()
        {
            if (rules.HitFlashSeconds <= 0)
            {
                yield break;
            }

            visual.color = rules.HitTint;
            yield return new WaitForSeconds(rules.HitFlashSeconds);
            if (!IsDead)
            {
                visual.color = normalColor;
            }
        }

        private void Update()
        {
            if (IsDead && input.RestartPressed)
            {
                RestartRoom();
            }
        }

        public void RestartRoom()
        {
            if (this != null && model != null)
            {
                RequireController().Restart();
            }
        }

    }
}
