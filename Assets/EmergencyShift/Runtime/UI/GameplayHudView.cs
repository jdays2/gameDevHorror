using System;
using System.Collections.Generic;
using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Reads the existing player state. Layout and icons remain authored Canvas children.</summary>
    public sealed class GameplayHudView : MonoBehaviour
    {
        [Serializable]
        public sealed class CardDisplay
        {
            public string cardId;
            public GameObject visual;
        }

        [SerializeField] private GameplayHudStyle style;
        [SerializeField] private PlayerLife2D life;
        [SerializeField] private PlayerWeapon2D weapon;
        [SerializeField] private Flashlight2D flashlight;
        [SerializeField] private PlayerAccessCards2D cards;
        [SerializeField] private UnityEngine.UI.Image healthFill;
        [SerializeField] private UnityEngine.UI.Image healthTrail;
        [SerializeField] private UnityEngine.UI.Text healthNumber;
        [SerializeField] private UnityEngine.UI.Text condition;
        [SerializeField] private UnityEngine.UI.Text lossLabel;
        [SerializeField] private CanvasGroup lamp;
        [SerializeField] private UnityEngine.UI.Image lampIcon;
        [SerializeField] private GameObject lampSlash;
        [SerializeField] private GameObject lampBinding;
        [SerializeField] private RectTransform activeWeaponSlot;
        [SerializeField] private RectTransform spareWeaponSlot;
        [SerializeField] private HudWeaponRow[] weaponRows;
        [SerializeField] private CardDisplay[] cardDisplays;
        [SerializeField] private AudioSource hitAudio;
        private readonly HashSet<string> ownedWeapons = new HashSet<string>();
        private float displayedFraction;
        private float trailFrom;
        private float damagedAt = float.NegativeInfinity;
        private bool trailAnimating;
        private bool damageChangePending;
        private bool subscribed;
        public GameplayHudStyle Style => style;

        public void Bind(PlayerLife2D player, PlayerWeapon2D weapons, Flashlight2D light, PlayerAccessCards2D accessCards)
        {
            if (style != null)
            {
                style.Validate();
            }

            Unsubscribe();
            life = player;
            weapon = weapons;
            flashlight = light;
            cards = accessCards;
            if (Application.isPlaying && isActiveAndEnabled)
            {
                Subscribe();
            }
        }

        private void Start()
        {
            if (style == null || life == null || weapon == null || flashlight == null)
            {
                Debug.LogError("Gameplay HUD requires its style and player references.", this);
                enabled = false;
                return;
            }

            Subscribe();
            OnHealthChanged(life.Health);
            RefreshWeapons();
            RefreshLamp(flashlight.IsOn);
            RefreshCards();
        }

        private void OnEnable()
        {
            if (style != null && life != null && weapon != null && flashlight != null)
            {
                Subscribe();
            }
        }

        private void Subscribe()
        {
            if (subscribed)
            {
                return;
            }

            style.Validate();
            life.HealthChanged += OnHealthChanged;
            life.Damaged += OnDamaged;
            weapon.OwnershipChanged += RefreshWeapons;
            flashlight.PowerChanged += RefreshLamp;
            flashlight.OwnershipChanged += RefreshLampOwnership;
            if (cards != null)
            {
                cards.Changed += RefreshCards;
            }
            subscribed = true;
            ClearFeedback();
            OnHealthChanged(life.Health);
            RefreshWeapons();
            RefreshLamp(flashlight.IsOn);
            RefreshCards();
        }

        private void Unsubscribe()
        {
            if (!subscribed)
            {
                return;
            }

            life.HealthChanged -= OnHealthChanged;
            life.Damaged -= OnDamaged;
            weapon.OwnershipChanged -= RefreshWeapons;
            flashlight.PowerChanged -= RefreshLamp;
            flashlight.OwnershipChanged -= RefreshLampOwnership;
            if (cards != null)
            {
                cards.Changed -= RefreshCards;
            }
            subscribed = false;
        }

        private void OnDisable()
        {
            Unsubscribe();
            ClearFeedback();
            if (hitAudio != null)
            {
                hitAudio.Stop();
            }
        }

        private void OnHealthChanged(int current)
        {
            float fraction = (float)current / Mathf.Max(1, life.MaximumHealth);
            if (!damageChangePending)
            {
                ClearFeedback();
                healthTrail.fillAmount = fraction;
            }

            healthFill.fillAmount = fraction;
            Color tint = style.Healthy;
            if (fraction <= style.CriticalFraction)
            {
                tint = style.Critical;
            }
            else if (fraction <= style.WoundedFraction)
            {
                tint = style.Wounded;
            }

            healthFill.color = tint;
            healthNumber.text = current.ToString();
            condition.text = current <= 0 ? "ПОРАЖЕНИЕ" : "";
            if (current > 0 && fraction <= style.CriticalFraction)
            {
                condition.text = "КРИТИЧЕСКОЕ СОСТОЯНИЕ";
            }

            displayedFraction = fraction;
            damageChangePending = false;
        }

        private void OnDamaged(int amount, Vector2 direction)
        {
            trailFrom = Mathf.Max(healthTrail.fillAmount, displayedFraction);
            damagedAt = Time.unscaledTime;
            trailAnimating = true;
            damageChangePending = true;
            healthTrail.color = style.LostHealth;
            healthTrail.fillAmount = trailFrom;
            lossLabel.text = "−" + amount;
            lossLabel.color = style.LostHealth;
            lossLabel.enabled = true;
            if (hitAudio != null && style.HitSoundCount > 0)
            {
                var clip = style.HitSoundAt(UnityEngine.Random.Range(0, style.HitSoundCount));
                if (clip != null)
                {
                    hitAudio.PlayOneShot(clip, style.HitVolume);
                }
            }
        }

        private void ClearFeedback()
        {
            trailAnimating = false;
            damageChangePending = false;
            damagedAt = float.NegativeInfinity;
            if (lossLabel != null)
            {
                lossLabel.enabled = false;
            }
        }

        private void RefreshLampOwnership()
        {
            RefreshLamp(flashlight.IsOn);
        }

        private void RefreshLamp(bool on)
        {
            if (lampBinding != null)
            {
                lampBinding.SetActive(flashlight.IsOwned);
            }

            lamp.alpha = on ? 1 : .7f;
            if (!flashlight.IsOwned)
            {
                lamp.alpha = 0;
            }
            lampIcon.gameObject.SetActive(on);
            lampSlash.SetActive(!on);
        }

        private void RefreshCards()
        {
            foreach (var display in cardDisplays)
            {
                display.visual.SetActive(cards != null && cards.Has(display.cardId));
            }
        }

        private void RefreshWeapons()
        {
            ownedWeapons.Clear();
            foreach (var entry in weapon.CaptureInventory().weapons)
            {
                ownedWeapons.Add(entry.contentId);
            }

            foreach (var row in weaponRows)
            {
                bool owned = ownedWeapons.Contains(row.Definition.ContentId);
                bool active = weapon.HasWeapon && weapon.Definition.ContentId == row.Definition.ContentId;
                row.Present(owned, active, activeWeaponSlot, spareWeaponSlot, style.SpareOpacity);
            }
        }

        private void LateUpdate()
        {
            if (!subscribed)
            {
                return;
            }

            foreach (var row in weaponRows)
            {
                if (row.isActiveAndEnabled && weapon.HasWeapon && row.Definition.ContentId == weapon.Definition.ContentId)
                {
                    row.RefreshAmmunition(weapon);
                }
            }

            if (!trailAnimating)
            {
                return;
            }

            float age = Time.unscaledTime - damagedAt;
            float progress = Mathf.Clamp01((age - style.TrailDelay) / Mathf.Max(.01f, style.TrailDuration));
            healthTrail.fillAmount = Mathf.Lerp(trailFrom, healthFill.fillAmount, progress);
            Color loss = lossLabel.color;
            loss.a = 1 - Mathf.Clamp01(age / Mathf.Max(.01f, style.HitDuration));
            lossLabel.color = loss;
            if (progress >= 1 && age >= style.HitDuration)
            {
                ClearFeedback();
            }
        }
    }
}
