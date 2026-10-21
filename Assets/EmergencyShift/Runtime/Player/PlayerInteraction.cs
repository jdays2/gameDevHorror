using System;
using System.Collections.Generic;
using EmergencyShift.Features.Interaction;
using EmergencyShift.Features.Interaction.Config;
using EmergencyShift.Features.Interaction.Logic;
using EmergencyShift.Core.Composition;
using UnityEngine;
using EmergencyShift.Features.PlayerInput.Logic;
using Zenject;

namespace EmergencyShift
{
    /// <summary>E selects a door; nearby loot collects automatically with queued HUD feedback.</summary>
    public sealed class PlayerInteraction : MonoBehaviour, IInteractionEnvironment
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
        [SerializeField, Min(0)]
        private float selectionTieDistance = .05f;
        [SerializeField, Range(1, 64)]
        private int maximumPendingNotices = 8;
        private readonly List<IInteractable> candidates = new List<IInteractable>(32);
        private InteractionSelectionController selection;
        private PickupNoticeModel notices;
        private readonly List<Collider2D> nearby = new List<Collider2D>(32);
        private Flashlight2D flashlight;
        private FlashlightUpgrades upgrades;
        private PlayerLife2D life;
        private PlayerWeapon2D weapon;
        private IPlayerInput input;
        private GameplayInputBlock inputBlock;
        private SceneRegistry<Door2D> doorRegistry;
        private LocalIlluminationQuery2D lights;

        [Inject]
        public void Construct(IPlayerInput playerInput, GameplayInputBlock block, SceneRegistry<Door2D> sceneDoors,
            PlayerInteractionControllerFactory factory, LocalIlluminationQuery2D illumination)
        {
            if (selection != null)
            {
                throw new InvalidOperationException("Player interaction is already composed.");
            }

            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            input = playerInput;
            inputBlock = block ?? throw new ArgumentNullException(nameof(block));
            doorRegistry = sceneDoors ?? throw new ArgumentNullException(nameof(sceneDoors));
            lights = illumination ?? throw new ArgumentNullException(nameof(illumination));
            var settings = new PlayerInteractionSettings(reach, selectionTieDistance, notificationSeconds, maximumPendingNotices);
            selection = factory.Create(settings, this);
            notices = factory.CreateNotices(settings);
        }
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
        public string Notification => notices?.Current;
        bool IInteractionEnvironment.CanAct => CanAct;
        private bool CanAct => inputBlock != null && !inputBlock.IsBlocked && isActiveAndEnabled && (life == null || !life.IsDead);

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
            if (inputBlock.IsBlocked)
            {
                return;
            }

            RequireNotices().Tick();

            CollectNearbyLoot();
            RefreshTarget();
            if (input != null && input.InteractPressed)
            {
                TryInteractCurrent();
            }
        }

        private void OnDisable()
        {
            selection?.Clear();
            notices?.Clear();
            candidates.Clear();
        }

        public void CollectNearbyLoot()
        {
            if (!CanAct)
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
            if (!CanAct)
            {
                return false;
            }

            if (!RequireSelection().Model.WithinReach((Vector2)transform.position - point, targetRange))
            {
                return false;
            }

            Collider2D blocker = Physics2D.Linecast(transform.position, point, blockingGeometry).collider;
            return blocker == null || blocker == allowedBlocker;
        }

        public bool IsCurrentlyLit(Vector2 point)
        {
            if (lights == null)
            {
                throw new InvalidOperationException("Player visibility requires level composition.");
            }

            return (flashlight != null && flashlight.Illuminates(point, blockingGeometry)) || lights.AnyIlluminates(gameObject.scene, point, blockingGeometry);
        }

        public void RefreshTarget() => RequireSelection().Refresh();

        int IInteractionEnvironment.ScanCandidates()
        {
            candidates.Clear();
            foreach (Door2D door in doorRegistry.Members)
            {
                if (door != null && door.gameObject.scene == gameObject.scene)
                {
                    candidates.Add(door);
                }
            }

            Physics2D.OverlapCircle(transform.position, selection.Model.Reach, new ContactFilter2D { useTriggers = true }, nearby);
            foreach (Collider2D collider in nearby)
            {
                IInteractable target = collider.GetComponentInParent<IInteractable>();
                if (target != null)
                {
                    candidates.Add(target);
                }
            }

            return candidates.Count;
        }

        InteractionCandidate IInteractionEnvironment.Observe(int index)
        {
            IInteractable target = TargetAt(index);
            bool available = target != null && target.IsAvailable(this);
            Vector2 offset = available ? target.InteractionPosition - (Vector2)transform.position : Vector2.zero;
            return new InteractionCandidate(index, available, offset, flashlight != null ? flashlight.Direction : Vector2.zero, flashlight != null);
        }

        bool IInteractionEnvironment.IsAvailable(int index)
        {
            IInteractable target = TargetAt(index);
            return target != null && target.IsAvailable(this);
        }

        bool IInteractionEnvironment.Interact(int index)
        {
            IInteractable target = TargetAt(index);
            return target != null && target.Interact(this);
        }

        void IInteractionEnvironment.PresentTarget(int index) => CurrentTarget = TargetAt(index);

        private IInteractable TargetAt(int index)
        {
            if (index < 0 || index >= candidates.Count)
            {
                return null;
            }

            IInteractable target = candidates[index];
            return target is UnityEngine.Object unityObject && unityObject == null ? null : target;
        }

        /// <summary>The E path refreshes and rechecks the target at command execution.</summary>
        public bool TryInteractCurrent() => RequireSelection().TryInteract().Success;

        public void ShowNotification(string message) => new ShowPickupNoticeCommand(RequireNotices(), message).Execute();
        private InteractionSelectionController RequireSelection() => selection ?? throw new InvalidOperationException("Player interaction requires level composition.");
        private PickupNoticeModel RequireNotices() => notices ?? throw new InvalidOperationException("Pickup notices require level composition.");

        public void ReportPickup(PickupKind kind, string description)
        {
            ShowNotification("Получено: " + description);
            ItemCollected?.Invoke(kind);
        }
    }
}
