using System.Collections;
using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Двигает створку двери; коллайдер и тень двигаются вместе с ней.</summary>
    public sealed class Door2D : MonoBehaviour, IInteractable
    {
        [SerializeField] private Transform leaf;
        [SerializeField] private Vector3 openOffset = new Vector3(0, 3.1f, 0);
        [SerializeField, Min(0.05f)] private float duration = 0.3f;
        private Vector3 closedPosition;
        private bool moving;
        private static readonly System.Collections.Generic.List<Door2D> active=new System.Collections.Generic.List<Door2D>();
        public static System.Collections.Generic.IReadOnlyList<Door2D> Active => active;
        public bool IsOpen { get; private set; }
        public bool IsMoving => moving;
        public event System.Action<bool> MotionStarted;
        public event System.Action<bool> MotionStopped;
        public bool CanInteract => !moving;
        public Collider2D Blocker => leaf.GetComponent<Collider2D>();
        public Vector2 InteractionPosition => transform.position;
        public float InteractionRange => 1.6f;
        public bool IsAvailable(PlayerInteraction actor) => isActiveAndEnabled && CanInteract
            && actor.CanReach(InteractionPosition, InteractionRange, Blocker);
        public string GetPrompt(PlayerInteraction actor) => IsOpen ? "[E]  Закрыть дверь" : "Нажмите «E», чтобы открыть";
        public bool Interact(PlayerInteraction actor)
        { if (!IsAvailable(actor)) return false; Toggle(); return true; }

        public void Configure(Transform doorLeaf) => leaf = doorLeaf;
        private void Awake() => closedPosition = leaf.localPosition;
        private void OnEnable(){if(!active.Contains(this))active.Add(this);}

        public void Toggle()
        {
            if (moving) return;
            IsOpen = !IsOpen;
            moving = true; MotionStarted?.Invoke(IsOpen);
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
            active.Remove(this);
            if (!moving) return;
            StopAllCoroutines(); moving = false;
            if (leaf != null) leaf.localPosition = IsOpen ? closedPosition + openOffset : closedPosition;
            MotionStopped?.Invoke(IsOpen);
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry(){active.Clear();}
    }
}
