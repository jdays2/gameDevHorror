using System;
using UnityEngine;
using Zenject;

namespace EmergencyShift.Core.Composition
{
    /// <summary>Injects scene-owned instances before Awake; never creates persistent spawn identities.</summary>
    public sealed class ScenePrefabFactory
    {
        private readonly DiContainer container;
        private readonly Transform sceneRoot;

        public ScenePrefabFactory(DiContainer container, Transform sceneRoot)
        {
            this.container = container ?? throw new ArgumentNullException(nameof(container));
            this.sceneRoot = sceneRoot != null ? sceneRoot : throw new ArgumentNullException(nameof(sceneRoot));
        }

        public GameObject Instantiate(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (parent != null && parent.gameObject.scene != sceneRoot.gameObject.scene)
            {
                throw new ArgumentException("An instance must belong to its composition scene.", nameof(parent));
            }

            GameObject instance = container.InstantiatePrefab(prefab, position, rotation, parent != null ? parent : sceneRoot);
            if (parent == null)
            {
                instance.transform.SetParent(null, true);
            }

            return instance;
        }
    }
}
