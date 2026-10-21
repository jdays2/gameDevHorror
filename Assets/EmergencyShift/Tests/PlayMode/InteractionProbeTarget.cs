using UnityEngine;

namespace EmergencyShift.Tests
{
    public sealed class InteractionProbeTarget : MonoBehaviour, IInteractable
    {
        public int Calls { get; private set; }
        public Vector2 InteractionPosition => transform.position;
        public float InteractionRange => 1.6f;
        public bool IsAvailable(PlayerInteraction actor) => isActiveAndEnabled && actor.CanReach(InteractionPosition, InteractionRange);
        public string GetPrompt(PlayerInteraction actor) => "Test target";

        public bool Interact(PlayerInteraction actor)
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
