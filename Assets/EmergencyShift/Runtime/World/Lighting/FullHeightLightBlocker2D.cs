using System;
using EmergencyShift.Core.Composition;
using UnityEngine;
using Zenject;

namespace EmergencyShift
{
    /// <summary>Marks a full-height wall or door, independently of low furniture collision layers.</summary>
    [ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(BoxCollider2D))]
    public sealed class FullHeightLightBlocker2D : MonoBehaviour
    {
        private SceneRegistry<FullHeightLightBlocker2D> registry;
        private BoxCollider2D shape;
        public BoxCollider2D Shape => shape != null ? shape : shape = GetComponent<BoxCollider2D>();

        [Inject]
        public void Construct(SceneRegistry<FullHeightLightBlocker2D> sceneBlockers)
        {
            if (sceneBlockers == null)
            {
                throw new ArgumentNullException(nameof(sceneBlockers));
            }

            registry?.Remove(this);
            registry = sceneBlockers;
            if (isActiveAndEnabled)
            {
                registry.Register(this);
            }
        }

        private void OnEnable()
        {
            registry?.Register(this);
        }

        private void OnDisable()
        {
            registry?.Remove(this);
        }
    }
}
