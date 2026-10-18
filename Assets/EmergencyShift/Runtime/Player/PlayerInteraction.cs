using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EmergencyShift
{
    /// <summary>E selects a door; nearby loot collects automatically with queued HUD feedback.</summary>
    public sealed class PlayerInteraction : MonoBehaviour
    {
        [SerializeField, Min(0.1f)]
        private float reach = 1.6f;
        [SerializeField]
        private Door2D[] doors;
        [SerializeField]
        private LayerMask blockingGeometry = 1 << 8;
        [SerializeField, Min(.1f)]
        private float autoPickupRadius = .65f;
        [SerializeField, Min(.5f)]
        private float notificationSeconds = 1.8f;
        private readonly List<Collider2D> nearby = new List<Collider2D>(32);
        private Flashlight2D flashlight;
        private FlashlightUpgrades upgrades;
        private PlayerLife2D life;
        private PlayerWeapon2D weapon;
        private float notificationUntil;
        private string notification;
        private readonly Queue<string> notices = new Queue<string>();
        public event System.Action<PickupKind> ItemCollected;
        public IInteractable CurrentTarget
        {
            get; private set;
        }
        public Door2D CurrentDoor => CurrentTarget as Door2D;
        public Flashlight2D Flashlight => flashlight;
        public FlashlightUpgrades Upgrades => upgrades;
        public PlayerWeapon2D Weapon => weapon;
        public LayerMask BlockingGeometry => blockingGeometry;
        public string CurrentPrompt => CurrentTarget != null ? CurrentTarget.GetPrompt(this) : null;
        public string Notification => Time.time < notificationUntil ? notification : null;

        public void Configure(Door2D[] sceneDoors) => doors = sceneDoors;
        private void Awake()
        {
            flashlight = GetComponent<Flashlight2D>();
            upgrades = GetComponent<FlashlightUpgrades>();
            life = GetComponent<PlayerLife2D>();
            weapon = GetComponent<PlayerWeapon2D>();
        }

        private void Update()
        {
            if (GameplayInputBlock.IsBlocked)
            {
                return;
            }

            if (Time.time >= notificationUntil && notices.Count > 0)
            {
                StartNotice(notices.Dequeue());
            }

            CollectNearbyLoot();
            RefreshTarget();
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                TryInteractCurrent();
            }
        }

        private void OnDisable()
        {
            CurrentTarget = null;
            notification = null;
            notices.Clear();
            notificationUntil = 0;
        }

        public void CollectNearbyLoot()
        {
            if (GameplayInputBlock.IsBlocked || !isActiveAndEnabled || (life != null && life.IsDead))
            {
                return;
            }

            Physics2D.OverlapCircle(transform.position, autoPickupRadius, new ContactFilter2D { useTriggers = true }, nearby);
            foreach (var collider in nearby)
            {
                var pickup = collider.GetComponentInParent<IAutoPickup>();
                if (pickup != null && (pickup.PickupPosition - (Vector2)transform.position).sqrMagnitude <= autoPickupRadius * autoPickupRadius)
                {
                    pickup.TryCollect(this);
                }
            }
        }

        public bool CanReach(Vector2 point, float targetRange, Collider2D allowedBlocker = null)
        {
            if (GameplayInputBlock.IsBlocked || !isActiveAndEnabled || (life != null && life.IsDead))
            {
                return false;
            }

            float effectiveRange = Mathf.Min(reach, targetRange);
            if (((Vector2)transform.position - point).sqrMagnitude > effectiveRange * effectiveRange)
            {
                return false;
            }

            Collider2D blocker = Physics2D.Linecast(transform.position, point, blockingGeometry).collider;
            return blocker == null || blocker == allowedBlocker;
        }

        public bool IsCurrentlyLit(Vector2 point)
        {
            return (flashlight != null && flashlight.Illuminates(point, blockingGeometry)) || LocalLamp2D.AnyIlluminates(point, blockingGeometry) || ShotEffect2D.IlluminatesPoint(point, blockingGeometry);
        }

        public void RefreshTarget()
        {
            CurrentTarget = null;
            if (GameplayInputBlock.IsBlocked || !isActiveAndEnabled || (life != null && life.IsDead))
            {
                return;
            }

            float closest = float.PositiveInfinity;
            float bestAngle = float.PositiveInfinity;
            foreach (Door2D door in Door2D.Active)
            {
                if (door != null && door.gameObject.scene == gameObject.scene)
                {
                    Consider(door, ref closest, ref bestAngle);
                }
            }

            // Локальный запрос с переиспользуемым списком; каждый предмет не читает E самостоятельно.
            Physics2D.OverlapCircle(transform.position, reach, new ContactFilter2D { useTriggers = true }, nearby);
            foreach (Collider2D collider in nearby)
            {
                IInteractable target = collider.GetComponentInParent<IInteractable>();
                if (target != null)
                {
                    Consider(target, ref closest, ref bestAngle);
                }
            }
        }

        private void Consider(IInteractable target, ref float closest, ref float bestAngle)
        {
            if (!target.IsAvailable(this))
            {
                return;
            }

            Vector2 offset = target.InteractionPosition - (Vector2)transform.position;
            float distance = offset.magnitude;
            float angle = flashlight != null ? Vector2.Angle(flashlight.Direction, offset) : 0;
            if (distance > closest + 0.05f || (Mathf.Abs(distance - closest) <= 0.05f && angle >= bestAngle))
            {
                return;
            }

            closest = distance;
            bestAngle = angle;
            CurrentTarget = target;
        }

        /// <summary>Тот же путь используется клавишей E; повторно проверяет цель перед действием.</summary>
        public bool TryInteractCurrent()
        {
            RefreshTarget();
            if (CurrentTarget == null)
            {
                return false;
            }

            bool succeeded = CurrentTarget.Interact(this);
            RefreshTarget();
            return succeeded;
        }

        private void StartNotice(string message)
        {
            notification = message;
            notificationUntil = Time.time + notificationSeconds;
        }

        public void ShowNotification(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            if (Time.time >= notificationUntil && notices.Count == 0)
            {
                StartNotice(message);
            }
            else
            {
                if (notices.Count >= 8)
                {
                    notices.Dequeue();
                }

                notices.Enqueue(message);
            }
        }

        public void ReportPickup(PickupKind kind, string description)
        {
            ShowNotification("Получено: " + description);
            ItemCollected?.Invoke(kind);
        }
    }
}
