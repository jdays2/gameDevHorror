using UnityEngine;

namespace EmergencyShift
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CircleCollider2D))]
    /// <summary>Наземный модуль: требует близости, света и E. После успешной установки исчезает.</summary>
    public sealed class FlashlightUpgradePickup : MonoBehaviour, IInteractable
    {
        [SerializeField, Tooltip("ID вида модуля. При Repeatable каждая новая копия даёт ещё один бонус.")]
        private string moduleId = "range-lens-01";
        [SerializeField] private string displayName = "Модуль фонаря";
        [SerializeField, Min(0)] private float rangeBonus = 0.6f;
        [SerializeField, Min(0)] private float angleBonus = 6f;
        [SerializeField, Min(0.1f)] private float pickupRange = 1f;
        [SerializeField] private bool requiresLight = true;
        [SerializeField, Tooltip("Можно собирать несколько экземпляров этого вида; каждый предмет всё равно однократный.")]
        private bool repeatable = true;
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
            return !requiresLight || actor.IsCurrentlyLit(InteractionPosition);
        }

        public string GetPrompt(PlayerInteraction actor)
        {
            if (!repeatable && actor.Upgrades.HasModule(moduleId)) return displayName + " — уже установлено";
            if (!actor.Upgrades.CanImprove(rangeBonus, angleBonus)) return "Фонарик улучшен до предела";
            return "[E]  Подобрать: " + displayName;
        }

        public bool Interact(PlayerInteraction actor)
        {
            if (!IsAvailable(actor)) return false;
            string message;
            if (!actor.Upgrades.TryInstall(moduleId, displayName, rangeBonus, angleBonus, out message, repeatable)) return false;
            collected = true;
            // Не Destroy: экземпляр явно хранит факт подбора, а повторный вызов безопасен.
            gameObject.SetActive(false);
            return true;
        }
    }
}
