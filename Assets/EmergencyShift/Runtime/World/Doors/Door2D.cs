using System;
using System.Collections;
using EmergencyShift.Core.Composition;
using EmergencyShift.Features.Doors;
using EmergencyShift.Features.Doors.Logic;
using UnityEngine;
using Zenject;

namespace EmergencyShift
{
    /// <summary>Двигает створку двери; коллайдер и тень двигаются вместе с ней.</summary>
    public sealed class Door2D : MonoBehaviour, IInteractable, IDoorMotionView
    {
        [SerializeField]
        private Transform leaf;
        [SerializeField]
        private Vector3 openOffset = new Vector3(0, 3.1f, 0);
        [SerializeField, Min(0.05f)]
        private float duration = 0.3f;
        private Vector3 closedPosition;
        private DoorAccess2D access;
        private SceneRegistry<Door2D> registry;
        private DoorMotionControllerFactory factory;
        private DoorMotionController controller;
        public bool IsOpen => controller != null && controller.IsOpen;
        public bool IsMoving => controller != null && controller.IsMoving;

        public event System.Action<bool> MotionStarted;
        public event System.Action<bool> MotionStopped;
        public bool CanInteract => controller == null || controller.CanInteract;
        public Collider2D Blocker => leaf.GetComponent<Collider2D>();
        public Vector2 InteractionPosition => transform.position;
        public float InteractionRange => 1.6f;

        [Inject]
        public void Construct(SceneRegistry<Door2D> sceneRegistry, DoorMotionControllerFactory controllers)
        {
            registry?.Remove(this);
            registry = sceneRegistry ?? throw new ArgumentNullException(nameof(sceneRegistry));
            factory = controllers ?? throw new ArgumentNullException(nameof(controllers));
            if (isActiveAndEnabled)
            {
                registry.Register(this);
            }
        }

        public bool IsAvailable(IInteractionActor actor) => isActiveAndEnabled && CanInteract && actor != null && actor.CanReach(InteractionPosition, InteractionRange, Blocker);
        public string GetPrompt(IInteractionActor actor)
        {
            if (IsOpen)
            {
                return "[E]  Закрыть дверь";
            }

            return access != null ? access.Prompt(actor) : "Нажмите «E», чтобы открыть";
        }

        public bool Interact(IInteractionActor actor)
        {
            if (!IsAvailable(actor))
            {
                return false;
            }

            bool firstUnlock = !IsOpen && access != null && !access.IsUnlocked;
            if (!IsOpen && access != null && !access.TryUnlock(actor))
            {
                return false;
            }

            float delay = firstUnlock ? access.ConfirmationDelay : 0;
            return RequestToggle(IsOpen || access == null || access.IsUnlocked, delay);
        }

        public void Configure(Transform doorLeaf) => leaf = doorLeaf;
        private void Awake()
        {
            closedPosition = leaf.localPosition;
            access = GetComponent<DoorAccess2D>();
            RequireController();
        }

        private void OnEnable()
        {
            registry?.Register(this);
        }

        public void RestoreState(bool open)
        {
            StopAllCoroutines();
            RequireController().Restore(open);
        }

        public void Toggle()
        {
            RequestToggle(IsOpen || access == null || access.IsUnlocked, 0);
        }

        private void OnDisable()
        {
            registry?.Remove(this);
            StopAllCoroutines();
            controller?.Stop();
        }

        private void OnDestroy() => controller?.Dispose();

        void IDoorMotionView.ApplyOpenAmount(float amount) => leaf.localPosition = Vector3.LerpUnclamped(closedPosition, closedPosition + openOffset, amount);
        void IDoorMotionView.MotionStarted(bool open) => MotionStarted?.Invoke(open);
        void IDoorMotionView.MotionStopped(bool open) => MotionStopped?.Invoke(open);

        private bool RequestToggle(bool unlocked, float delay)
        {
            if (!RequireController().Toggle(unlocked, delay).Success)
            {
                return false;
            }

            StartCoroutine(AdvanceMotion());
            return true;
        }

        private IEnumerator AdvanceMotion()
        {
            while (!controller.CanInteract)
            {
                controller.Tick();
                yield return null;
            }
        }

        private DoorMotionController RequireController()
        {
            if (factory == null)
            {
                throw new InvalidOperationException("Door motion requires level composition.");
            }

            return controller ??= factory.Create(duration, this);
        }
    }
}
