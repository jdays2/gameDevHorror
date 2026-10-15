using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace EmergencyShift
{
    /// <summary>Два попадания, короткая защита после удара, поражение и перезапуск комнаты по R.</summary>
    public sealed class PlayerLife2D : Damageable2D
    {
        [SerializeField, Min(1)] private int maximumHealth = 2;
        [SerializeField, Min(0)] private float hitProtectionSeconds = 0.7f;
        private Rigidbody2D body;
        private PlayerMotor2D motor;
        private Flashlight2D flashlight;
        private PlayerInteraction interaction;
        private PlayerWeapon2D weapon;
        private SpriteRenderer visual;
        private Color normalColor;
        private float protectedUntil;
        public int Health { get; private set; }
        public int MaximumHealth => maximumHealth;
        public bool IsDead => Health <= 0;
        public event System.Action<int> HealthChanged;
        public override bool IsAlive => !IsDead;
        public override void ReceiveBulletHit(int damage,Vector2 point,Vector2 direction) { TakeDamage(damage); }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>(); motor = GetComponent<PlayerMotor2D>();
            flashlight = GetComponent<Flashlight2D>(); interaction = GetComponent<PlayerInteraction>();
            weapon = GetComponent<PlayerWeapon2D>();
            visual = transform.Find("Capsule visual").GetComponent<SpriteRenderer>();
            normalColor = visual.color; Health = maximumHealth;
        }

        public bool TakeDamage(int amount)
        {
            if (IsDead || amount <= 0 || Time.time < protectedUntil) return false;
            Health = Mathf.Max(0, Health - amount);
            protectedUntil = Time.time + hitProtectionSeconds;
            if (IsDead)
            {
                StopAllCoroutines();
                motor.enabled = false; interaction.enabled = false;
                flashlight.SetPower(false); flashlight.enabled = false;
                if (weapon != null) weapon.enabled = false;
                body.linearVelocity = Vector2.zero; body.simulated = false;
                visual.color = new Color(0.35f, 0.08f, 0.10f);
            }
            else StartCoroutine(HitFlash());
            HealthChanged?.Invoke(Health);
            return true;
        }

        private IEnumerator HitFlash()
        {
            visual.color = new Color(1,0.25f,0.25f);
            yield return new WaitForSeconds(0.16f);
            if (!IsDead) visual.color = normalColor;
        }

        private void Update()
        {
            if (IsDead && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
                RestartRoom();
        }

        public void RestartRoom()
        {
            if (IsDead) SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
