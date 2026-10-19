using System.Collections;
using EmergencyShift.Features.Health.Config;
using EmergencyShift.Features.Health.Logic;
using UnityEngine;
using EmergencyShift.Features.PlayerInput.Logic;
using UnityEngine.SceneManagement;
using Zenject;

namespace EmergencyShift
{
    /// <summary>Два попадания, короткая защита после удара, поражение и перезапуск комнаты по R.</summary>
    public sealed class PlayerLife2D : Damageable2D
    {
        [SerializeField, Min(1)]
        private int maximumHealth = 2;
        [SerializeField, Min(0)]
        private float hitProtectionSeconds = 0.7f;
        private Rigidbody2D body;
        private PlayerMotor2D motor;
        private Flashlight2D flashlight;
        private PlayerInteraction interaction;
        private PlayerWeapon2D weapon;
        private SpriteRenderer visual;
        private Color normalColor;
        private HealthModel model;
        private HealthModelFactory models;
        private IPlayerInput input;
        public int Health => model?.Current ?? 0;
        public int MaximumHealth => maximumHealth;
        public bool IsDead => Health <= 0;

        public event System.Action<int> HealthChanged;
        public event System.Action<int, Vector2> Damaged;
        public override bool IsAlive => !IsDead;

        [Inject]
        public void Construct(HealthModelFactory modelFactory, IPlayerInput playerInput)
        {
            models = modelFactory ?? throw new System.ArgumentNullException(nameof(modelFactory));
            input = playerInput ?? throw new System.ArgumentNullException(nameof(playerInput));
        }

        public override bool TryTakeDamage(int amount, Vector2 point, Vector2 direction) => ApplyDamage(amount, direction);
        public override void ReceiveBulletHit(int damage, Vector2 point, Vector2 direction)
        {
            ApplyDamage(damage, direction);
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            motor = GetComponent<PlayerMotor2D>();
            flashlight = GetComponent<Flashlight2D>();
            interaction = GetComponent<PlayerInteraction>();
            weapon = GetComponent<PlayerWeapon2D>();
            visual = transform.Find("Capsule visual").GetComponent<SpriteRenderer>();
            normalColor = visual.color;
            if (models == null)
            {
                throw new System.InvalidOperationException("Player health requires level composition before Awake.");
            }

            model = models.Create(new HealthSettings(Mathf.Max(1, maximumHealth), hitProtectionSeconds));
        }

        public void RestoreHealth(int health)
        {
            StopAllCoroutines();
            model.Restore(Mathf.Clamp(health, 1, maximumHealth));
            motor.enabled = true;
            interaction.enabled = true;
            flashlight.enabled = true;
            if (weapon != null)
            {
                weapon.enabled = true;
            }

            body.simulated = true;
            body.linearVelocity = Vector2.zero;
            visual.color = normalColor;
            HealthChanged?.Invoke(Health);
        }

        /// <summary>Changes this player's capacity without reviving a defeated player.</summary>
        public void SetHealthCapacity(int capacity, int currentHealth)
        {
            if (IsDead)
            {
                return;
            }

            new SetHealthCapacityCommand(model, capacity, currentHealth).Execute();
            maximumHealth = model.Maximum;
            HealthChanged?.Invoke(Health);
        }

        public bool TakeDamage(int amount)
        {
            return ApplyDamage(amount, Vector2.zero);
        }

        private bool ApplyDamage(int amount, Vector2 direction)
        {
            if (model == null)
            {
                return false;
            }

            int previousHealth = Health;
            if (!new DamageHealthCommand(model, amount).Execute().Success)
            {
                return false;
            }
            if (IsDead)
            {
                StopAllCoroutines();
                motor.enabled = false;
                interaction.enabled = false;
                flashlight.SetPower(false);
                flashlight.enabled = false;
                if (weapon != null)
                {
                    weapon.enabled = false;
                }

                body.linearVelocity = Vector2.zero;
                body.simulated = false;
                visual.color = new Color(0.35f, 0.08f, 0.10f);
            }
            else
            {
                StartCoroutine(HitFlash());
            }

            Damaged?.Invoke(previousHealth - Health, direction);
            HealthChanged?.Invoke(Health);
            return true;
        }

        private IEnumerator HitFlash()
        {
            visual.color = new Color(1, 0.25f, 0.25f);
            yield return new WaitForSeconds(0.16f);
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
            if (!IsDead)
            {
                return;
            }

            var checkpoint = LevelCheckpointController2D.For(this);
            if (checkpoint != null)
            {
                checkpoint.Reload();
            }
            else
            {
                SceneManager.LoadScene(gameObject.scene.buildIndex);
            }
        }
    }
}
