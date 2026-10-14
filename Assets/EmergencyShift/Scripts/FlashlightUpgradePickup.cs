using UnityEngine;

namespace EmergencyShift
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CircleCollider2D))]
    /// <summary>Наземный модуль: требует близости, света и E. После успешной установки исчезает.</summary>
    public sealed class FlashlightUpgradePickup : MonoBehaviour, IInteractable
    {
        [SerializeField, Tooltip("Одинаковый ID означает один и тот же модуль; копия не усилит фонарь повторно.")]
        private string moduleId = "range-lens-01";
        [SerializeField] private string displayName = "Фокусирующая линза";
        [SerializeField, Min(0)] private float rangeBonus = 0.8f;
        [SerializeField, Min(0)] private float angleBonus;
        [SerializeField, Min(0.1f)] private float pickupRange = 1f;
        [SerializeField] private bool requiresLight = true;
        private bool collected;
        public string ModuleId => moduleId;
        public bool Collected => collected;
        public Vector2 InteractionPosition => transform.position;
        public float InteractionRange => pickupRange;

        private void Reset() => GetComponent<CircleCollider2D>().isTrigger = true;
        private void Awake() => GetComponent<CircleCollider2D>().isTrigger = true;
        private void OnValidate()
        {
            rangeBonus = Mathf.Max(0, rangeBonus); angleBonus = Mathf.Max(0, angleBonus);
            pickupRange = Mathf.Max(0.1f, pickupRange);
            if (moduleId != null) moduleId = moduleId.Trim();
        }

        public bool IsAvailable(PlayerInteraction actor)
        {
            if (collected || !isActiveAndEnabled || actor == null || actor.Upgrades == null) return false;
            if (!actor.CanReach(InteractionPosition, pickupRange)) return false;
            return !requiresLight || (actor.Flashlight != null
                && actor.Flashlight.Illuminates(InteractionPosition, actor.BlockingGeometry));
        }

        public string GetPrompt(PlayerInteraction actor)
        {
            if (actor.Upgrades.HasModule(moduleId)) return displayName + " — уже установлена";
            string bonus = rangeBonus > 0 ? $"дальность +{rangeBonus:0.#}" : "";
            if (angleBonus > 0) bonus += (bonus.Length > 0 ? "; " : "") + $"угол +{angleBonus:0.#}°";
            return "[E]  Подобрать: " + displayName + " · " + bonus;
        }

        public bool Interact(PlayerInteraction actor)
        {
            if (!IsAvailable(actor)) return false;
            string message;
            if (!actor.Upgrades.TryInstall(moduleId, displayName, rangeBonus, angleBonus, out message)) return false;
            collected = true;
            actor.ShowNotification(message);
            // Не Destroy: экземпляр явно хранит факт подбора, а повторный вызов безопасен.
            gameObject.SetActive(false);
            return true;
        }
    }
}
