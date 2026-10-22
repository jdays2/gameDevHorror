using UnityEngine;

namespace EmergencyShift.Tests
{
    public sealed class InteractionProbeTarget : MonoBehaviour, IInteractable
    {
        public int Calls { get; private set; }
        public Vector2 InteractionPosition => transform.position;
        public float InteractionRange => 1.6f;
        public bool IsAvailable(IInteractionActor actor) => isActiveAndEnabled && actor.CanReach(InteractionPosition, InteractionRange);
        public string GetPrompt(IInteractionActor actor) => "Test target";

        public bool Interact(IInteractionActor actor)
        {
            if (!IsAvailable(actor))
            {
                return false;
            }

            Calls++;
            return true;
        }
    }
}
