using UnityEngine;

namespace EmergencyShift
{
    [RequireComponent(typeof(CircleCollider2D))]
    public sealed class DialogueInteraction2D : MonoBehaviour, IInteractable
    {
        [SerializeField] private DialogueSequence sequence;
        [SerializeField] private DialogueSession session;
        [SerializeField, Min(.1f)] private float interactionRange = 1.6f;
        public Vector2 InteractionPosition => transform.position;
        public float InteractionRange => interactionRange;

        public bool IsAvailable(IInteractionActor actor)
        {
            return isActiveAndEnabled && actor != null && sequence != null && session != null
                && session.CanBegin && actor.CanReach(InteractionPosition, interactionRange);
        }

        public string GetPrompt(IInteractionActor actor)
        {
            return "[E] Пробный диалог · можно повторить";
        }

        public bool Interact(IInteractionActor actor)
        {
            return IsAvailable(actor) && session.TryBegin(sequence);
        }
    }
}
