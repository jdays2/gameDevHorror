using UnityEngine;

namespace EmergencyShift
{
    /// <summary>One-shot authorable activation of already placed enemies. It never resets an active fight.</summary>
    [RequireComponent(typeof(BoxCollider2D)), DisallowMultipleComponent]
    public sealed class EncounterActivation2D : MonoBehaviour
    {
        [SerializeField]
        private GameObject[] actors;
        public bool Activated
        {
            get; private set;
        }

        public void RestoreState(bool triggered)
        {
            Activated = triggered;
        }

        public void Configure(GameObject[] targets)
        {
            actors = targets;
        }

        private void Reset()
        {
            GetComponent<BoxCollider2D>().isTrigger = true;
        }

        private void Awake()
        {
            GetComponent<BoxCollider2D>().isTrigger = true;
            // Actors are authored inactive. Awake does not turn off enemies that already exist in the room.
        }

        public bool Activate()
        {
            if (Activated)
            {
                return false;
            }

            Activated = true;
            if (actors != null)
            {
                foreach (var actor in actors)
                {
                    if (actor != null)
                    {
                        actor.SetActive(true);
                    }
                }
            }

            return true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerLife2D>() != null)
            {
                Activate();
            }
        }

        private void OnDrawGizmosSelected()
        {
            var shape = GetComponent<BoxCollider2D>();
            Gizmos.color = new Color(.8f, .45f, .15f, .65f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(shape.offset, shape.size);
        }
    }
}
