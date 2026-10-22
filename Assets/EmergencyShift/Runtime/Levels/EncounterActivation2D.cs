using System;
using EmergencyShift.Features.Encounter;
using EmergencyShift.Features.Encounter.Logic;
using UnityEngine;
using Zenject;

namespace EmergencyShift
{
    /// <summary>One-shot authorable activation of already placed enemies. It never resets an active fight.</summary>
    [RequireComponent(typeof(BoxCollider2D)), DisallowMultipleComponent]
    public sealed class EncounterActivation2D : MonoBehaviour, IEncounterView
    {
        [SerializeField]
        private GameObject[] actors;
        private EncounterController controller;
        public bool Activated => controller != null && controller.Activated;

        [Inject]
        public void Construct(EncounterControllerFactory factory)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            controller = factory.Create(this, Activated);
        }

        public void RestoreState(bool triggered)
        {
            RequireController().Restore(triggered);
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

        public bool Activate() => RequireController().Activate().Success;

        void IEncounterView.ActivateActors()
        {
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

        }

        private EncounterController RequireController() => controller ??
            throw new InvalidOperationException("Encounter activation requires level composition.");

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
