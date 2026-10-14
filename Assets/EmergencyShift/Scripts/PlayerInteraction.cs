using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EmergencyShift
{
    /// <summary>Выбирает одну близкую дверь/предмет и передаёт E. HUD только читает результат.</summary>
    public sealed class PlayerInteraction : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float reach = 1.6f;
        [SerializeField] private Door2D[] doors;
        [SerializeField] private LayerMask blockingGeometry = 1 << 8;
        private readonly List<Collider2D> nearby = new List<Collider2D>(32);
        private Flashlight2D flashlight;
        private FlashlightUpgrades upgrades;
        private PlayerLife2D life;
        private float notificationUntil;
        private string notification;
        public IInteractable CurrentTarget { get; private set; }
        public Door2D CurrentDoor => CurrentTarget as Door2D;
        public Flashlight2D Flashlight => flashlight;
        public FlashlightUpgrades Upgrades => upgrades;
        public LayerMask BlockingGeometry => blockingGeometry;
        public string CurrentPrompt => CurrentTarget != null ? CurrentTarget.GetPrompt(this) : null;
        public string Notification => Time.time < notificationUntil ? notification : null;
        public void Configure(Door2D[] sceneDoors) => doors = sceneDoors;

        private void Awake()
        {
            flashlight = GetComponent<Flashlight2D>();
            upgrades = GetComponent<FlashlightUpgrades>();
            life = GetComponent<PlayerLife2D>();
        }

        private void Update()
        {
            RefreshTarget();
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) TryInteractCurrent();
        }

        private void OnDisable() { CurrentTarget = null; notification = null; }

        public bool CanReach(Vector2 point, float targetRange, Collider2D allowedBlocker = null)
        {
            if (!isActiveAndEnabled || (life != null && life.IsDead)) return false;
            float effectiveRange = Mathf.Min(reach, targetRange);
            if (((Vector2)transform.position - point).sqrMagnitude > effectiveRange * effectiveRange) return false;
            Collider2D blocker = Physics2D.Linecast(transform.position, point, blockingGeometry).collider;
            return blocker == null || blocker == allowedBlocker;
        }

        public void RefreshTarget()
        {
            CurrentTarget = null;
            if (!isActiveAndEnabled || (life != null && life.IsDead)) return;
            float closest = float.PositiveInfinity;
            float bestAngle = float.PositiveInfinity;
            if (doors != null)
                foreach (Door2D door in doors)
                    if (door != null) Consider(door, ref closest, ref bestAngle);
            // Локальный запрос с переиспользуемым списком; каждый предмет не читает E самостоятельно.
            Physics2D.OverlapCircle(transform.position, reach, new ContactFilter2D { useTriggers = true }, nearby);
            foreach (Collider2D collider in nearby)
            {
                IInteractable target = collider.GetComponentInParent<IInteractable>();
                if (target != null) Consider(target, ref closest, ref bestAngle);
            }
        }

        private void Consider(IInteractable target, ref float closest, ref float bestAngle)
        {
            if (!target.IsAvailable(this)) return;
            Vector2 offset = target.InteractionPosition - (Vector2)transform.position;
            float distance = offset.magnitude;
            float angle = flashlight != null ? Vector2.Angle(flashlight.Direction, offset) : 0;
            if (distance > closest + 0.05f || (Mathf.Abs(distance - closest) <= 0.05f && angle >= bestAngle)) return;
            closest = distance; bestAngle = angle; CurrentTarget = target;
        }

        /// <summary>Тот же путь используется клавишей E; повторно проверяет цель перед действием.</summary>
        public bool TryInteractCurrent()
        {
            RefreshTarget();
            if (CurrentTarget == null) return false;
            bool succeeded = CurrentTarget.Interact(this);
            RefreshTarget();
            return succeeded;
        }

        public void ShowNotification(string message)
        { notification = message; notificationUntil = Time.time + 3f; }
    }
}
