using System.Collections;
using EmergencyShift.Core.Composition;
using UnityEngine;
using Zenject;

namespace EmergencyShift
{
    /// <summary>Двигает створку двери; коллайдер и тень двигаются вместе с ней.</summary>
    public sealed class Door2D : MonoBehaviour, IInteractable
    {
        [SerializeField]
        private Transform leaf;
        [SerializeField]
        private Vector3 openOffset = new Vector3(0, 3.1f, 0);
        [SerializeField, Min(0.05f)]
        private float duration = 0.3f;
        private Vector3 closedPosition;
        private bool moving;
        private bool awaitingAccess;
        private DoorAccess2D access;
        private SceneRegistry<Door2D> registry;
        public bool IsOpen
        {
            get; private set;
        }
        public bool IsMoving => moving;

        public event System.Action<bool> MotionStarted;
        public event System.Action<bool> MotionStopped;
        public bool CanInteract => !moving && !awaitingAccess;
        public Collider2D Blocker => leaf.GetComponent<Collider2D>();
        public Vector2 InteractionPosition => transform.position;
        public float InteractionRange => 1.6f;

        [Inject]
        private void Construct(SceneRegistry<Door2D> sceneRegistry)
        {
            registry?.Remove(this);
            registry = sceneRegistry;
            if (isActiveAndEnabled)
            {
                registry.Register(this);
            }
        }

        public bool IsAvailable(PlayerInteraction actor) => isActiveAndEnabled && CanInteract && actor.CanReach(InteractionPosition, InteractionRange, Blocker);
        public string GetPrompt(PlayerInteraction actor)
        {
            if (IsOpen)
            {
                return "[E]  Закрыть дверь";
            }

            return access != null ? access.Prompt(actor) : "Нажмите «E», чтобы открыть";
        }

        public bool Interact(PlayerInteraction actor)
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

            if (firstUnlock && access.ConfirmationDelay > 0)
            {
                awaitingAccess = true;
                StartCoroutine(OpenAfterConfirmation(access.ConfirmationDelay));
                return true;
            }

            Toggle();
            return true;
        }

        private IEnumerator OpenAfterConfirmation(float delay)
        {
            yield return new WaitForSeconds(delay);
            awaitingAccess = false;
            Toggle();
        }

        public void Configure(Transform doorLeaf) => leaf = doorLeaf;
        private void Awake()
        {
            closedPosition = leaf.localPosition;
            access = GetComponent<DoorAccess2D>();
        }

        private void OnEnable()
        {
            registry?.Register(this);
        }

        public void RestoreState(bool open)
        {
            StopAllCoroutines();
            awaitingAccess = false;
            IsOpen = open;
            moving = false;
            leaf.localPosition = open ? closedPosition + openOffset : closedPosition;
        }

        public void Toggle()
        {
            if (moving || awaitingAccess || (!IsOpen && access != null && !access.IsUnlocked))
            {
                return;
            }

            IsOpen = !IsOpen;
            moving = true;
            MotionStarted?.Invoke(IsOpen);
            StartCoroutine(Slide(IsOpen ? closedPosition + openOffset : closedPosition));
        }

        private IEnumerator Slide(Vector3 destination)
        {
            moving = true;
            Vector3 start = leaf.localPosition;
            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                leaf.localPosition = Vector3.Lerp(start, destination, Mathf.SmoothStep(0, 1, elapsed / duration));
                yield return null;
            }

            leaf.localPosition = destination;
            moving = false;
            MotionStopped?.Invoke(IsOpen);
        }

        private void OnDisable()
        {
            registry?.Remove(this);
            StopAllCoroutines();
            awaitingAccess = false;
            if (!moving)
            {
                return;
            }

            moving = false;
            if (leaf != null)
            {
                leaf.localPosition = IsOpen ? closedPosition + openOffset : closedPosition;
            }

            MotionStopped?.Invoke(IsOpen);
        }

    }
}
